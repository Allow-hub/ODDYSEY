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
        public void OnClickStart()
        {
            SceneManager.LoadScene("StageSelectScene");
        }
    }
}
