using UnityEngine;

namespace TechC.Core.Manager
{
    /// <summary>
    /// ビルド時ログを画面に表示する
    /// </summary>
    public class DebugLogDisplay : MonoBehaviour
    {
        private const int MaxLogLines = 10; // 表示するログの最大行数
        private string logText = "";
        private GUIStyle guiStyle = new GUIStyle();
        private bool showLogInGame = false;

        private float lastLogTime = 0f;

        private void Start()
        {
            // ログのテキストをスタイルに設定
            guiStyle.fontSize = 20;
            guiStyle.normal.textColor = Color.white;

            // エディタで実行していない場合のみ、ゲーム画面内のログを表示
            showLogInGame = !Application.isEditor;
        }

        private void OnGUI()
        {
            // ゲーム画面中にログを表示（Windowsのビルド時のみ有効かつエディタで実行していない場合のみ有効かつ0キーで表示/非表示を切り替え）
#if UNITY_STANDALONE_WIN
            if (showLogInGame && Input.GetKeyDown(KeyCode.Alpha0))
            {
                showLogInGame = !showLogInGame;
            }

            if (showLogInGame)
            {
                GUI.Label(new Rect(10, 10, Screen.width, Screen.height), logText, guiStyle);
            }
#endif
        }

        private void OnEnable()
        {
            // デバッグログを表示するためのイベントハンドラを登録
            Application.logMessageReceived += HandleLog;
        }

        private void OnDisable()
        {
            // イベントハンドラを解除
            Application.logMessageReceived -= HandleLog;
        }

        private void Update()
        {
            // ゲーム画面内のログ表示が有効な場合のみ、3秒ごとにログのテキストをクリア
            if (showLogInGame && Time.time - lastLogTime > 3f)
            {
                logText = "";
            }
        }

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            // ログメッセージの種類に応じて色を変更
            string typeColor = type switch
            {
                LogType.Error => "<color=red>",
                LogType.Warning => "<color=yellow>",
                LogType.Log => "<color=white>",
                _ => "<color=gray>"
            };

            // スタックトレースの1行目（ファイル名と行番号）
            string[] stackLines = stackTrace.Split('\n');
            string sourceInfo = stackLines.Length > 1 ? stackLines[0] : "";

            // ログメッセージを構築
            string formattedLog = $"{typeColor}[{type}] {logString} <i>{sourceInfo}</i></color>\n";

            // ログテキストに追加
            logText += formattedLog;

            // 表示するログの行数がMaxLogLinesを超えたら、古いログを削除
            string[] logLines = logText.Split('\n');
            if (logLines.Length > MaxLogLines * 2) // カラータグで倍になることを考慮
            {
                logText = string.Join("\n", logLines, logLines.Length - MaxLogLines * 2, MaxLogLines * 2);
            }

            // 最後にログを表示した時刻を更新
            lastLogTime = Time.time;
        }

    }
}