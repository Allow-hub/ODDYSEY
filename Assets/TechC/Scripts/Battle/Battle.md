# Battle システム概要
 
## アーキテクチャ
 
```
BattleController（MonoBehaviour）
  ├─ BattleLogic（純粋 C#）  ← ゲームロジック
  ├─ BattleView（MonoBehaviour） ← 表示・アニメーション
  └─ BreakZoneView（MonoBehaviour） ← カード破砕UI
```
 
| クラス | 役割 |
|---|---|
| `BattleController` | 司令塔。Logic と View を橋渡しし、UniTask で非同期フローを制御 |
| `BattleLogic` | HP・デッキ・プレイゾーン・LuckGauge などのステートを管理 |
| `BattleView` | フェードイン・カードアニメ・HP更新など演出一切を担当 |
| `BreakZoneView` | ドロップで受け取り、カード破砕 → LuckGauge 回復を通知 |
 
---
 
## バトル全体フロー
 
```
Initialize()
  │
  ├─ BattleLogic.StartBattle(GameContext)   // HP・デッキ・LuckGauge 初期化
  └─ RunBattleAsync() ループ開始
        │
        ▼
  ┌─────────────────────────────────────────────────┐
  │  while (IsBattleActive)                         │
  │                                                 │
  │  1. BeginTurn()                                 │
  │     ├─ DrawToFull()  デッキ → 手札              │
  │     └─ PlaceEnemyCards()  敵カード配置          │
  │                                                 │
  │  2. 演出                                        │
  │     ├─ 1ターン目: PlayBattleStartAsync()        │
  │     └─ 2ターン目以降: ShowTurnStartAsync()      │
  │                                                 │
  │  3. WaitForPlayerConfirmAsync()                 │
  │     └─ 「確定」ボタン押下まで待機               │
  │                                                 │
  │  4. ConfirmTurn()  カード解決（左→右）          │
  │     ├─ 各スロットの Effect を Execute()         │
  │     ├─ PlayCardResolveAsync()  攻撃アニメ       │
  │     ├─ UpdateEnemyHpAsync() / UpdatePlayerHpAsync() │
  │     └─ IsBattleEnd == true → 勝敗処理へ        │
  │                                                 │
  │  5. RemoveUsedCardsAsync()  使用カード除去      │
  │  6. EndTurn()  LuckGauge TickDown・スロットクリア│
  │  7. UpdateLuckGaugeAsync()  ゲージ表示更新      │
  └─────────────────────────────────────────────────┘
```
 
---
 
## ターン詳細
 
### 1. ターン開始（BeginTurn）
 
- `DrawToFull()` で手札を上限（5枚）まで補充する
  - デッキが尽きたら `ShuffleDiscardToDeck()` で捨て札をシャッフル・補充
  - `CardData` → `CardInstance` を生成し `RollValues()` で効果値を確定
- `PlaceEnemyCards()` で敵の配置戦略（`IEnemyCardPlacementStrategy`）に従いスロットへ敵カードを配置
- 結果を `TurnData` スナップショットにまとめて返す
### 2. プレイヤー操作
 
- `BattleView` が手札カードを UI に描画
- プレイヤーはカードをドラッグしてプレイゾーンのスロットへ配置
  - 配置: `PlayZoneSlot.PlayerCardInstance` に `CardInstance` がセットされる
  - 破砕: `BreakZoneView` へドロップ → LuckGauge 回復 → カード消滅
- 「確定」ボタンを押すと `WaitForPlayerConfirmAsync()` が解除され次フェーズへ
### 3. カード解決（ConfirmTurn）
 
- プレイゾーンをスロット 0 → 3 の順に走査
- 各スロットのカード（プレイヤー or 敵）に対して `Effect.Execute(context)` を呼ぶ
- `CardResolveResult` に解決結果（ダメージ量・HP残量・勝敗フラグ）を記録して返す
- `IsBattleActive` が `false` になった時点でループを抜け、勝敗演出へ
### 4. ターン終了（EndTurn）
 
- `LuckGauge.TickDown()` でゲージを自然減衰
- 各スロットをクリア（次ターンの敵カード配置に備える）
- 使用済みカードは `discardPile`（`CardData`）へ戻す
---
 
## 勝敗処理
 
```
IsBattleEnd == true
  │
  ├─ IsWon == true  → ShowWinEffectAsync()  → OnBattleWon?.Invoke()
  └─ IsWon == false → ShowLoseEffectAsync() → OnBattleLost?.Invoke()
```
 
`OnBattleWon` / `OnBattleLost` は `MainManager` が購読し、次のシーンへ遷移する。
 
---
 
## LuckGauge（運ゲージ）
 
| 操作 | タイミング |
|---|---|
| 初期値セット | `StartBattle()` で `MainManager` から引き継ぎ |
| 加算（`Add`） | カード破砕時（`BreakZoneView` → `BattleController.OnCardBroken`） |
| 自然減衰（`TickDown`） | ターン終了時（`EndTurn`） |
| 最終値保存 | バトル終了時に `MainManager.SetLackGaugeValue()` へ渡す |
| 激アツモード（`IsHotMode`） | ゲージが一定値を超えると有効化、カードの効果値が最大固定になる |
 
---
 
## カードシステムとの連携
 
```
CardData（ScriptableObject） ← 静的定義（不変）
  └─ CardEffectBase[]  DamageEffect / StatusEffect など
 
CardInstance（ランタイム）
  ├─ RollValues()        効果値をランダムに確定（手札追加時1回）
  ├─ TryExecuteEffect(i) 確率判定
  ├─ GetEffectiveDamage(i) 実効ダメージ取得
  └─ AddBonusProbability / AddBonusDamage  運ゲージによる強化
 
CardView（MonoBehaviour） ← ドラッグ操作・アニメーション
```
 
---
 
## イベント一覧
 
| イベント | 発火元 | 購読先 |
|---|---|---|
| `OnBattleWon` | `BattleController` | `MainManager` |
| `OnBattleLost` | `BattleController` | `MainManager` |
| `OnCardBroken` | `BreakZoneView` | `BattleController` |
 
---
 
## クラス間データフロー
 
```
GameContext ──StartBattle──▶ BattleLogic
                                  │
                          BeginTurn() : TurnData
                                  │
                  BattleController ──SetupTurn──▶ PlayZonePresenter
                                  │              ──UpdateHandAsync──▶ BattleView
                                  │
                          ConfirmTurn() : List<CardResolveResult>
                                  │
                  BattleController ──PlayCardResolveAsync──▶ BattleView
                                  └──UpdateEnemyHpAsync / UpdatePlayerHpAsync──▶ BattleView
```
 
---
 
## 未実装・TODO
 
- `ApplyStatusToEnemy` / `ApplyStatusToPlayer` — ステータス効果の適用処理が空
- 運ゲージ連携 — `SpendLuckForProbability()` / `SpendLuckForDamage()` （BattleLogic 内にコメントアウトあり）
- 激アツモード — `isHotMode=true` 時のカード演出切り替え（`TurnData.IsHotMode` は未使用）
- `enemyHp` の初期値がハードコード（`20`）— `GameContext` からの取得に変更が必要