using TechC.ODDESEY.Result;
using TMPro;
using UnityEngine;

namespace TechC.ODDESEY.Reward
{
    /// <summary>
    /// ミッション結果1行分の表示。
    /// ResultView から動的に生成される。
    ///
    /// 表示例：バトル勝利×3 : +3000
    /// </summary>
    public class MissionRow : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI labelText;
        [SerializeField] private TextMeshProUGUI scoreText;

        public void Setup(MissionResult mission)
        {
            if (labelText != null)
                labelText.text = $"{mission.Label}×{mission.Count}";

            if (scoreText != null)
                scoreText.text = $"+{mission.ScoreGain:D4}";
        }
    }
}