using System;
using System.Collections.Generic;
using TechC.ODDESEY.Result;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TechC.ODDESEY.Reward
{
    /// <summary>
    /// リザルト画面の表示を管理する View。
    ///
    /// Prefab 構成：
    ///   ResultView
    ///     ├─ clearPanel    （ゲームクリア時に表示）
    ///     │    ├─ titleText       「ゲームクリア」
    ///     │    ├─ subtitleText    「運命を打ち倒した」
    ///     │    ├─ rankText        「A」
    ///     │    ├─ scoreText       「スコア：0000000」
    ///     │    ├─ missionContainer（MissionRow を並べる親）
    ///     │    └─ closeButton     「タイトルに戻る」
    ///     └─ overPanel     （ゲームオーバー時に表示）
    ///          ├─ titleText
    ///          ├─ subtitleText
    ///          ├─ rankText
    ///          ├─ scoreText
    ///          ├─ missionContainer
    ///          └─ closeButton
    /// </summary>
    public class ResultView : MonoBehaviour
    {
        [Header("クリア画面")]
        [SerializeField] private GameObject clearPanel;
        [SerializeField] private TextMeshProUGUI clearRankText;
        [SerializeField] private TextMeshProUGUI clearScoreText;
        [SerializeField] private Transform clearMissionContainer;
        [SerializeField] private Button clearCloseButton;

        [Header("オーバー画面")]
        [SerializeField] private GameObject overPanel;
        [SerializeField] private TextMeshProUGUI overRankText;
        [SerializeField] private TextMeshProUGUI overScoreText;
        [SerializeField] private Transform overMissionContainer;
        [SerializeField] private Button overCloseButton;

        [Header("ミッション行 Prefab")]
        [SerializeField] private GameObject missionRowPrefab; // MissionRow コンポーネントをアタッチ

        [Header("ランク色設定")]
        [SerializeField] private Color rankColorS = new Color(1.0f, 0.85f, 0.0f); // 金
        [SerializeField] private Color rankColorA = new Color(0.8f, 0.2f, 1.0f); // 紫（画像参照）
        [SerializeField] private Color rankColorB = new Color(0.2f, 0.6f, 1.0f); // 青
        [SerializeField] private Color rankColorC = new Color(0.3f, 0.9f, 0.3f); // 緑
        [SerializeField] private Color rankColorD = new Color(0.6f, 0.6f, 0.6f); // グレー

        private readonly List<GameObject> spawnedRows = new();

        public void Setup(ResultData data, Action onClose)
        {
            bool isCleared = data.IsCleared;

            clearPanel.SetActive(isCleared);
            overPanel.SetActive(!isCleared);

            var rankText = isCleared ? clearRankText : overRankText;
            var scoreText = isCleared ? clearScoreText : overScoreText;
            var missionContainer = isCleared ? clearMissionContainer : overMissionContainer;
            var closeButton = isCleared ? clearCloseButton : overCloseButton;

            // ランク
            Rank rank = data.CalcRank();
            if (rankText != null)
            {
                rankText.text = isCleared ? rank.ToString() : "－";
                rankText.color = isCleared ? GetRankColor(rank) : rankColorD;
            }

            // スコア
            if (scoreText != null)
                scoreText.text = $"スコア：{data.TotalScore:D7}";

            // ミッション行を生成
            foreach (var row in spawnedRows) Destroy(row);
            spawnedRows.Clear();

            foreach (var mission in data.Missions)
            {
                var obj = Instantiate(missionRowPrefab, missionContainer);
                obj.GetComponent<MissionRow>()?.Setup(mission);
                spawnedRows.Add(obj);
            }

            // タイトルに戻るボタン
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(() => onClose?.Invoke());
            }
        }

        private Color GetRankColor(Rank rank) => rank switch
        {
            Rank.S => rankColorS,
            Rank.A => rankColorA,
            Rank.B => rankColorB,
            Rank.C => rankColorC,
            _ => rankColorD,
        };
    }
}