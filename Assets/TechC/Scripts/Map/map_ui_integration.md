# Map画面 UI組み込み資料

## 関連ファイル

- 作業用シーン：`Assets/TechC/Scenes/WorkScene/WorkScene_SR_Map.unity`
- 本番用UI Prefab：`Assets/TechC/Prefabs/Map/Map.prefab`
- ノードPrefab：`Assets/TechC/Prefabs/Map/NodeView.prefab`
- 選択ボタンPrefab：`Assets/TechC/Prefabs/Map/Choice.prefab`
- マップデータ：`Assets/TechC/Data/Map/StageMapData.asset`
- プレビュー初期化：`MapPreviewBootstrap`
- 本番制御：`MapController`

作業用シーンを開いてPlay Modeに入ると、`MapPreviewBootstrap`が設定された
`StageMapData`とプレビュー用の進行状態を使って`MapController.Initialize`を呼び出す。

`MapPreviewBootstrap`の`currentNodeIndex`を変更すると、通過済み・現在位置・選択可能・
未選択の各状態を確認できる。

表示するノードは`StageMapData.nodes`から実行時に自動生成されるため、
データ内のノード数を変更してもPrefab上でNodeViewを手動増減する必要はない。

## 本番での表示フロー

本番のマップ表示は次の順序で行われる。

1. `MainManager.EnterMap`
2. `Map.prefab`を生成
3. `MapController`の戦闘・イベント・ステージ完了イベントを購読
4. `MapController.Initialize(currentStageMapData, mapProgress)`を呼び出す

本番で使用されるレイアウトは`Map.prefab`である。
作業用シーン上で配置を調整した場合も、最終的な変更は`Map.prefab`へ反映する。

## ノードの表示仕様

- 現在位置：プレイヤー立ち絵をノードより前面に表示
- 選択可能：ノード内容に対応するタイルとアイコンを表示
- 未選択：`None`タイルと、ノード内容に対応するアイコンを表示
- 通過済み：`Off`タイルを表示し、アイコンを非表示
- ノード自体は選択操作を受け付けず、見た目の表示だけを担当

プレイヤー立ち絵は、立ち絵の下端がノード中央より少し下に来るよう配置する。
この位置は`MapNodeView`の`currentMarkerOffset`から調整できる。

## ノードの自動配置

ノードの列位置は`StageNodeData.nextNodeIndices`から自動計算される。

- 進行方向は横方向
- 同じ進行段階に1ノードだけある場合は基準線上へ配置
- 同じ進行段階に複数ノードがある場合は縦方向へ並べる
- 分岐ノードは通常位置を基準に、上下へ重ならないよう配置

現在の主な設定値は次のとおり。

- ノードサイズ：`100 x 100`
- 横方向の中心間隔：`220`
- 縦方向の中心間隔：`160`
- 表示領域：`1180 x 440`

各値は`Map.prefab`の`MapController`から調整できる。

- `nodeGap.x`：横方向のノード間隔
- `nodeGap.y`：分岐時の縦方向間隔
- `nodeSize`：ノードの基準サイズ
- `nodeContainerPadding`：スクロール端の余白

## スクロール表示

`Map.prefab`には横方向の`ScrollRect`である`MapScrollView`を常設している。

- マップ全体を一画面へ縮小せず、横スクロールで表示
- マップ更新時に現在位置付近へ自動でスクロール
- 移動後は現在位置まで滑らかにスクロール
- ノード数や横間隔に応じてContent幅を自動調整

調整項目：

- `mapScrollRect`：常設ScrollRectの参照
- `centerCurrentNodeOnRefresh`：更新時に現在位置へ合わせる
- `animateCurrentNodeScroll`：スクロール移動を補間する
- `currentNodeScrollDuration`：補間時間

表示範囲を変更する場合は、原則として`MapScrollView`のRectTransformを直接編集する。
互換用に、`mapScrollRect`が未設定の場合は実行時にScrollRectを生成する処理も残している。

## 選択ボタン

次に進めるノードの選択は、画面下部のボタンから行う。
マップ上のノードをクリックして選択する仕様ではない。

- `Choice.prefab`を選択可能なノード数だけ自動生成
- ボタンは`ChoiceButtonContainer`内に横並びで配置
- 背景には`Map_Button.png`を使用
- 表示文字は戦闘・回復・カード・危険・休憩
- 日本語対応フォント`FORMUDPGothic-Bold SDF`を使用
- 現在のボタンサイズは`320 x 108`
- 長い文言に備えてTextMesh Proの自動サイズ調整を有効化

`choiceButtonContainer`が未設定の場合は、画面下部中央へ一時的な横並びコンテナを
実行時に生成する。

## HP・運ゲージ

- HP表示：`Assets/TechC/Prefabs/UI/PlayerHpSlider.prefab`
- 運ゲージ：既存の`LuckGaugeView`

マップ表示時に`MainManager.I.GameContext`から現在HPと最大HPを取得する。
運ゲージは`MainManager.I.LuckGaugeValue`を表示する。

## 背景・共通ボタン

- マップ背景：`Assets/TechC/Images/Maps/Map_BackGround.png`
- 選択ボタン背景：`Assets/TechC/Images/Maps/Map_Button.png`
- 一時停止・設定ボタン：バトル画面と同じ画像を使用

一時停止・設定ボタンは既存の`PauseManager`へ入力を渡す。
設定画面を開く処理は既存実装が未完成のため、マップ側独自の機能追加は行っていない。

## データと表示の対応

- `StageNodeData.nodeType`：戦闘・イベント・休憩など、ノードの基本種別
- `StageNodeData.nextNodeIndices`：現在ノードから選択可能な次ノード
- `StageNodeData.EventData`：イベント内容とイベント用アイコン種別
- `StageNodeData.RewardData`：戦闘後のカード報酬データ
- `StageNodeData.EnemyData`：戦闘する敵データ
- `StageNodeData.IsBossNode`：ボス戦判定
- `MapProgressState.currentNodeIndex`：現在位置
- `MapProgressState.visitedNodeIndices`：通過済みノード

## Editor用セットアップ

大きなPrefab編集後に構造や参照を復元する場合は、Unityメニューから次を実行する。

`Tools/TechC/Map/Configure Map Prefab`

この処理は主に次の項目を設定する。

- 常設`MapScrollView`
- ノードContent
- 非表示の`NodeView_Template`
- 現在位置マーカー
- HP表示と運ゲージ
- 一時停止・設定ボタン
- `MapController`の必要参照

デザイナーが意図的に調整したレイアウト値を上書きする可能性があるため、
実行後は必ず`Map.prefab`の配置を確認する。

## 現在確認されている別系統の課題

以下はマップUIやマップデータ設定ではなく、戦闘・画面遷移側の課題。

- 通常戦闘勝利後がカード報酬画面ではなく、直接マップへ戻る設定になっている
- 敵撃破アニメーションを二重に待機しており、勝利通知まで進まない可能性がある
- 設定画面を開く共通処理が未実装

これらはマップUIの配置・ノード生成とは分けて扱う。
