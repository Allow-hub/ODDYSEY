using System.Collections;
using System.Collections.Generic;
using TechC.ODDESEY.Stage;
using TechC.ODDESEY.Battle;
using UnityEngine;
using TechC.ODDESEY.Reward;
using TechC.ODDESEY.Event;

namespace TechC.Core.Manager
{

    /// <summary>
    /// MainScene の司令塔。
    /// Prefab の生成・破棄・切り替えを一手に担う。
    /// BattlePrefab / StagePrefab / rewardPrefab / EventPrefab を知っている唯一のクラス。
    /// </summary>
    public class MainManager : Singleton<MainManager>
    {
        // -------------------------------------------------------
        // Inspector 設定
        // -------------------------------------------------------

        [Header("Prefab references")]
        [SerializeField] private GameObject stagePrefab;
        [SerializeField] private GameObject battlePrefab;
        [SerializeField] private GameObject rewardPrefab;
        [SerializeField] private GameObject eventPrefab;

        [Header("Debug")]
        [SerializeField] private StartPhase debugStartPhase = StartPhase.Stage;

        // -------------------------------------------------------
        // 内部状態
        // -------------------------------------------------------

        private GameObject currentPrefab;

        private StageController stageController;
        private BattleController battleController;
        private RewardController rewardController;
        private EventController eventController;

        // -------------------------------------------------------
        // フェーズ定義
        // -------------------------------------------------------

        public enum StartPhase
        {
            Stage,
            Battle,
            Result,
            Event,
        }

        protected override bool DontDestroy => false;
        protected override void OnInit()
        {
            base.OnInit();
        }

        // -------------------------------------------------------
        // ライフサイクル
        // -------------------------------------------------------

        private void Awake()
        {
            Init();
        }
        
        private void Start()
        {
            EnterPhase(debugStartPhase);
        }

        // -------------------------------------------------------
        // フェーズ切り替え
        // -------------------------------------------------------

        public void EnterPhase(StartPhase phase)
        {
            DestroyCurrentPrefab();

            switch (phase)
            {
                case StartPhase.Stage: EnterStage(); break;
                case StartPhase.Battle: EnterBattle(); break;
                case StartPhase.Result: EnterResult(); break;
                case StartPhase.Event: EnterEvent(); break;
            }
        }

        // -------------------------------------------------------
        // 各フェーズ開始
        // -------------------------------------------------------

        private void EnterStage()
        {
            currentPrefab = Instantiate(stagePrefab);
            stageController = currentPrefab.GetComponent<StageController>();

            stageController.OnStageCompleted += HandleStageCompleted;
            stageController.OnBattleRequested += HandleBattleRequested;

            stageController.Initialize();
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
        // コールバックハンドラ
        // -------------------------------------------------------

        private void HandleStageCompleted() => EnterPhase(StartPhase.Result);
        private void HandleBattleRequested() => EnterPhase(StartPhase.Battle);
        private void HandleBattleWon() => EnterPhase(StartPhase.Result);
        private void HandleBattleLost() => EnterPhase(StartPhase.Result); // 必要なら敗北フェーズへ分岐
        private void HandleResultClosed() => EnterPhase(StartPhase.Stage);
        private void HandleEventCompleted() => EnterPhase(StartPhase.Stage);

        // -------------------------------------------------------
        // クリーンアップ（フェーズ切り替え前に必ず呼ぶ）
        // -------------------------------------------------------

        private void DestroyCurrentPrefab()
        {
            if (stageController != null)
            {
                stageController.OnStageCompleted -= HandleStageCompleted;
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