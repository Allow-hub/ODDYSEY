using System;
using System.Collections.Generic;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// 配置済みカードがクリックされた。
    /// ReturnToHand は呼ばない。受け手（PlayZonePresenter など）が判断する。
    /// </summary>
    public readonly struct CardPlacedClickedEvent
    {
        public readonly CardView Card;
        public readonly int InstanceId;
        public CardPlacedClickedEvent(CardView card) { Card = card; InstanceId = card.InstanceId; }
    }

    /// <summary>
    /// カードがスロットから手札へ戻された（ドラッグキャンセル含む）。
    /// </summary>
    public readonly struct CardReturnedToHandEvent
    {
        public readonly CardView Card;
        public readonly int InstanceId;
        public CardReturnedToHandEvent(CardView card) { Card = card; InstanceId = card.InstanceId; }
    }

    /// <summary>
    /// カードが砕かれた。
    /// </summary>
    public readonly struct CardBrokenEvent
    {
        public readonly CardView Card;
        public readonly float LuckGain;
        public CardBrokenEvent(CardView card, float luckGain) { Card = card; LuckGain = luckGain; }
    }

    /// <summary>
    /// 運ゲージの消費を要求する。
    /// PlayZoneView が発行 → BattleController が BattleLogic.TrySpendLuckGauge() に委譲する。
    ///
    /// 消費できたかどうかは OnResult コールバックで同期的に返る。
    /// PlayZoneView 側はコールバック内で「成功時だけ値を変更する」処理を書く。
    ///
    /// 使い方：
    ///   BattleEventBus.Publish(new LuckGaugeSpendRequestEvent(cost: 20f, onResult: success =>
    ///   {
    ///       if (success) ApplyDamageBonus();
    ///   }));
    /// </summary>
    public readonly struct LuckGaugeSpendRequestEvent
    {
        public readonly float Cost;

        /// <summary>消費結果。true = 成功 / false = ゲージ不足。BattleController が即座に呼ぶ。</summary>
        public readonly Action<bool> OnResult;

        public LuckGaugeSpendRequestEvent(float cost, Action<bool> onResult)
        {
            Cost = cost;
            OnResult = onResult;
        }
    }

    /// <summary>
    /// 運ゲージの現在値が変わったことを通知する。
    /// BattleController が LuckGaugeSpendRequestEvent を処理した後に発行し、
    /// LuckGaugeView がこれを購読して即時 UI を更新する。
    /// </summary>
    public readonly struct LuckGaugeChangedEvent
    {
        public readonly float Current;
        public readonly float Max;
        public readonly bool IsHotMode;

        public LuckGaugeChangedEvent(float current, float max, bool isHotMode)
        {
            Current = current;
            Max = max;
            IsHotMode = isHotMode;
        }
    }

    /// <summary>
    /// バトルシーン内で使うシンプルな静的 EventBus。
    /// UniRx 不要・型安全・購読解除は MonoBehaviour の OnDisable/OnDestroy で行う。
    ///
    /// 使い方:
    ///   // 購読
    ///   BattleEventBus.Subscribe&lt;CardBrokenEvent&gt;(OnCardBroken);
    ///   // 発行
    ///   BattleEventBus.Publish(new CardBrokenEvent(cardView, luckGain));
    ///   // 解除
    ///   BattleEventBus.Unsubscribe&lt;CardBrokenEvent&gt;(OnCardBroken);
    /// </summary>
    public static class BattleEventBus
    {
        private static readonly Dictionary<Type, Delegate> handlers = new();

        public static void Subscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            handlers[type] = handlers.TryGetValue(type, out var existing)
                ? Delegate.Combine(existing, handler)
                : handler;
        }

        public static void Unsubscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (handlers.TryGetValue(type, out var existing))
                handlers[type] = Delegate.Remove(existing, handler);
        }

        public static void Publish<T>(T ev)
        {
            if (handlers.TryGetValue(typeof(T), out var del))
                (del as Action<T>)?.Invoke(ev);
        }

        /// <summary>
        /// シーン切り替え時など、全ハンドラをまとめてクリアしたい場合に使う。
        /// </summary>
        public static void Clear() => handlers.Clear();
    }
}