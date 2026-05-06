using System;
using Cysharp.Threading.Tasks;
using TechC.Core.Manager;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// ヒットストップ（攻撃ヒット時の一時的なスロー）を管理する。
    ///
    /// 使い方：
    ///   HitStopManager.I.Play();             // デフォルト設定で再生
    ///   HitStopManager.I.Play(0.05f, 0.1f);  // timeScale=0.05 を 0.1秒
    ///
    /// 注意：
    ///   UniTask.Delay は TimeScale の影響を受けるため
    ///   UnscaledTime を使って実時間で待機する。
    /// </summary>
    public class HitStopManager : Singleton<HitStopManager>
    {
        [Header("ヒットストップ設定")]
        [Tooltip("ヒットストップ中の TimeScale")]
        [SerializeField, Range(0f, 1f)] private float hitStopTimeScale = 0.05f;

        [Tooltip("ヒットストップの継続時間（実時間・秒）")]
        [SerializeField] private float hitStopDuration = 0.08f;

        [Tooltip("ヒットストップ終了後に元に戻すときの補間時間（実時間・秒）。0 で即戻し。")]
        [SerializeField] private float recoveryDuration = 0.05f;

        /// <summary>ヒットストップ前の TimeScale を保持（他で変えている場合も対応）</summary>
        private float previousTimeScale = 1f;
        private bool isPlaying = false;
        protected override bool DontDestroy => base.DontDestroy;

        private void Awake()
        {
            Init();
        }

        // ─── 公開API ──────────────────────────────────────────────────────

        /// <summary>
        /// デフォルト設定でヒットストップを再生する。
        /// 既に再生中の場合は上書きしてリスタートする。
        /// </summary>
        public void Play() => Play(hitStopTimeScale, hitStopDuration, recoveryDuration);

        /// <summary>
        /// 引数でパラメータを上書きしてヒットストップを再生する。
        /// </summary>
        public void Play(float timeScale, float duration, float recovery = 0.05f)
        {
            PlayAsync(timeScale, duration, recovery).Forget();
        }

        // ─── 内部処理 ────────────────────────────────────────────────────

        private async UniTaskVoid PlayAsync(float timeScale, float duration, float recovery)
        {
            // 前回のヒットストップが残っていても上書き
            previousTimeScale = isPlaying ? previousTimeScale : Time.timeScale;
            isPlaying = true;

            // スロー開始
            Time.timeScale = timeScale;

            // 実時間で待機（UnscaledTime を使う）
            await UniTask.Delay(
                System.TimeSpan.FromSeconds(duration),
                ignoreTimeScale: true);

            // 回復フェーズ（滑らかに元のスケールへ）
            if (recovery > 0f)
            {
                float elapsed = 0f;
                float from = timeScale;

                while (elapsed < recovery)
                {
                    // Time.unscaledDeltaTime で実時間の経過を計測
                    elapsed += Time.unscaledDeltaTime;
                    Time.timeScale = Mathf.Lerp(from, previousTimeScale, elapsed / recovery);
                    await UniTask.Yield(PlayerLoopTiming.Update);
                }
            }

            Time.timeScale = previousTimeScale;
            isPlaying = false;
        }
    }
}