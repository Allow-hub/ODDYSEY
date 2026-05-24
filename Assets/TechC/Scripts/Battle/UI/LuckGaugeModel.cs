using System;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// 運ゲージのロジックを管理する。
    ///
    /// 変更点：
    ///   - 激アツ開始しきい値（startThreshold）と解除しきい値（endThreshold）を分離。
    ///     100% で開始し、0% になるまで継続する。
    ///   - OnHotModeChanged イベントで BattleLogic に通知する。
    /// </summary>
    public class LuckGaugeModel
    {
        private float current;
        private readonly float max;

        /// <summary>激アツ開始しきい値（デフォルト100%）</summary>
        private readonly float startThreshold;

        /// <summary>激アツ解除しきい値（デフォルト0%）</summary>
        private readonly float endThreshold;

        private bool isHotMode = false;
        private bool prevHotMode = false;

        public float Current => current;
        public float Max => max;
        public float Ratio => current / max;

        /// <summary>
        /// 激アツ状態か。
        /// 開始：current >= startThreshold
        /// 解除：current <= endThreshold（一度開始した後）
        /// </summary>
        public bool IsHotMode => isHotMode;

        /// <summary>IsHotMode が変化したとき発火。引数は新しい値。</summary>
        public event Action<bool> OnHotModeChanged;

        public LuckGaugeModel(
            float max = 100f,
            float startThreshold = 100f,
            float endThreshold = 0f)
        {
            this.max = max;
            this.startThreshold = startThreshold;
            this.endThreshold = endThreshold;
            current = 0f;
            isHotMode = false;
            prevHotMode = false;
        }

        public void Add(float amount)
        {
            current = UnityEngine.Mathf.Clamp(current + amount, 0f, max);
            CheckHotModeChange();
        }

        public bool TrySpend(float amount)
        {
            if (current < amount) return false;
            current -= amount;
            CheckHotModeChange();
            return true;
        }

        public void TickDown(float amount = 10f)
        {
            current = UnityEngine.Mathf.Clamp(current - amount, 0f, max);
            CheckHotModeChange();
        }

        public void Reset()
        {
            current = 0f;
            isHotMode = false;
            CheckHotModeChange();
        }

        private void CheckHotModeChange()
        {
            // 激アツ開始：startThreshold 以上になったとき
            if (!isHotMode && current >= startThreshold)
                isHotMode = true;

            // 激アツ解除：endThreshold 以下になったとき
            if (isHotMode && current <= endThreshold)
                isHotMode = false;

            // 状態が変化したときだけイベント発火
            if (isHotMode != prevHotMode)
            {
                prevHotMode = isHotMode;
                OnHotModeChanged?.Invoke(isHotMode);
            }
        }
    }
}