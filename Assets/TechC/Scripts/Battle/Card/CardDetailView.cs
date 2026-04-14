using TechC.Core.Manager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// カードの詳細情報を表示するUI、カードをクリックしたときに出す
    /// </summary>
    public class CardDetailView : Singleton<CardDetailView>
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Button closeButton;

        protected override bool DontDestroy => false;

        private void Awake()
        {
            Init();
            Hide();
            closeButton.onClick.AddListener(Hide);
        }

        public void Show(CardData data)
        {
            canvasGroup.alpha = 1;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            nameText.text = data.CardName;
            // descriptionText.text = data.Description;
        }

        public void Hide()
        {
            canvasGroup.alpha = 0;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }
}
