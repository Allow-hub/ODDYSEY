using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
            SceneManager.LoadScene("StageSelectScene");
        }
    }
}
