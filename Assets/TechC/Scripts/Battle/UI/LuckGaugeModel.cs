namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// 運ゲージのロジックを管理する。BattleLogic が所有する。
    /// </summary>
    public class LuckGaugeModel
    {
        private float current;
        private readonly float max;
        private readonly float hotModeThreshold; // 激アツモードになるしきい値

        public float Current => current;
        public float Max => max;
        public float Ratio => current / max;
        public bool IsHotMode => current >= hotModeThreshold;

        public LuckGaugeModel(float max = 100f, float hotModeThreshold = 100f)
        {
            this.max = max;
            this.hotModeThreshold = hotModeThreshold;
            current = 0f;
        }

        /// <summary>ゲージを増やす（上限クランプ）</summary>
        public void Add(float amount) => current = UnityEngine.Mathf.Clamp(current + amount, 0f, max);

        /// <summary>ゲージを消費する。足りない場合は失敗してfalseを返す</summary>
        public bool TrySpend(float amount)
        {
            if (current < amount) return false;
            current -= amount;
            return true;
        }

        /// <summary>ターン終了時に自然減少</summary>
        public void TickDown(float amount = 10f)
        {
            current = UnityEngine.Mathf.Clamp(current - amount, 0f, max);
        }

        /// <summary>リセット</summary>
        public void Reset() => current = 0f;
    }
}