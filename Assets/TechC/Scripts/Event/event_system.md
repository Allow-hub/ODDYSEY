# イベントシステム 設計ドキュメント

## 概要

マップ上のイベントノードを選択した際に開くイベント画面の実装まとめ。  
「挑戦する / やめる」の2択で、運ゲージをリソースとして使う判断を発生させる。

---

## ファイル構成

```
TechC/Scripts/
├── Event/
│   ├── EventData.cs          ScriptableObject。イベント1件のデータ定義
│   ├── EventLogic.cs         純粋C#。ゲージ操作・成功判定・GameContext更新
│   ├── EventController.cs    MonoBehaviour。UIとロジックの橋渡し
│   └── EventView.cs          MonoBehaviour。画面表示・ボタン制御
├── Core/
│   └── GameContext.cs        ゲーム全体の状態（HP・ゲージ・デッキ）
└── Map/
    ├── StageNodeData.cs      マップノードの定義（EventDataを保持）
    └── MapController.cs      ノード選択時にEventDataをMainManagerへ渡す
```

---

## クラス責務

```
GameContext          ゲームの状態を持つ。HP・ゲージ・デッキの操作メソッドを提供
    ↓ 参照・変更
EventLogic           ゲージ計算・成功判定・GameContext更新（MainManager不依存）
    ↓ 委譲
EventController      Logic呼び出し・GainCardをMainManagerへ委譲・View更新
    ↓ 表示
EventView            イベント画面・結果画面の描画・長押しボタン制御
```

---

## データフロー

### イベント開始まで

```
MapController.OnNodeChoiceSelected(Event)
  └─ nodes[i].EventData を取得
       └─ OnEventRequested(eventData) を発火

MainManager.HandleEventRequested(eventData)
  └─ pendingEventData に保持
       └─ EnterPhase(Event)
            └─ EventController.Initialize(pendingEventData ?? debugEventData)
```

### ゲージ操作

```
EventView ボタン押下
  └─ EventController.OnAddGaugePressed()
       └─ EventLogic.TryAddReserved(1)
            ├─ CanAddReserved() チェック
            │    ├─ AvailableGauge >= 1（未予約残量）
            │    └─ FinalSuccessRate < 100
            └─ ReservedGauge += 1
       └─ EventController.RefreshView()
            └─ EventView.UpdateGaugeInfo(...)
```

### 挑戦フロー

```
EventView「挑戦する」押下
  └─ EventController.OnChallengePressed()
       └─ EventLogic.ChallengeAndApply()
            ├─ 1. successRate = FinalSuccessRate を保存（ReservedGauge リセット前）
            ├─ 2. consumedGauge = ReservedGauge
            ├─ 3. context.SpendGauge(ReservedGauge)
            ├─ 4. ReservedGauge = 0
            ├─ 5. roll = Random.Range(0, 100)
            │       success = roll < successRate
            ├─ 6. ApplyToContext(resultType, resultValue)
            │       HealHp / DamageHp / AddGauge / SpendGauge
            │       GainCard は返すだけ（EventControllerが処理）
            └─ 7. 失敗 && consumedGauge >= 1 → context.AddGauge(refund)

       ├─ result.ResultType == GainCard → MainManager.AddCardToContext()
       ├─ SyncGaugeToMainManager()
       └─ EventView.ShowResult(result, OnResultClosed)

OnResultClosed → CompleteEvent → OnEventCompleted → MainManager.EnterPhase(Map)
```

### やめるフロー

```
EventView「やめる」押下
  └─ EventController.OnCancelPressed()
       └─ EventLogic.Cancel()  // ReservedGauge = 0、消費なし
       └─ CompleteEvent() → OnEventCompleted → MainManager.EnterPhase(Map)
```

---

## EventData（ScriptableObject）

`Create > ODDESEY > EventData` で作成。

| フィールド | 型 | 説明 |
|---|---|---|
| `EventId` | string | 識別ID（例: E001） |
| `EventName` | string | 表示名 |
| `MapIconType` | enum | Card / Heal / Risk |
| `Description` | string | 説明文（TextArea） |
| `ChallengeText` | string | 挑戦ボタンのテキスト |
| `BaseSuccessRate` | int | 基礎成功率（0〜100） |
| `SuccessResultType` | enum | 成功時の結果タイプ |
| `SuccessResultValue` | int | 成功時の値 |
| `SuccessFlavorText` | string | 成功時のフレーバー |
| `FailureResultType` | enum | 失敗時の結果タイプ |
| `FailureResultValue` | int | 失敗時の値 |
| `FailureFlavorText` | string | 失敗時のフレーバー |
| `FailureGaugeRefund` | int | 失敗時還元量（デフォルト10） |

### EventResultType 一覧

| 値 | 内容 |
|---|---|
| `None` | 何も起きない |
| `HealHp` | HPを回復 |
| `DamageHp` | HPを減らす |
| `GainGauge` | 運ゲージを増やす |
| `LoseGauge` | 運ゲージを減らす |
| `GainCard` | カードを獲得（MainManagerに委譲） |

---

## EventLogic（純粋C#）

### プロパティ

| プロパティ | 説明 |
|---|---|
| `ReservedGauge` | 使用予定ゲージ量 |
| `FinalSuccessRate` | `BaseSuccessRate + ReservedGauge`（上限100） |
| `CurrentGauge` | `GameContext.LuckGauge` の整数値 |
| `AvailableGauge` | `CurrentGauge - ReservedGauge`（未予約残量） |

### 成功判定の実装

```csharp
// ReservedGauge をリセットする前に成功率を保存する
int successRate = FinalSuccessRate;

consumedGauge = ReservedGauge;
context.SpendGauge(ReservedGauge);
ReservedGauge = 0;

int roll = Random.Range(0, 100); // 0〜99
bool success = roll < successRate;
// successRate=100 → 常にtrue
// successRate=0   → 常にfalse
```

> **注意**：消費後に `FinalSuccessRate` を参照すると `ReservedGauge=0` になるため常に基礎成功率で判定されてしまう。必ず消費前に保存する。

### ゲージ追加の上限計算

```csharp
int maxByGauge       = AvailableGauge;               // 残ゲージを超えない
int maxBySuccessRate = 100 - FinalSuccessRate;        // 成功率100%を超えない
int maxAddable       = Mathf.Min(maxByGauge, maxBySuccessRate);
ReservedGauge += Mathf.Min(amount, maxAddable);
```

---

## GameContext のゲージ同期

運ゲージは `MainManager.LuckGaugeValue`（float）と `GameContext.LuckGauge`（float）の2か所に存在する。

| タイミング | 処理 |
|---|---|
| イベント開始時 | `context.LuckGauge = MainManager.I.LuckGaugeValue` で同期 |
| 挑戦・やめる完了時 | `MainManager.SetLackGaugeValue(context.LuckGauge)` で書き戻し |

---

## デバッグ方法

### イベントから直接起動する

1. `MainManager` の `Debug Start Phase` を `Event` に設定
2. `MainManager` の `Debug Event Data` に使いたい `EventData` をアサイン
3. `DebugGameContext` の `Luck Gauge` を `50` 程度に設定（ゲージ操作を確認するため）

### 確認チェックリスト

| 確認内容 | 期待動作 |
|---|---|
| イベント名・説明文が表示される | ✅ |
| 基礎成功率・最終成功率が表示される | ✅ |
| ゲージを使うボタンで成功率が +1% | ✅ |
| ゲージを戻すボタンで成功率が -1% | ✅ |
| 成功率100%でゲージ追加ボタンが押せない | ✅ |
| 使用予定0%でゲージ戻しボタンが押せない | ✅ |
| 成功率100%で挑戦すると必ず成功する | ✅ |
| 失敗 + ゲージ消費あり → 還元が表示される | ✅ |
| 失敗 + ゲージ消費なし → 還元が表示されない | ✅ |
| やめると何も起きずマップに戻る | ✅ |

---

## 既知の注意点・TODO

| 項目 | 内容 |
|---|---|
| `GainCard` の選択UI | 現状は `RewardCandidates` の先頭から自動追加。将来はカード選択画面を挟む |
| イベント案の追加 | 仕様書の E001〜E003 の ScriptableObject を作成してアサインする |
| マップアイコン | `EventMapIconType` を `NodeChoiceButton` でアイコン表示に使う（未実装） |
| 成功率の下限設定 | 仕様書では「成功率下限はイベントごとに設定」とあるが現状未実装 |
