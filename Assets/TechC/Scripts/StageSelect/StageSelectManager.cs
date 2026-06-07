using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TechC.Core.Manager;

namespace TechC.ODDESEY.StageSelect
{
    /// <summary>
    /// ステージセレクト全体の管理を行うクラス
    /// </summary>
    public class StageSelectManager : MonoBehaviour
    {
        [SerializeField] private Button normalButton;
        [SerializeField] private Button hardButton;

        void Start()
        {
            AudioManager.I?.PlayBGM(BGMID.StageSelect);

            if (normalButton == null || hardButton == null)
            {
                Debug.LogError("Buttonが設定されていません");
                return;
            }

            normalButton.onClick.AddListener(StartNormal);
            hardButton.onClick.AddListener(StartHard);
        }

        private void StartNormal()
        {
            GameManager.I?.SetDifficulty(Difficulty.Normal);
            GameManager.I?.LoadSceneAsync(2); // シーンインデックス 2 をロード（MainScene）
        }

        public void StartHard()
        {
            GameManager.I.SetDifficulty(Difficulty.Hard);
            GameManager.I?.LoadSceneAsync(2); // シーンインデックス 2 をロード（MainScene）
        }
    }
}
