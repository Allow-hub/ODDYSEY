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

        [Tooltip("このカメラに切り替えるときのブレンド時間（秒）。0 はカット切り替え。")]
        public float blendDuration = 0.5f;
    }

    /// <summary>
    /// カメラを管理するクラス。
    ///
    /// 変更点：
    ///   - VCamEntry に blendDuration を追加。
    ///   - SwitchTo() で CinemachineBrain.m_DefaultBlend を一時的に上書きして
    ///     ステートごとに異なるブレンド時間を実現する。
    /// </summary>
    public class CameraManager : Singleton<CameraManager>
    {
        [SerializeField] private List<VCamEntry> vcamEntries;
        [SerializeField] private CinemachineImpulseSource impulseSource;

        [SerializeField] private int activePriority = 20;
        [SerializeField] private int inactivePriority = 10;

        private CancellationTokenSource attackCts;
        private Dictionary<CameraState, CinemachineVirtualCamera> vcamMap;
        private Dictionary<CameraState, float> blendDurationMap;
        private CinemachineBrain brain;
        private CameraState currentState;

        private void Awake()
        {
            Init();
            BuildMap();
            brain = Camera.main?.GetComponent<CinemachineBrain>();
            SwitchTo(CameraState.Default);
        }

        private void BuildMap()
        {
            vcamMap       = new Dictionary<CameraState, CinemachineVirtualCamera>();
            blendDurationMap = new Dictionary<CameraState, float>();

            foreach (var entry in vcamEntries)
            {
                if (!vcamMap.ContainsKey(entry.state))
                {
                    vcamMap[entry.state]          = entry.vcam;
                    blendDurationMap[entry.state] = entry.blendDuration;
                }

                entry.vcam.Priority = inactivePriority;
                ForceSwitch(CameraState.Default);
            }
        }

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
        /// 指定した State の VCam に切り替える。
        /// 切り替え先の blendDuration を CinemachineBrain に反映してからプライオリティを変更する。
        /// </summary>
        public void SwitchTo(CameraState state)
        {
            if (currentState == state) return;
            if (!vcamMap.TryGetValue(state, out var nextVCam))
            {
                CustomLogger.Warning($"[CameraManager] VCam not found for state: {state}", LogTagUtil.TagCamera);
                return;
            }

            // 切り替え先のブレンド時間を Brain に適用
            ApplyBlendDuration(state);

            // 現在のカメラを下げる
            if (vcamMap.TryGetValue(currentState, out var currentVCam))
                currentVCam.Priority = inactivePriority;

            nextVCam.Priority = activePriority;
            currentState = state;
        }

        /// <summary>
        /// 攻撃カメラ演出を再生しアニメーション完了まで待つ。
        /// </summary>
        public async UniTask PlayAttackCameraAsync(AttackCameraData data)
        {
            if (data == null) return;

            attackCts?.Cancel();
            attackCts?.Dispose();
            attackCts = new CancellationTokenSource();
            var token = attackCts.Token;

            SwitchTo(data.onAttackState);

            var vcam     = GetCurrentVCam();
            var animator = vcam?.GetComponent<Animator>();

            if (animator != null)
            {
                animator.SetBool(data.AnimTriggerHash, true);

                await UniTask.Yield(token);

                await UniTask.WaitUntil(() =>
                    animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f
                    || animator.IsInTransition(0), cancellationToken: token);

                await UniTask.WaitUntil(() =>
                    animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f
                    && !animator.IsInTransition(0), cancellationToken: token);

                animator.SetBool(data.AnimTriggerHash, false);
            }

            await UniTask.WaitForSeconds(data.returnDelay, cancellationToken: token);
            SwitchTo(CameraState.Default);
        }

        // ─── 内部処理 ────────────────────────────────────────────────────

        /// <summary>
        /// 指定ステートの blendDuration を CinemachineBrain.m_DefaultBlend に適用する。
        /// </summary>
        private void ApplyBlendDuration(CameraState state)
        {
            if (brain == null) return;
            if (!blendDurationMap.TryGetValue(state, out float duration)) return;

            brain.m_DefaultBlend = new CinemachineBlendDefinition(
                duration > 0f
                    ? CinemachineBlendDefinition.Style.EaseInOut
                    : CinemachineBlendDefinition.Style.Cut,
                duration
            );
        }

        private CinemachineVirtualCamera GetCurrentVCam()
        {
            vcamMap.TryGetValue(currentState, out var vcam);
            return vcam;
        }

        public CameraState CurrentState => currentState;

        [ContextMenu("Switch/Default")]    void DbgDefault()     => SwitchTo(CameraState.Default);
        [ContextMenu("Switch/BattleStart")] void DbgBattleStart() => SwitchTo(CameraState.BattleStart);
        [ContextMenu("Switch/Aim")]        void DbgAim()         => SwitchTo(CameraState.Aim);
    }
}