using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// このターンに受ける敵ダメージを軽減する効果。
    /// 軽減率（%）を RollValues() でロールし、BattleLogic 側のターン軽減バッファに積む。
    /// rolledDamages[] に軽減率を格納するため GetEffectiveReductionRate() で取得する。
    /// </summary>
    [CreateAssetMenu(menuName = "CardEffect/Defense")]
    public class DefenseEffect : CardEffectBase
    {
        [Header("軽減率（範囲）%")]
        [Tooltip("ゲージなしのとき最低でもこの割合で軽減する")]
        [Range(0, 100)] public int ReductionMin = 20;

        [Tooltip("ゲージなしのとき最大でもこの割合で軽減する（ゲージで上限まで上昇）")]
        [Range(0, 100)] public int ReductionMax = 60;

        public override void Execute(EffectContext context, int effectIndex)
        {
            // DefenseEffect の確率は原則 100% だが、設計上の拡張余地を残して判定する
            if (!context.Source.TryExecuteEffect(effectIndex))
            {
                context.Result.IsHit = false;
                return;
            }

            int rate = context.Source.GetEffectiveReductionRate(effectIndex);

            // BattleLogic 側でこのターンの軽減率を保持させる
            context.Logic.SetDamageReduction(rate);

            context.Result.IsHit = true;
            context.Result.ReductionRate = rate;

            CustomLogger.Info(
                $"ダメージ軽減: {rate}% Slot:{context.SlotIndex}",
                LogTagUtil.TagCard);
        }
    }
}