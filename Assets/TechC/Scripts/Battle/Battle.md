MainScene 構造まとめ
概要

MainScene は ゲーム全体の進行を管理するシーンであり、
MainManager が唯一の司令塔として各フェーズを制御する。

Prefab の生成・破棄
フェーズ遷移
各 Controller の接続

これらをすべて一元管理する。

構造（責務分離）
MainManager（中核）
シーン全体の管理クラス
唯一 Prefab を知っているクラス
フェーズ切り替えを担当
フェーズ構造

ゲームは以下の4フェーズで構成される：

Stage → Battle → Result → Stage
           ↓
         Event（分岐）
1. Stageフェーズ
マップ進行・次の行動選択
遷移：
バトル開始 → Battle
ステージ終了 → Result
2. Battleフェーズ
戦闘処理
遷移：
勝利 → Result
敗北 → Result（※将来的に分岐可能）
3. Resultフェーズ
報酬表示・結果処理
遷移：
閉じる → Stage
4. Eventフェーズ
特殊イベント
遷移：
イベント終了 → Stage
実行フロー
初期化
Start()
 ↓
EnterPhase(debugStartPhase)
フェーズ遷移の流れ
EnterPhase()
 ↓
DestroyCurrentPrefab()
 ↓
対応する EnterXXX() を実行
 ↓
Prefab生成
 ↓
Controller取得
 ↓
イベント登録
 ↓
Initialize()
各フェーズの処理構造
共通パターン
Prefab生成
 ↓
Controller取得
 ↓
イベント購読
 ↓
Initialize呼び出し
コールバックによる遷移

各Controllerからのイベントで遷移：

StageController
 ├ OnStageCompleted → Result
 └ OnBattleRequested → Battle

BattleController
 ├ OnBattleWon → Result
 └ OnBattleLost → Result

RewardController
 └ OnResultClosed → Stage

EventController
 └ OnEventCompleted → Stage
クリーンアップ処理

フェーズ遷移時は必ず実行：

DestroyCurrentPrefab()

内容：

イベント解除（メモリリーク防止）
Controller参照破棄
Prefab削除