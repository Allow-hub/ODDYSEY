# Card System（カードシステム）

## システム概要

カードは **CardData**（ScriptableObject）・**CardInstance**（ランタイムインスタンス）・**CardView**（UI）の3層で構成される。  
ターン開始時に `BattleLogic` がデッキから `CardInstance` を生成してロールし、`TurnData` 経由で View に渡す。

---

## クラス構成

```
CardData            ScriptableObject。カード1枚の静的定義。
  └─ CardEffectBase[]  効果リスト（DamageEffect / StatusEffect など）

CardInstance        ランタイムオブジェクト。手札1枚を表す。
  └─ CardData への参照 + ロール済み値（確率・ダメージ）を保持

CardView            MonoBehaviour。手札UIとドラッグ操作を担当。
```

---

## 全体フロー

```
BattleLogic.DrawToFull()
  │
  ├─ deck から CardData を取り出す
  ├─ new CardInstance(cardData)          // インスタンス生成
  ├─ instance.RollValues()               // 効果値を確定（1度だけ）
  └─ hand に追加
        │
        ▼
BattleLogic.BeginTurn() → TurnData.Hand に CardInstance のリストを入れて返す
        │
        ▼
PlayZonePresenter.SetupTurn(turnData, hand)
  └─ CardView に CardData を渡して表示

    （プレイヤーがカードをスロットにドラッグ）
        │
        ▼
PlayZonePresenter.OnCardPlaced(slotIndex, cardView)
  └─ PlayZoneSlot.PlayerCardInstance = instance  // ロジック側へ反映

    （ターン確定 ※未実装）
        │
        ▼
BattleLogic.ConfirmTurn()  // 各スロットのカードを左から順に解決
  └─ instance.TryExecuteEffect(i)        // 確率判定
      ├─ 成功: instance.GetEffectiveDamage(i) でダメージ取得
      └─ 失敗: 効果なし
```

---

## CardData

```csharp
[CreateAssetMenu(fileName = "CardData", menuName = "ODDESEY/CardData")]
public class CardData : ScriptableObject
{
    public string CardName;
    public Sprite CardSprite;

    [SerializeReference]
    public List<CardEffectBase> Effects = new();   // 複数効果を持てる

    [Range(0f, 100f)]
    public float LuckConversionRate = 20f;         // 砕いたとき回復する運ゲージ量

    public T GetEffect<T>() where T : CardEffectBase         // 最初に一致する効果を返す
    public List<T> GetEffects<T>() where T : CardEffectBase  // 一致する効果をすべて返す
}
```

**注意:** ScriptableObject は共有参照のため、手札の値は CardInstance 側で保持する。CardData 自体は不変。

---

## CardEffectBase

すべての効果に共通する確率フィールドと判定メソッドを定義する抽象クラス。

```csharp
public abstract class CardEffectBase : ScriptableObject
{
    [Range(0f, 1f)] public float ProbabilityMin = 1f;
    [Range(0f, 1f)] public float ProbabilityMax = 1f;
}
```

確率のロールと判定は CardInstance 側で一元管理する（後述）。  
`CardEffectBase` 自身の `RollProbability()` / `TryExecute()` は旧設計の名残で現在は使用しない。

---

## CardInstance

手札に追加された時点で生成し、ターン終了で破棄される。  
`OriginalData`（CardData）への参照と、ロール済みの数値配列を保持する。

### ロール

```csharp
var instance = new CardInstance(cardData);
instance.RollValues();          // ← 手札追加時に1度だけ呼ぶ
```

`RollValues()` 内では `Effects[i]` の型に応じて確率とダメージを確定する。

```
ProbabilityMin ～ ProbabilityMax → rolledProbabilities[i]  （isHotMode=true なら Max 固定）
DamageMin ～ DamageMax          → rolledDamages[i]         （DamageEffect のみ）
```

### 効果値の取得

```csharp
// 実効確率（基礎 + ボーナス、上限 1.0）
float prob = instance.GetEffectiveProbability(i);

// 実効ダメージ（基礎 + ボーナス、上限なし）
int dmg = instance.GetEffectiveDamage(i);
```

### 確率判定

```csharp
if (instance.TryExecuteEffect(i))
{
    // 成功 → ダメージ適用 or 状態異常付与
}
```

### 運ゲージによる強化

```csharp
instance.AddBonusProbability(i, bonus);   // 確率を加算（上限 1.0 は GetEffectiveProbability 内で保証）
instance.AddBonusDamage(i, bonus);        // ダメージを加算（上限なし）

// 上書きが必要な場合
instance.SetBonusProbability(i, bonus);
instance.SetBonusDamage(i, bonus);
```

---

## DamageEffect

```csharp
[CreateAssetMenu(menuName = "CardEffect/Damage")]
public class DamageEffect : CardEffectBase
{
    public int DamageMin = 3;
    public int DamageMax = 6;

    // ロール済み値・ボーナスは CardInstance 側で管理する
    // DamageEffect.BaseDamage / BonusDamage は旧設計の名残（使用しない）
}
```

ダメージ値の取得は `instance.GetEffectiveDamage(effectIndex)` 経由で行う。

---

## StatusEffect

```csharp
public enum StatusType { Shock, Burn, Poison }

[CreateAssetMenu(menuName = "CardEffect/Status")]
public class StatusEffect : CardEffectBase
{
    public StatusType StatusType;
    public int Duration = 2;     // 持続ターン数
    public int StackCount = 1;   // 重ね掛けスタック数
}
```

`Duration` / `StackCount` は CardData 定義値をそのまま使う（ロール対象外）。  
確率判定のみ `instance.TryExecuteEffect(i)` で行う。

---

## PlayZoneSlot との連携

プレイヤーがスロットにカードをドラッグすると `PlayZonePresenter.OnCardPlaced()` が呼ばれ、  
ロジック側の `PlayZoneSlot.PlayerCardInstance` に `CardInstance` が紐づく。

```
CardView（UI） ──drop──▶ PlayZoneSlotView.OnDrop()
                               │
                               ▼
                   PlayZonePresenter.OnCardPlaced(slotIndex, cardView)
                               │
                               ▼
                   PlayZoneSlot.PlayerCardInstance = instance   ← ロジック側
```

敵カードの場合は `PlayZoneSlot.EnemyCardData`（CardData）に直接セットされ、  
`CardInstance` は生成しない（敵カードはロール不要なため）。

---

## ライフサイクル

| タイミング | 処理 |
|---|---|
| ターン開始 | `DrawToFull()` で CardInstance 生成・`RollValues()` |
| プレイヤー操作 | ドラッグ→スロット配置で `PlayZoneSlot.PlayerCardInstance` にセット |
| ターン確定（未実装） | `ConfirmTurn()` で左から順に `TryExecuteEffect()` → 効果適用 |
| ターン終了 | `EndTurn()` で `hand` を `discardPile`（CardData）に戻し CardInstance を破棄 |
| デッキ切れ | `ShuffleDiscardToDeck()` で捨て札をシャッフルして補充 |

---

## 未実装・TODO

- `ConfirmTurn()` ― スロットのカードを順に解決して `CardResolveResult` を返す
- 運ゲージ連携 ― `SpendLuckForProbability()` / `SpendLuckForDamage()` （BattleLogic 内にコメントアウトあり）
- 激アツモード ― `isHotMode=true` 時は `RollValues(isHotMode: true)` で全値最大
- カード破砕演出 ― `CardView.PlayBreakAnimationAsync()` は実装済み、呼び出し側が未実装