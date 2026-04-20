using TechC.ODDESEY.Battle;
using UnityEngine;
using TechC.ODDESEY.Reward;
using TechC.ODDESEY.Event;
using TechC.ODDESEY;
using TechC.ODDESEY.Map;

namespace TechC.Core.Manager
{
    /// <summary>
    /// MainScene の司令塔。
    /// Prefab の生成・破棄・切り替えを一手に担う。
    /// BattlePrefab / MapPrefab / rewardPrefab / EventPrefab を知っている唯一のクラス。
    /// 
    /// マップの進行状態（MapProgressState）はここで保持し、
    /// Prefab 再生成のたびに MapController へ渡して復元させる。
    /// </summary>
    public class MainManager : Singleton<MainManager>
    {
        // -------------------------------------------------------
        // Inspector
        // -------------------------------------------------------
        [Header("プレハブ")]
        [SerializeField] private GameObject mapPrefab;
        [SerializeField] private GameObject battlePrefab;
        [SerializeField] private GameObject rewardPrefab;
        [SerializeField] private GameObject eventPrefab;

        [Header("ステージ定義（ScriptableObject）")]
        [SerializeField] private StageMapData currentStageMapData;

        public float LuckGaugeValue => lackGaugeValue;
        private float lackGaugeValue = 0f;

        [Header("デバッグ用設定")]
        [SerializeField] private StartPhase debugStartPhase = StartPhase.Map;

        // -------------------------------------------------------
        // 内部フィールド
        // -------------------------------------------------------
        private GameObject currentPrefab;
        private MapController mapController;
        private BattleController battleController;
        private RewardController rewardController;
        private EventController eventController;

        /// <summary>
        /// マップの進行状態。Prefab が破棄されても保持し続ける。
        /// </summary>
        private readonly MapProgressState mapProgress = new();

        public GameContext GameContext => gameContext;
        [SerializeField] private DebugGameContext debugContext = new();
        private GameContext gameContext;

        // -------------------------------------------------------
        // フェーズ定義
        // -------------------------------------------------------
        public enum StartPhase
        {
            Map,
            Battle,
            Result,
            Event,
        }

        // -------------------------------------------------------
        // 初期化
        // -------------------------------------------------------
        protected override bool DontDestroy => false;

        protected override void OnInit()
        {
            base.OnInit();
            gameContext = debugContext.ToGameContext();
        }

        private void Awake() => Init();

        private void Start() => EnterPhase(debugStartPhase);

        // -------------------------------------------------------
        // フェーズ遷移
        // -------------------------------------------------------

        /// <summary>
        /// 各フェーズへ遷移する。
        /// </summary>
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

        /// <summary>
        /// マップへ遷移。進行状態を復元して渡す。
        /// </summary>
        private void EnterMap()
        {
            currentPrefab = Instantiate(mapPrefab);
            mapController = currentPrefab.GetComponent<MapController>();

            mapController.OnBattleRequested += HandleBattleRequested;
            mapController.OnEventRequested += HandleEventRequested;
            mapController.OnStageCompleted += HandleStageCompleted;

            // 進行状態を渡して復元
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

            eventController.Initialize();
        }

        // -------------------------------------------------------
        // ハンドラ
        // -------------------------------------------------------
        private void HandleBattleRequested() => EnterPhase(StartPhase.Battle);
        private void HandleEventRequested() => EnterPhase(StartPhase.Event);
        private void HandleStageCompleted()
        {
            // ステージクリア → リザルトへ。次回のために進行状態をリセット。
            mapProgress.Reset();
            EnterPhase(StartPhase.Result);
        }

        private void HandleBattleWon() => EnterPhase(StartPhase.Map); // マップに戻り次ノードへ
        private void HandleBattleLost() => EnterPhase(StartPhase.Result);

        private void HandleResultClosed() => EnterPhase(StartPhase.Map);
        private void HandleEventCompleted() => EnterPhase(StartPhase.Map);

        public void SetLackGaugeValue(float value) => lackGaugeValue = Mathf.Clamp(value, 0f, 100f);

        // -------------------------------------------------------
        // クリーンアップ
        // -------------------------------------------------------
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