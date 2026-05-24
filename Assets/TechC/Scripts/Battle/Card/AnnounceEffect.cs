using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// E-06「アポカリプス・コール / 審判準備」エフェクト。
    ///
    /// 効果：
    ///   確定発動。次ターンの予告フラグ（isAnnouncedNextTurn）を立てる。
    ///   ダメージなし。プレイヤーに「次ターンに大技が来る」と知らせる。
    ///
    /// EnemyActionPattern の条件分岐で AnnouncedAction を使うと
    ///   このカードの次ターンに必ず ジャッジメント が出るように設定できる。
    ///
    /// カード設定値（Inspector）:
    ///   ProbabilityMin : 1.0（確定）
    ///   ProbabilityMax : 1.0
    /// </summary>
    [CreateAssetMenu(menuName = "ODDESEY/CardEffect/Announce")]
    public class AnnounceEffect : CardEffectBase
    {
        // 確定発動なので確率・値の強化は意味なし
        public override bool CanBoostProbability => false;
        public override bool CanBoostValue       => false;

        public override void RollValue(EffectSlot slot, bool isHotMode)
        {
            slot.RolledProbability = 1f; // 確定発動
            slot.Value             = 0;
            slot.ValueRange        = (0, 0);
        }

        public override void Execute(EffectContext context, EffectExecutionState state, int effectIndex)
        {
            // 予告フラグを立てる
            context.Logic.SetAnnounced();

            state.PreviousEffectHadHitCheck = false;
            context.Result.IsHit            = true;
            context.Result.SetExtra(ResultKeys.IsAnnounced, true);

            CustomLogger.Info(
                "[審判準備] 次ターンに審判が来ることを予告した",
                LogTagUtil.TagCard);
        }
    }
}
