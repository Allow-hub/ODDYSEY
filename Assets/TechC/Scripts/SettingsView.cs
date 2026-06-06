using TechC.Core.Manager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TechC.ODDESEY
{
    public class SettingsView : MonoBehaviour
    {
        [Header("Master")]
        [SerializeField] private Slider masterSlider;
        [SerializeField] private TMP_Text masterText;

        [Header("BGM")]
        [SerializeField] private Slider bgmSlider;
        [SerializeField] private TMP_Text bgmText;

        [Header("SE")]
        [SerializeField] private Slider seSlider;
        [SerializeField] private TMP_Text seText;

        public void Init()
        {
            SetupSlider(masterSlider, masterText, AudioManager.I.masterVolume,
                v => AudioManager.I.SetMasterVolume(v));
            SetupSlider(bgmSlider, bgmText, AudioManager.I.bgmVolume,
                v => AudioManager.I.SetBGMVolume(v));
            SetupSlider(seSlider, seText, AudioManager.I.seVolume,
                v => AudioManager.I.SetSEVolume(v));
        }

        private void SetupSlider(Slider slider, TMP_Text label, float initialValue,
            System.Action<float> onChanged)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = initialValue;
            UpdateLabel(label, initialValue);

            slider.onValueChanged.AddListener(v =>
            {
                onChanged(v);
                UpdateLabel(label, v);
            });
        }

        private void UpdateLabel(TMP_Text label, float value)
        {
            label.text = $"{Mathf.RoundToInt(value * 100)}%";
        }
    }
}
