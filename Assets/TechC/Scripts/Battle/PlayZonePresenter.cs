using System.Collections.Generic;
using TechC.ODDESEY.Util;
using TechC.VBattle.Core.Extensions;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    public class PlayZonePresenter : MonoBehaviour
    {
        [Header("スロットの View（Inspector でアサイン / 順番がスロット番号）")]
        [SerializeField] private List<PlayZoneSlotView> slotViews;

        private PlayZoneSlot[] slots;

        // instanceId → スロット番号（配置追跡）
        private Dictionary<int, int> cardToSlotIndex = new();

        // instanceId → CardInstance（ロジック側への書き込み用）
        private Dictionary<int, CardInstance> cardInstanceMap = new();

        /// <summary>
        /// ターン開始時に BattleController から呼ぶ。
        /// </summary>
        public void SetupTurn(TurnData turnData, List<CardInstance> playerHand)
        {
            slots = turnData.PlayZone;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null)
                    slots[i] = new PlayZoneSlot();
            }
            cardToSlotIndex.Clear();
            cardInstanceMap.Clear();

            // 手札の CardInstance を instanceId でマッピング
            if (playerHand != null)
                foreach (var inst in playerHand)
                    cardInstanceMap[inst.InstanceId] = inst;

            for (int i = 0; i < slotViews.Count; i++)
            {
                int idx = i;
                slotViews[i].Setup(idx, OnCardPlaced, OnCardRemoved);
                slotViews[i].SetSlotData(i < slots.Length ? slots[i] : null);
            }
        }

        public void ClearAll()
        {
            foreach (var sv in slotViews) sv.ClearSlot();
            cardToSlotIndex.Clear();
        }

        /// <summary>
        /// カードがスロットから手札に戻るときのコールバック。BattleViewがCardViewにInjectして呼ぶ。
        /// カード配置確定後（isPlaced = true）に呼ばれる想定。
        /// </summary>
        /// <param name="cardView"></param>
        public void OnCardReturnRequested(CardView cardView)
        {
            // どのスロットに入っていたか逆引き
            if (!cardToSlotIndex.TryGetValue(cardView.InstanceId, out int slotIndex)) return;

            // 手札の親は cardView が元いた場所（ReturnToHand 前に取得が必要）
            slotViews[slotIndex].RemoveCard(cardView.OriginalParent);
            CustomLogger.Info($"カードがスロット {slotIndex} から手札に戻された: InstanceId {cardView.InstanceId}", LogTagUtil.TagCard);
        }

        /// <summary>
        /// カードがスロットに配置されたときのコールバック。PlayZoneSlotView から呼ぶ。
        /// </summary>
        /// <param name="slotIndex"></param>
        /// <param name="cardView"></param>
        private void OnCardPlaced(int slotIndex, CardView cardView)
        {
            if (slots == null || slotIndex >= slots.Length) return;

            var instance = ResolveCardInstance(cardView);
            if (instance == null) return;

            var slot = slots[slotIndex];
            slot.PlayerCardInstance = instance;
            slot.IsEnemyCard = false;

            cardToSlotIndex[cardView.InstanceId] = slotIndex;

            CustomLogger.Info($"カード配置確定: {instance.OriginalData.CardName} (InstanceId: {instance.InstanceId}) → Slot {slotIndex}", LogTagUtil.TagBattle);
        }

        /// <summary>
        /// カードがスロットから取り外されたときのコールバック。PlayZoneSlotView から呼ぶ。
        /// </summary>
        /// <param name="slotIndex"></param>
        private void OnCardRemoved(int slotIndex)
        {
            if (slots == null || slotIndex >= slots.Length) return;

            slots[slotIndex].Clear();

            // 追跡から削除
            foreach (var kv in cardToSlotIndex)
            {
                if (kv.Value == slotIndex)
                {
                    cardToSlotIndex.Remove(kv.Key);
                    break;
                }
            }
        }

        /// <summary>
        /// instanceId から CardInstance を返す。
        /// SetupTurn で登録されたマップを参照する。
        /// </summary>
        private CardInstance ResolveCardInstance(CardView cardView)
        {
            if (cardInstanceMap.TryGetValue(cardView.InstanceId, out var inst))
                return inst;

            CustomLogger.Warning($"CardInstance が見つからない: InstanceId {cardView.InstanceId}", LogTagUtil.TagBattle);
            return null;
        }

        public IReadOnlyList<PlayZoneSlot> GetFilledSlots()
        {
            var result = new List<PlayZoneSlot>();
            if (slots == null) return result;
            foreach (var slot in slots)
                if (slot.IsPlayerCard) result.Add(slot);
            return result;
        }

        public bool AllSlotsFilled()
        {
            if (slots == null) return false;
            foreach (var slot in slots)
                if (!slot.IsEnemyCard && slot.IsEmpty) return false;
            return true;
        }
    }
}