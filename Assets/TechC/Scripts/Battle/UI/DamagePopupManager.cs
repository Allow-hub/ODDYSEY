using System.Collections.Generic;
using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    public class DamagePopupManager : MonoBehaviour
    {
        [SerializeField] private DamagePopup popupPrefab;
        [SerializeField] private Canvas canvas;
        [SerializeField] private Transform poolRoot;
        [SerializeField] private int poolSize = 10;

        private Queue<DamagePopup> pool = new();

        private void Awake()
        {
            for (int i = 0; i < poolSize; i++)
            {
                var popup = Instantiate(popupPrefab, poolRoot);
                popup.gameObject.SetActive(false);
                pool.Enqueue(popup);
            }
        }

        /// <summary>
        /// ダメージまたは Miss ポップアップを表示する。
        /// isHit=false のとき "Miss" を表示する。
        /// </summary>
        public void Show(
            int damage,
            bool isHit,
            bool isPlayerDamage,
            bool isCritical,
            Vector3 worldPos)
        {
            var popup = GetFromPool();
            popup.transform.SetParent(canvas.transform, false);
            popup.Show(damage, isHit, isPlayerDamage, isCritical, worldPos, canvas);
        }

        private DamagePopup GetFromPool()
        {
            while (pool.Count > 0)
            {
                var candidate = pool.Dequeue();
                if (candidate != null && !candidate.gameObject.activeSelf)
                    return candidate;
            }
            var popup = Instantiate(popupPrefab, canvas.transform);
            popup.gameObject.SetActive(false);
            return popup;
        }
    }
}