# Card System（カードシステム）

## システム概要

カードは **CardData**（ScriptableObject）・**CardInstance**（ランタイムインスタンス）・**CardView**（UI）の3層で構成される。  
ターン開始時に `BattleLogic` がデッキから `CardInstance` を生成してロールし、`TurnData` 経由で View に渡す。

---

## クラス構成

```
CardData              ScriptableObject。カード1枚の静的定義。
  └─ CardEffectBase[] 効果リスト（各種 Effect クラス）

CardInstance          ランタイムオブジェクト。手札1枚を表す。
  └─ CardData への参照 + ロール済み値を保持

CardView              MonoBehaviour。手札UIとドラッグ操作を担当。
```

---

## 全体フロー

```
BattleLogic.DrawToFull()
  ├─ deck から CardData を取り出す
  ├─ new CardInstance(cardData)
  ├─ instance.RollValues()         // 効果値を確定（EvaluateAtResolve な効果は確率のみ）
  └─ hand に追加

BattleLogic.BeginTurn() → TurnData.Hand に CardInstance リストを入れて返す
  └─ PlayZonePresenter.SetupTurn(turnData, hand)

    （プレイヤーがカードをスロットにドラッグ）
  └─ PlayZonePresenter.OnCardPlaced(slotIndex, cardView)
      └─ PlayZoneSlot.PlayerCardInstance = instance

BattleLogic.ConfirmTurn()
  ├─ instance.EvaluateResolveValues(handCount)   // 手札連動など解決時評価を確定
  └─ Effects[0..n-1] を順に Execute()
      ├─ DamageEffect       → TryExecuteEffect → TakeEnemyDamage / TakePlayerDamage
      ├─ SelfDamageEffect   → Result.IsHit == false なら TakePlayerDamage（自傷）
      ├─ CriticalDamageEffect → 確定ダメージ + クリティカル判定 × 倍率
      ├─ DefenseEffect      → SetDamageReduction(rate)
      ├─ HandSizeDamageEffect → 手札枚数 × 乗数ダメージ
      └─ StatusEffect       → ApplyStatusToEnemy / ApplyStatusToPlayer
```

---

## CardEffectBase

すべての効果に共通する確率フィールドと `EvaluateAtResolve` フラグを定義する抽象クラス。

```csharp
public abstract class CardEffectBase : ScriptableObject
{
    [Range(0f, 1f)] public float ProbabilityMin = 1f;
    [Range(0f, 1f)] public float ProbabilityMax = 1f;

    // true のとき RollValues() では値を確定せず、解決タイミングで EvaluateResolveValues() が確定する
    public virtual bool EvaluateAtResolve => false;   // デフォルト false

    public abstract void Execute(EffectContext context, int effectIndex);
}
```

### EffectContext（拡張フィールド）

| フィールド | 型 | 用途 |
|---|---|---|
| Logic | BattleLogic | ダメージ・状態異常適用 |
| Source | CardInstance | ロール済み値の取得 |
| IsEnemy | bool | 敵カードかどうか |
| SlotIndex | int | 配置ボーナス判定 |
| CurrentHandCount | int | 手札連動ダメージの計算 |
| Result | CardResolveResult | 結果の書き込み |

---

## Effect クラス一覧

### DamageEffect（既存・拡張）
確率付きダメージ。配置ボーナスをフィールドで設定可能。

```csharp
[CreateAssetMenu(menuName = "CardEffect/Damage")]
public class DamageEffect : CardEffectBase
{
    public int DamageMin, DamageMax;

    // 先制特化などの配置ボーナス設定
    public bool UsePositionBonus = false;
    public int  RequiredSlotIndex = 0;      // 0 = 左端
    public int  PositionBonusDamage = 5;
}
```

**ScriptableObject 設定例（先制特化）**

| フィールド | 値 |
|---|---|
| DamageMin / Max | 5 / 7 |
| ProbabilityMin / Max | 0.70 / 0.80 |
| UsePositionBonus | true |
| RequiredSlotIndex | 0 |
| PositionBonusDamage | 5 |

---

### CriticalDamageEffect（新設）
確定ダメージ＋クリティカル確率でダメージ倍率。

```csharp
[CreateAssetMenu(menuName = "CardEffect/CriticalDamage")]
public class CriticalDamageEffect : CardEffectBase
{
    public int BaseDamageMin, BaseDamageMax;  // 確定部分
    public int CriticalMultiplier = 3;        // クリティカル時の倍率
    // ProbabilityMin / Max → クリティカル確率（ゲージで上昇）
}
```

**動作フロー**
```
RollValues() → rolledDamages[i] = BaseDamageMin〜Max をロール
Execute()    → damage = GetEffectiveDamage(i)
               isCrit = TryExecuteEffect(i)
               finalDamage = isCrit ? damage × Multiplier : damage
               → Result.IsCritical = isCrit
```

---

### DefenseEffect（新設）
このターンに受ける敵ダメージを軽減する。軽減率は `rolledDamages[]` に格納し `GetEffectiveReductionRate()` で取得。

```csharp
[CreateAssetMenu(menuName = "CardEffect/Defense")]
public class DefenseEffect : CardEffectBase
{
    [Range(0,100)] public int ReductionMin = 20;
    [Range(0,100)] public int ReductionMax = 60;
    // ProbabilityMin / Max は通常 1.0 固定（外れない）
    // ゲージで ReductionRate を上昇させる場合は AddBonusDamage() を流用
}
```

BattleLogic 側で `SetDamageReduction(rate)` を実装し、このターンの受けるダメージに乗算する。

---

### SelfDamageEffect（新設）
直前の効果が外れたとき（`Result.IsHit == false`）にプレイヤーが固定ダメージを受ける。捨て身カード専用。

```csharp
[CreateAssetMenu(menuName = "CardEffect/SelfDamage")]
public class SelfDamageEffect : CardEffectBase
{
    public int SelfDamage = 5;
    // ProbabilityMin / Max = 1.0 固定（判定不使用）
}
```

**CardData 設定（捨て身）**

```
Effects[0] = DamageEffect      ProbMin=0.60 ProbMax=0.80  DmgMin=10 DmgMax=25
Effects[1] = SelfDamageEffect  SelfDamage=5
```

---

### HandSizeDamageEffect（新設）
手札枚数 × 乗数のダメージ。解決タイミングで値を確定する（`EvaluateAtResolve = true`）。

```csharp
[CreateAssetMenu(menuName = "CardEffect/HandSizeDamage")]
public class HandSizeDamageEffect : CardEffectBase
{
    public int MultiplierMin = 2;
    public int MultiplierMax = 4;
    public override bool EvaluateAtResolve => true;
}
```

**ConfirmTurn() での呼び出し順**

```csharp
// 1. 解決時評価を確定（手札連動など）
instance.EvaluateResolveValues(hand.Count, isHotMode);

// 2. EffectContext に現在の手札枚数を渡す
var ctx = new EffectContext { ..., CurrentHandCount = hand.Count };

// 3. 各効果を実行
foreach (var effect in instance.OriginalData.Effects)
    effect.Execute(ctx, i);
```

---

### StatusEffect（既存・変更なし）
状態異常（Shock / Burn / Poison）を付与する。確率判定のみ `TryExecuteEffect()` 経由。

---

## CardInstance（主な変更点）

### RollValues() の拡張

`EvaluateAtResolve == true` な効果は確率のみロールし、ダメージ値はスキップする。  
`CriticalDamageEffect` と `DefenseEffect` も専用フィールドを参照してロールする。

### EvaluateResolveValues()（新設）

```csharp
instance.EvaluateResolveValues(handCount, isHotMode);
```

`HandSizeDamageEffect` など遅延評価効果の `rolledDamages[i]` をここで確定する。  
`ConfirmTurn()` の開始直後、各効果の `Execute()` を呼ぶ前に一度だけ呼ぶ。

### GetEffectiveReductionRate()（新設）

```csharp
int rate = instance.GetEffectiveReductionRate(effectIndex); // 0〜100
```

`DefenseEffect` の軽減率を取得する。内部的には `rolledDamages[]+bonusDamages[]` を `Clamp(0,100)` して返す。

---

## CardResolveResult（拡張フィールド）

| フィールド | 型 | 説明 |
|---|---|---|
| SelfDamageDealt | int | 自傷ダメージ量（捨て身） |
| IsCritical | bool | クリティカルが発生したか |
| ReductionRate | int | 軽減率 % (0=軽減なし) |

---

## BattleLogic 側で必要なメソッド（スタブ一覧）

| メソッド | 概要 |
|---|---|
| `TakePlayerDamage(int, CardResolveResult)` | 既存。自傷時も呼ぶ |
| `TakeEnemyDamage(int, CardResolveResult)` | 既存 |
| `ApplyStatusToPlayer(...)` | 既存 |
| `ApplyStatusToEnemy(...)` | 既存 |
| `SetDamageReduction(int rate)` | **新設**。このターンの受けダメージに乗算する軽減率を保持 |

---

## カード別 ScriptableObject 設定早見表

| カード名 | Effects 構成 | 変動値 |
|---|---|---|
| 高威力低確率 | DamageEffect × 1 | 確率・ダメージ |
| 低火力高確率 | DamageEffect × 1 | 確率・ダメージ |
| 軽減 | DefenseEffect × 1 | 軽減率（ゲージで上昇） |
| 捨て身 | DamageEffect → SelfDamageEffect | 確率・ダメージ |
| 先制特化 | DamageEffect（UsePositionBonus=true） × 1 | 確率・ダメージ |
| クリティカル | CriticalDamageEffect × 1 | 確定ダメージ・クリ確率 |
| 手札連動 | HandSizeDamageEffect × 1 | 確率・ダメージ乗数 |
| 感電付与（補助） | StatusEffect（Shock） × 1 | 確率 |
| 毒付与（補助） | StatusEffect（Poison） × 1 | 確率 |
| 燃焼付与（補助） | StatusEffect（Burn） × 1 | 確率 |

---

## 未実装・TODO

- `ConfirmTurn()` ― EvaluateResolveValues() + 各スロット順に Execute() → CardResolveResult を返す
- `BattleLogic.SetDamageReduction()` ― ターン軽減率バッファの実装
- 運ゲージ連携 ― `SpendLuckForProbability()` / `SpendLuckForDamage()` / `SpendLuckForReduction()`
- 激アツモード ― `RollValues(isHotMode: true)` ですべて最大値（DefenseEffect/CriticalDamageEffect/HandSizeDamageEffect も対応済み）
- ターンまたぎ補助カード（確率最大化・効果数値最大化）― 実装コスト要確認、採否未定
- カード破砕演出 ― `CardView.PlayBreakAnimationAsync()` は実装済み、呼び出し側が未実装