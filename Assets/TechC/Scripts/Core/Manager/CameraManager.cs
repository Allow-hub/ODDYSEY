using System.Collections.Generic;
using System.Threading;
using Cinemachine;
using Cysharp.Threading.Tasks;
using TechC.Core.Manager;
using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using UnityEngine;

namespace TechC.ODDESEY.Core.Manager
{
    public enum CameraState
    {
        Default,
        PlayerAttack,
        EnemyAttack,
        BattleStart,
        Cutscene,
        Aim,
    }

    [System.Serializable]
    public class VCamEntry
    {
        public CameraState state;
        public CinemachineVirtualCamera vcam;
        [Range(0, 20)] public int defaultPriority = 10;
    }

    /// <summary>
    /// カメラを管理するクラス
    /// </summary>
    public class CameraManager : Singleton<CameraManager>
    {
        [SerializeField] private List<VCamEntry> vcamEntries;
        [SerializeField] private CinemachineImpulseSource impulseSource;

        [SerializeField] private int activePriority = 20;
        [SerializeField] private int inactivePriority = 10;
        private CancellationTokenSource attackCts;
        private Dictionary<CameraState, CinemachineVirtualCamera> vcamMap;
        private CameraState currentState;

        private void Awake()
        {
            Init();
            BuildMap();
            SwitchTo(CameraState.Default); // 最初は Default カメラに切り替える
        }

        private void BuildMap()
        {
            vcamMap = new Dictionary<CameraState, CinemachineVirtualCamera>();
            foreach (var entry in vcamEntries)
            {
                if (!vcamMap.ContainsKey(entry.state))
                    vcamMap[entry.state] = entry.vcam;

                // 最初は全て非アクティブ優先度に
                entry.vcam.Priority = inactivePriority;
                ForceSwitch(CameraState.Default);
            }
        }

        // ガード条件なしで切り替える
        private void ForceSwitch(CameraState state)
        {
            if (!vcamMap.TryGetValue(state, out var nextVCam))
            {
                CustomLogger.Warning($"[CameraManager] VCam not found for state: {state}", LogTagUtil.TagCamera);
                return;
            }

            nextVCam.Priority = activePriority;
            currentState = state;
        }
        /// <summary>
        /// 指定したStateのVCamに切り替える
        /// </summary>
        public void SwitchTo(CameraState state)
        {
            if (currentState == state) return;
            if (!vcamMap.TryGetValue(state, out var nextVCam))
            {
                CustomLogger.Warning($"[CameraManager] VCam not found for state: {state}", LogTagUtil.TagCamera);
                return;
            }

            // 現在のカメラを下げる
            if (vcamMap.TryGetValue(currentState, out var currentVCam))
                currentVCam.Priority = inactivePriority;

            // 次のカメラを上げる
            nextVCam.Priority = activePriority;
            currentState = state;
        }

        /// <summary>
        /// 攻撃カメラ演出を再生しアニメーション完了まで待つ
        /// </summary>
        public async UniTask PlayAttackCameraAsync(AttackCameraData data)
        {
            if (data == null) return;

            // 前回の演出が残っていればキャンセル
            attackCts?.Cancel();
            attackCts?.Dispose();
            attackCts = new CancellationTokenSource();
            var token = attackCts.Token;

            SwitchTo(data.onAttackState);

            var vcam = GetCurrentVCam();
            var animator = vcam?.GetComponent<Animator>();

            if (animator != null)
            {
                animator.SetBool(data.AnimTriggerHash, true);

                // 1フレーム待つ
                await UniTask.Yield(token);

                // Idle(normalizedTime >= 1f)を抜けるまで待つ
                await UniTask.WaitUntil(() =>
                    animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f
                    || animator.IsInTransition(0), cancellationToken: token);

                // 再生が終わるまで待つ
                await UniTask.WaitUntil(() =>
                    animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f
                    && !animator.IsInTransition(0), cancellationToken: token);

                animator.SetBool(data.AnimTriggerHash, false);
            }

            await UniTask.WaitForSeconds(data.returnDelay, cancellationToken: token);
            SwitchTo(CameraState.Default);
        }

        private CinemachineVirtualCamera GetCurrentVCam()
        {
            vcamMap.TryGetValue(currentState, out var vcam);
            return vcam;
        }

        /// <summary>
        /// 現在のStateを返す
        /// </summary>
        public CameraState CurrentState => currentState;

        // --- ContextMenu デバッグ用 ---
        [ContextMenu("Switch/Default")] void DbgDefault() => SwitchTo(CameraState.Default);
        [ContextMenu("Switch/BattleStart")] void DbgBattleStart() => SwitchTo(CameraState.BattleStart);
        [ContextMenu("Switch/Aim")] void DbgAim() => SwitchTo(CameraState.Aim);
    }
}