using TechC.ODDESEY.Stage;
using TechC.ODDESEY.Battle;
using UnityEngine;
using TechC.ODDESEY.Reward;
using TechC.ODDESEY.Event;
using TechC.ODDESEY;

namespace TechC.Core.Manager
{

    /// <summary>
    /// MainScene の司令塔。
    /// Prefab の生成・破棄・切り替えを一手に担う。
    /// BattlePrefab / MapPrefab / rewardPrefab / EventPrefab を知っている唯一のクラス。
    /// </summary>
    public class MainManager : Singleton<MainManager>
    {
        [Header("プレハブ")]
        [SerializeField] private GameObject mapPrefab;
        [SerializeField] private GameObject battlePrefab;
        [SerializeField] private GameObject rewardPrefab;
        [SerializeField] private GameObject eventPrefab;

        [Header("デバッグ用設定")]
        [SerializeField] private StartPhase debugStartPhase = StartPhase.Map;

        private GameObject currentPrefab;
        private MapController stageController;
        private BattleController battleController;
        private RewardController rewardController;
        private EventController eventController;

        public GameContext GameContext => gameContext;
        [SerializeField] private DebugGameContext debugContext  = new();
        private GameContext gameContext;
        
        public enum StartPhase
        {
            Map,
            Battle,
            Result,
            Event,
        }

        protected override bool DontDestroy => false;
        protected override void OnInit()
        {
            base.OnInit();
            gameContext = debugContext.ToGameContext();
        }

        private void Awake()
        {
            Init();
        }
        
        private void Start()
        {
            EnterPhase(debugStartPhase);
        }

        /// <summary>
        /// 各フェーズ（ステージ選択・バトル・リザルト・イベント）へ遷移するためのメソッド。
        /// </summary>
        /// <param name="phase"></param>
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
        /// マップ選択肢
        /// </summary>
        private void EnterMap()
        {
            currentPrefab = Instantiate(mapPrefab);
            stageController = currentPrefab.GetComponent<MapController>();

            stageController.OnStageCompleted += HandleMapCompleted;
            stageController.OnBattleRequested += HandleBattleRequested;

            stageController.Initialize();
        }

        /// <summary>
        /// バトル開始
        /// </summary>
        private void EnterBattle()
        {
            currentPrefab = Instantiate(battlePrefab);
            battleController = currentPrefab.GetComponent<BattleController>();

            battleController.OnBattleWon += HandleBattleWon;
            battleController.OnBattleLost += HandleBattleLost;

            battleController.Initialize();
        }

        /// <summary>
        /// リザルト表示
        /// </summary>
        private void EnterResult()
        {
            currentPrefab = Instantiate(rewardPrefab);
            rewardController = currentPrefab.GetComponent<RewardController>();

            rewardController.OnResultClosed += HandleResultClosed;

            rewardController.Initialize();
        }

        /// <summary>
        /// イベント開始
        /// </summary>
        private void EnterEvent()
        {
            currentPrefab = Instantiate(eventPrefab);
            eventController = currentPrefab.GetComponent<EventController>();

            eventController.OnEventCompleted += HandleEventCompleted;

            eventController.Initialize();
        }

        private void HandleMapCompleted() => EnterPhase(StartPhase.Result);
        private void HandleBattleRequested() => EnterPhase(StartPhase.Battle);
        private void HandleBattleWon() => EnterPhase(StartPhase.Result);
        private void HandleBattleLost() => EnterPhase(StartPhase.Result); // 必要なら敗北フェーズへ分岐
        private void HandleResultClosed() => EnterPhase(StartPhase.Map);
        private void HandleEventCompleted() => EnterPhase(StartPhase.Map);

        /// <summary>
        /// クリーンアップ（フェーズ切り替え前に必ず呼ぶ）
        /// </summary>
        private void DestroyCurrentPrefab()
        {
            if (stageController != null)
            {
                stageController.OnStageCompleted -= HandleMapCompleted;
                stageController.OnBattleRequested -= HandleBattleRequested;
                stageController = null;
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