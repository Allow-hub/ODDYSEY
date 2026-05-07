using TechC.ODDESEY.Battle;
using UnityEngine;
using TechC.ODDESEY.Reward;
using TechC.ODDESEY.Event;
using TechC.ODDESEY;
using TechC.ODDESEY.Map;

namespace TechC.Core.Manager
{
    public class MainManager : Singleton<MainManager>
    {
        [Header("プレハブ")]
        [SerializeField] private GameObject mapPrefab;
        [SerializeField] private GameObject battlePrefab;
        [SerializeField] private GameObject rewardPrefab;
        [SerializeField] private GameObject eventPrefab;

        [Header("ステージ定義（ScriptableObject）")]
        [SerializeField] private StageMapData currentStageMapData;

        public float LuckGaugeValue => luckGaugeValue;
        public int LuckGaugeMax => 100;
        private float luckGaugeValue = 0f;

        [Header("デバッグ用設定")]
        [SerializeField] private StartPhase debugStartPhase = StartPhase.Map;

        [Tooltip("debugStartPhase = Event のときに使う EventData")]
        [SerializeField] private EventData debugEventData; // ← 追加

        private GameObject currentPrefab;
        private MapController mapController;
        private BattleController battleController;
        private RewardController rewardController;
        private EventController eventController;

        private EventData pendingEventData;
        private readonly MapProgressState mapProgress = new();

        public GameContext GameContext => gameContext;
        [SerializeField] private DebugGameContext debugContext = new();
        private GameContext gameContext;

        public enum StartPhase { Map, Battle, Result, Event }

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

        private void EnterResult()
        {
            currentPrefab = Instantiate(rewardPrefab);
            rewardController = currentPrefab.GetComponent<RewardController>();

            rewardController.OnResultClosed += HandleResultClosed;
            rewardController.Initialize();
        }

        private void EnterEvent()
        {
            currentPrefab = Instantiate(eventPrefab);
            eventController = currentPrefab.GetComponent<EventController>();

            eventController.OnEventCompleted += HandleEventCompleted;

            // デバッグ直起動の場合は debugEventData を使う
            var data = pendingEventData ?? debugEventData;

            if (data == null)
                Debug.LogError("[MainManager] EventData が null です。debugEventData をアサインしてください。");

            eventController.Initialize(data);
        }

        // ─── ハンドラ ─────────────────────────────────────────────────────

        private void HandleBattleRequested() => EnterPhase(StartPhase.Battle);

        private void HandleEventRequested(EventData eventData)
        {
            pendingEventData = eventData;
            EnterPhase(StartPhase.Event);
        }

        private void HandleStageCompleted()
        {
            mapProgress.Reset();
            EnterPhase(StartPhase.Result);
        }

        private void HandleBattleWon() => EnterPhase(StartPhase.Map);
        private void HandleBattleLost() => EnterPhase(StartPhase.Result);
        private void HandleResultClosed() => EnterPhase(StartPhase.Map);

        private void HandleEventCompleted()
        {
            pendingEventData = null; // 使い終わったらリセット
            EnterPhase(StartPhase.Map);
        }

        public void SetLackGaugeValue(float value) => luckGaugeValue = Mathf.Clamp(value, 0f, 100f);

        /// <summary>
        /// カードをデッキに追加する。EventController の GainCard 結果から呼ばれる。
        /// </summary>
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
            if (rewardController != null)
            {
                rewardController.OnResultClosed -= HandleResultClosed;
                rewardController = null;
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