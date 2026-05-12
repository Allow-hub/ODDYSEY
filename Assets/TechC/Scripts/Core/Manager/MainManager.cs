using TechC.ODDESEY.Battle;
using UnityEngine;
using TechC.ODDESEY.Reward;
using TechC.ODDESEY.Event;
using TechC.ODDESEY;
using TechC.ODDESEY.Map;
using TechC.ODDESEY.Result;

namespace TechC.Core.Manager
{
    public class MainManager : Singleton<MainManager>
    {
        [Header("プレハブ")]
        [SerializeField] private GameObject mapPrefab;
        [SerializeField] private GameObject battlePrefab;
        [SerializeField] private GameObject rewardPrefab;
        [SerializeField] private GameObject cardRewardPrefab;
        [SerializeField] private GameObject eventPrefab;

        [Header("ステージ定義（ScriptableObject）")]
        [SerializeField] private StageMapData currentStageMapData;

        public float LuckGaugeValue => luckGaugeValue;
        public int LuckGaugeMax => 100;
        private float luckGaugeValue = 0f;

        [Header("デバッグ用設定")]
        [SerializeField] private StartPhase debugStartPhase = StartPhase.Map;
        [SerializeField] private EventData debugEventData;

        private GameObject currentPrefab;
        private MapController mapController;
        private BattleController battleController;
        private ResultController resultController;
        private CardRewardController cardRewardController;
        private EventController eventController;

        private BattleRewardData pendingRewardData;
        private bool pendingIsBoss;
        private EventData pendingEventData;

        // ─── ミッション集計 ───────────────────────────────────────────────
        private int battleWinCount = 0;
        // 将来のミッション追加はここにフィールドを追加する
        // private int eventClearCount = 0;

        private readonly MapProgressState mapProgress = new();

        public GameContext GameContext => gameContext;
        [SerializeField] private DebugGameContext debugContext = new();
        private GameContext gameContext;

        public enum StartPhase { Map, Battle, CardReward, Result, Event }

        protected override bool DontDestroy => false;

        protected override void OnInit()
        {
            base.OnInit();
            gameContext = debugContext.ToGameContext();
            luckGaugeValue = gameContext.LuckGauge;
        }

        private void Awake() => Init();
        private void Start() => EnterPhase(debugStartPhase);

        // ─── フェーズ遷移 ────────────────────────────────────────────────

        public void EnterPhase(StartPhase phase)
        {
            DestroyCurrentPrefab();
            switch (phase)
            {
                case StartPhase.Map: EnterMap(); break;
                case StartPhase.Battle: EnterBattle(); break;
                case StartPhase.CardReward: EnterCardReward(); break;
                case StartPhase.Result: EnterResult(); break;
                case StartPhase.Event: EnterEvent(); break;
            }
        }

        private void EnterMap()
        {
            currentPrefab = Instantiate(mapPrefab);
            mapController = currentPrefab.GetComponent<MapController>();

            mapController.OnBattleRequested += HandleBattleRequested;
            mapController.OnEventRequested += HandleEventRequested;
            mapController.OnStageCompleted += HandleStageCompleted;

            mapController.Initialize(currentStageMapData, mapProgress);
        }

        private void EnterBattle()
        {
            currentPrefab = Instantiate(battlePrefab);
            battleController = currentPrefab.GetComponent<BattleController>();

            battleController.OnBattleWon += HandleBattleWon;
            battleController.OnBattleLost += HandleBattleLost;

            battleController.Initialize();
        }

        private void EnterCardReward()
        {
            currentPrefab = Instantiate(cardRewardPrefab);
            cardRewardController = currentPrefab.GetComponent<CardRewardController>();

            cardRewardController.OnRewardCompleted += HandleCardRewardCompleted;
            cardRewardController.Initialize(pendingRewardData);
        }

        private void EnterResult()
        {
            currentPrefab = Instantiate(rewardPrefab);
            resultController = currentPrefab.GetComponent<ResultController>();

            resultController.OnResultClosed += HandleResultClosed;
            resultController.Initialize(BuildResultData(isCleared: pendingIsBoss));
        }

        private void EnterEvent()
        {
            currentPrefab = Instantiate(eventPrefab);
            eventController = currentPrefab.GetComponent<EventController>();

            eventController.OnEventCompleted += HandleEventCompleted;

            var data = pendingEventData ?? debugEventData;
            if (data == null)
                Debug.LogError("[MainManager] EventData が null です。");

            eventController.Initialize(data);
        }

        // ─── ハンドラ ─────────────────────────────────────────────────────

        private void HandleBattleRequested(BattleRewardData rewardData, bool isBoss)
        {
            pendingRewardData = rewardData;
            pendingIsBoss = isBoss;
            EnterPhase(StartPhase.Battle);
        }

        private void HandleEventRequested(EventData eventData)
        {
            pendingEventData = eventData;
            EnterPhase(StartPhase.Event);
        }

        private void HandleStageCompleted()
        {
            mapProgress.Reset();
            // ステージ完全クリア → ボスクリア扱いでリザルトへ
            pendingIsBoss = true;
            EnterPhase(StartPhase.Result);
        }

        private void HandleBattleWon()
        {
            SetLackGaugeValue(luckGaugeValue);
            battleWinCount++;

            if (pendingIsBoss)
                EnterPhase(StartPhase.Result);
            else
                EnterPhase(StartPhase.CardReward);
        }

        private void HandleBattleLost()
        {
            pendingIsBoss = false; // 敗北時はクリアではない
            EnterPhase(StartPhase.Result);
        }

        private void HandleCardRewardCompleted()
        {
            pendingRewardData = null;
            EnterPhase(StartPhase.Map);
        }

        private void HandleResultClosed()
        {
            // ランへ戻る（タイトルへの遷移は将来実装）
            ResetRunStats();
            EnterPhase(StartPhase.Map);
        }

        private void HandleEventCompleted()
        {
            pendingEventData = null;
            EnterPhase(StartPhase.Map);
        }

        // ─── ResultData の組み立て ────────────────────────────────────────

        /// <summary>
        /// 現在のミッション集計から ResultData を組み立てる。
        /// 将来ミッションが増えたら Missions.Add を追加するだけ。
        /// </summary>
        private ResultData BuildResultData(bool isCleared)
        {
            var data = new ResultData { IsCleared = isCleared };

            // バトル勝利ミッション
            data.Missions.Add(new MissionResult(
                label: "バトル勝利",
                count: battleWinCount,
                scoreGain: battleWinCount * MissionScore.PerBattleWin
            ));

            // 将来のミッション追加例：
            // data.Missions.Add(new MissionResult(
            //     label:     "イベントクリア",
            //     count:     eventClearCount,
            //     scoreGain: eventClearCount * MissionScore.PerEventClear
            // ));

            return data;
        }

        /// <summary>ラン終了時にミッション集計をリセットする</summary>
        private void ResetRunStats()
        {
            battleWinCount = 0;
            mapProgress.Reset();
            // 将来追加されるカウンターもここでリセット
        }

        // ─── ユーティリティ ───────────────────────────────────────────────

        public void SetLackGaugeValue(float value) => luckGaugeValue = Mathf.Clamp(value, 0f, 100f);

        public void AddCardToContext(int count)
        {
            var context = GameContext;
            if (context?.RewardCandidates == null) return;

            int added = 0;
            foreach (var card in context.RewardCandidates)
            {
                if (added >= count) break;
                context.AddCard(card);
                added++;
            }
        }

        // ─── クリーンアップ ───────────────────────────────────────────────

        private void DestroyCurrentPrefab()
        {
            if (mapController != null)
            {
                mapController.OnBattleRequested -= HandleBattleRequested;
                mapController.OnEventRequested -= HandleEventRequested;
                mapController.OnStageCompleted -= HandleStageCompleted;
                mapController = null;
            }
            if (battleController != null)
            {
                battleController.OnBattleWon -= HandleBattleWon;
                battleController.OnBattleLost -= HandleBattleLost;
                battleController = null;
            }
            if (cardRewardController != null)
            {
                cardRewardController.OnRewardCompleted -= HandleCardRewardCompleted;
                cardRewardController = null;
            }
            if (resultController != null)
            {
                resultController.OnResultClosed -= HandleResultClosed;
                resultController = null;
            }
            if (eventController != null)
            {
                eventController.OnEventCompleted -= HandleEventCompleted;
                eventController = null;
            }
            if (currentPrefab != null)
            {
                Destroy(currentPrefab);
                currentPrefab = null;
            }
        }
    }
}