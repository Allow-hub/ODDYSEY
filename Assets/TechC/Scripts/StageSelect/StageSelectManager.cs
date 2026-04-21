using System.Collections;
using System.Collections.Generic;
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
            GameManager.I.SetDifficulty(Difficulty.Normal);
            SceneManager.LoadScene("MainScene");
            Debug.Log(GameManager.I.CurrentDifficulty);
        }

        public void StartHard()
        {
            GameManager.I.SetDifficulty(Difficulty.Hard);
            SceneManager.LoadScene("MainScene");
            Debug.Log(GameManager.I.CurrentDifficulty);
        }
    }
}
