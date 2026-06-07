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
        EnemyDied,
        PlayerDied,
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

    public class CameraManager : Singleton<CameraManager>
    {
        [SerializeField] private List<VCamEntry> vcamEntries;
        [SerializeField] private CinemachineImpulseSource impulseSource;
        [SerializeField] private int activePriority = 20;
        [SerializeField] private int inactivePriority = 10;

        private Dictionary<CameraState, CinemachineVirtualCamera> vcamMap;
        private Dictionary<CameraState, float> blendDurationMap;
        private CinemachineBrain brain;
        private CameraState currentState;

        private void Awake()
        {
            Init();
            BuildMap();
            brain = Camera.main?.GetComponent<CinemachineBrain>();
            SwitchTo(CameraState.BattleStart);
        }

        private void BuildMap()
        {
            vcamMap = new Dictionary<CameraState, CinemachineVirtualCamera>();
            blendDurationMap = new Dictionary<CameraState, float>();

            foreach (var entry in vcamEntries)
            {
                if (!vcamMap.ContainsKey(entry.state))
                {
                    vcamMap[entry.state] = entry.vcam;
                    blendDurationMap[entry.state] = entry.blendDuration;
                }
                entry.vcam.Priority = inactivePriority;
                ForceSwitch(CameraState.Default);
            }
        }

        private void ForceSwitch(CameraState state)
        {
            if (!vcamMap.TryGetValue(state, out var nextVCam)) return;
            nextVCam.Priority = activePriority;
            currentState = state;
        }

        public void SwitchTo(CameraState state)
        {
            if (currentState == state) return;
            if (!vcamMap.TryGetValue(state, out var nextVCam)) return;

            ApplyBlendDuration(state);

            if (vcamMap.TryGetValue(currentState, out var currentVCam))
                currentVCam.Priority = inactivePriority;

            nextVCam.Priority = activePriority;
            currentState = state;
        }

        public async UniTask SwitchToAndWaitBlendAsync(CameraState state)
        {
            SwitchTo(state);
            await UniTask.Yield(PlayerLoopTiming.Update);

            float elapsed = 0f;
            while (brain != null && brain.IsBlending && elapsed < 3f)
            {
                elapsed += Time.unscaledDeltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
        }

        public async UniTask ReturnToDefaultAsync()
        {
            SwitchTo(CameraState.Default);
            await UniTask.Yield(PlayerLoopTiming.Update);

            float elapsed = 0f;
            while (brain != null && brain.IsBlending && elapsed < 3f)
            {
                elapsed += Time.unscaledDeltaTime;
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
        }

        /// <summary>
        /// 攻撃カメラアニメーションを再生して完了まで待つ。
        ///
        /// 変更点：
        ///   normalizedTime による待機をやめ、
        ///   SetBool → 1フレーム待機 → 遷移開始を確認 → 遷移完了を確認
        ///   という順序に変更した。
        ///   これにより「カメラアニメが始まる前に完了判定してしまう」問題を解消する。
        /// </summary>
        public async UniTask PlayAttackCameraAsync(AttackCameraData data)
        {
            if (data == null) return;

            var vcam = GetCurrentVCam();
            var camAnimator = vcam?.GetComponent<Animator>();
            if (camAnimator == null) return;

            var localCts = new CancellationTokenSource();
            localCts.CancelAfter(System.TimeSpan.FromSeconds(10f));
            var token = localCts.Token;

            camAnimator.SetBool(data.AnimTriggerHash, true);
            try
            {
                await UniTask.WaitUntil(() => camAnimator.IsInTransition(0), cancellationToken: token);
                await UniTask.WaitUntil(() => !camAnimator.IsInTransition(0), cancellationToken: token);
                await UniTask.WaitUntil(
                    () => camAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f,
                    cancellationToken: token);
                await UniTask.WaitForSeconds(data.returnDelay, cancellationToken: token);
            }
            catch (System.OperationCanceledException)
            {
                CustomLogger.Warning("[CameraManager] PlayAttackCameraAsync タイムアウト", LogTagUtil.TagBattle);
            }
            finally
            {
                camAnimator.SetBool(data.AnimTriggerHash, false);
                localCts.Dispose();
            }
        }

        private void ApplyBlendDuration(CameraState state)
        {
            if (brain == null) return;
            if (!blendDurationMap.TryGetValue(state, out float duration)) return;

            brain.m_DefaultBlend = new CinemachineBlendDefinition(
                duration > 0f
                    ? CinemachineBlendDefinition.Style.EaseInOut
                    : CinemachineBlendDefinition.Style.Cut,
                duration);
        }

        private CinemachineVirtualCamera GetCurrentVCam()
        {
            vcamMap.TryGetValue(currentState, out var vcam);
            return vcam;
        }

        public CameraState CurrentState => currentState;

        [ContextMenu("Switch/Default")] void DbgDefault() => SwitchTo(CameraState.Default);
        [ContextMenu("Switch/BattleStart")] void DbgBattleStart() => SwitchTo(CameraState.BattleStart);
        [ContextMenu("Switch/Aim")] void DbgAim() => SwitchTo(CameraState.Aim);
    }
}