using System;
using TechC.Core.Manager;
using TechC.ODDESEY.Result;
using UnityEngine;

namespace TechC.ODDESEY.Reward
{
    /// <summary>
    /// リザルト画面の管理。
    ///
    /// 責務：
    ///   - MainManager から ResultData を受け取り ResultView に渡す
    ///   - タイトルへ戻るボタンの完了を MainManager に通知
    /// </summary>
    public class ResultController : MonoBehaviour
    {
        public event Action OnResultClosed;

        [SerializeField] private ResultView resultView;

        public void Initialize(ResultData data)
        {
            if (data == null)
            {
                Debug.LogError("[ResultController] ResultData が null です。");
                return;
            }

            resultView.Setup(data, OnClosePressed);
        }

        private void OnClosePressed() => OnResultClosed?.Invoke();
    }
}