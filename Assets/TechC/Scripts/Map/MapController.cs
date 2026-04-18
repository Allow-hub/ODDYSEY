using System;
using TechC.ODDESEY.Battle;
using UnityEngine;

namespace TechC.ODDESEY.Stage
{
    /// <summary>
    /// ステージマップの管理。
    /// ノード選択UI・進行状態を担当し、次フェーズを MainManager に通知する。
    /// </summary>
    public class MapController : MonoBehaviour
    {
        [SerializeField] private LuckGaugeView luckGaugeView;


        public void Initialize()
        {
            luckGaugeView.Setup(100f);
        }

        // -------------------------------------------------------
        // マップ表示
        // -------------------------------------------------------

        private void ShowMap()
        {
        }
    }
}