using TechC.ODDESEY.Core.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace TechC.ODDESEY.Map
{
    /// <summary>
    /// マップHUDのボタンを、ゲーム共通のポーズ・設定処理へ接続する。
    /// パネルの表示管理は既存のPauseManager/PauseViewに集約し、
    /// マップ側ではボタン入力の中継だけを担当する。
    /// </summary>
    public sealed class MapSystemButtons : MonoBehaviour
    {
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button settingButton;

        private void Awake()
        {
            pauseButton?.onClick.AddListener(OpenPause);
            settingButton?.onClick.AddListener(OpenSettings);
        }

        private void OnDestroy()
        {
            pauseButton?.onClick.RemoveListener(OpenPause);
            settingButton?.onClick.RemoveListener(OpenSettings);
        }

        private static void OpenPause()
        {
            PauseManager.I?.Pause();
        }

        private static void OpenSettings()
        {
            PauseManager.I?.OnOpenSettings();
        }
    }
}
