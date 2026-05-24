using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TechC.Core.Manager;

namespace TechC.ODDESEY.Title
{
    /// <summary>
    /// タイトル全体の管理を行うクラス
    /// </summary>
    public class TitleManager : MonoBehaviour
    {
        [SerializeField] private Button startButton;

        void Start()
        {
            if (startButton == null)
            {
                Debug.LogError("startButton が設定されていません");
                return;
            }

            startButton.onClick.AddListener(OnClickStart);
        }

        private void OnClickStart()
        {
            GameManager.I?.LoadSceneAsync(1); // シーンインデックス 1 をロード（BattleScene）
        }
    }
}
