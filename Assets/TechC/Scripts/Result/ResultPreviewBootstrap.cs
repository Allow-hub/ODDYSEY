using TechC.ODDESEY.Result;
using UnityEngine;

namespace TechC.ODDESEY.Reward
{
    /// <summary>
    /// Result prefab を単体確認するための作業 Scene 用 bootstrap。
    /// </summary>
    public sealed class ResultPreviewBootstrap : MonoBehaviour
    {
        [SerializeField] private ResultController resultController;
        [SerializeField] private bool initializeOnStart = true;
        [SerializeField] private bool isCleared = true;
        [SerializeField] private Rank previewRank = Rank.A;

        private void Reset()
        {
            resultController = FindAnyObjectByType<ResultController>();
        }

        private void OnEnable()
        {
            if (ResolveReferences())
            {
                resultController.OnResultClosed += HandleResultClosed;
            }
        }

        private void Start()
        {
            if (initializeOnStart)
            {
                ShowPreview();
            }
        }

        private void OnDisable()
        {
            if (resultController != null)
            {
                resultController.OnResultClosed -= HandleResultClosed;
            }
        }

        [ContextMenu("Show Preview")]
        public void ShowPreview()
        {
            if (!ResolveReferences())
            {
                return;
            }

            resultController.Initialize(CreatePreviewData());
        }

        [ContextMenu("Show Game Clear")]
        public void ShowGameClear()
        {
            isCleared = true;
            ShowPreview();
        }

        [ContextMenu("Show Game Over")]
        public void ShowGameOver()
        {
            isCleared = false;
            ShowPreview();
        }

        private bool ResolveReferences()
        {
            if (resultController == null)
            {
                resultController = FindAnyObjectByType<ResultController>();
            }

            if (resultController != null)
            {
                return true;
            }

            Debug.LogError("[ResultPreviewBootstrap] ResultController is not assigned.");
            return false;
        }

        private ResultData CreatePreviewData()
        {
            int totalScore = previewRank switch
            {
                Rank.S => 6500,
                Rank.A => 3500,
                Rank.B => 2000,
                Rank.C => 800,
                _ => 200,
            };

            int battleScore = Mathf.FloorToInt(totalScore * 0.6f);
            int eventScore = Mathf.FloorToInt(totalScore * 0.25f);

            var data = new ResultData { IsCleared = isCleared };
            data.Missions.Add(new MissionResult("バトル勝利", 3, battleScore));
            data.Missions.Add(new MissionResult("イベント達成", 2, eventScore));
            data.Missions.Add(new MissionResult("カード収集", 7, totalScore - battleScore - eventScore));
            return data;
        }

        private void HandleResultClosed()
        {
            Debug.Log("[ResultPreviewBootstrap] Close button pressed.");
        }
    }
}
