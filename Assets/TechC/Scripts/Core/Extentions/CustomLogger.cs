using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace TechC.VBattle.Core.Extensions
{
    /// <summary>
    /// Unity向け拡張ログシステム（永続設定対応版）
    /// </summary>
    public static class CustomLogger
    {
        /// <summary>
        /// ログレベルを定義
        /// </summary>
        public enum LogLevel
        {
            Info,
            Warning,
            Error
        }

        // ログレベルごとの表示設定
        private static Dictionary<LogLevel, bool> _levelEnabled = new Dictionary<LogLevel, bool>();

        // 全体的なログ表示設定
        private static bool _loggingEnabled = true;
        public static bool _timeEnabled = false;

        // 静的コンストラクタでデフォルト設定を初期化
        static CustomLogger()
        {
            // 初期設定
            InitializeSettings();
        }

        /// <summary>
        /// 設定の初期化
        /// </summary>
        public static void InitializeSettings()
        {
            // すべてのログレベルを初期化でオンに
            foreach (LogLevel level in Enum.GetValues(typeof(LogLevel)))
            {
                _levelEnabled[level] = true;
            }
        }

        /// <summary>
        /// 全ログの有効/無効を設定
        /// </summary>
        /// <param name="enable"></param>
        public static void EnableLogging(bool enable)
        {
            _loggingEnabled = enable;
        }
        /// <summary>
        /// 全ログの有効/無効を設定
        /// </summary>
        /// <param name="enable"></param>
        public static void TimeLogging(bool enable)
        {
            _timeEnabled = enable;
        }
        /// <summary>
        /// 特定カテゴリのログ有効/無効を設定
        /// </summary>
        /// <param name="categoryId"></param>
        /// <param name="enable"></param>
        public static void EnableCategory(string categoryId, bool enable)
        {
            if (LoggerSettings.Instance.CategoryExists(categoryId))
            {
                LoggerSettings.Instance.SetCategoryEnabled(categoryId, enable);
            }
            else
            {
                UnityEngine.Debug.LogWarning($"Unknown log category: {categoryId}");
            }
        }

        /// <summary>
        /// LogCategoryDefinitionを使用してカテゴリを有効/無効化
        /// </summary>
        /// <param name="category"></param>
        /// <param name="enable"></param>
        public static void EnableCategory(LogCategoryDefinition category, bool enable)
        {
            if (category != null)
            {
                category.SetEnabled(enable);
            }
        }

        /// <summary>
        /// 特定ログレベルの有効/無効を設定
        /// </summary>
        /// <param name="level"></param>
        /// <param name="enable"></param>
        public static void EnableLevel(LogLevel level, bool enable)
        {
            _levelEnabled[level] = enable;
        }

        /// <summary>
        /// Infoログを出力（カテゴリID指定）
        /// </summary>
        /// <param name="message"></param>
        /// <param name="categoryId"></param>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Info(string message, string categoryId = null)
        {
            Log(message, LogLevel.Info, categoryId);
        }

        /// <summary>
        /// Infoログを出力（カテゴリオブジェクト指定）
        /// </summary>
        /// <param name="message"></param>
        /// <param name="category"></param>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Info(string message, LogCategoryDefinition category)
        {
            Log(message, LogLevel.Info, category?.categoryId);
        }

        /// <summary>
        /// Warningログを出力（カテゴリID指定）
        /// </summary>
        /// <param name="message"></param>
        /// <param name="categoryId"></param>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Warning(string message, string categoryId = null)
        {
            Log(message, LogLevel.Warning, categoryId);
        }

        /// <summary>
        /// Warningログを出力（カテゴリオブジェクト指定）
        /// </summary>
        /// <param name="message"></param>
        /// <param name="category"></param>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Warning(string message, LogCategoryDefinition category)
        {
            Log(message, LogLevel.Warning, category?.categoryId);
        }

        /// <summary>
        /// Errorログを出力（カテゴリID指定）
        /// </summary>
        /// <param name="message"></param>
        /// <param name="categoryId"></param>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]

        public static void Error(string message, string categoryId = null)
        {
            Log(message, LogLevel.Error, categoryId);
        }

        /// <summary>
        /// Errorログを出力（カテゴリオブジェクト指定）
        /// </summary>
        /// <param name="message"></param>
        /// <param name="category"></param>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Error(string message, LogCategoryDefinition category)
        {
            Log(message, LogLevel.Error, category?.categoryId);
        }

        /// <summary>
        /// 内部ログ処理
        /// </summary>
        /// <param name="message"></param>
        /// <param name="level"></param>
        /// <param name="categoryId"></param>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void Log(string message, LogLevel level, string categoryId)
        {
            // ログが全体的に無効化されている場合は出力しない
            if (!_loggingEnabled)
                return;

            // カテゴリを取得（指定がない場合はデフォルト）
            var category = categoryId != null
                ? LoggerSettings.Instance.GetCategory(categoryId)
                : LoggerSettings.Instance.defaultCategory;

            // カテゴリが無効化されている場合は出力しない
            if (!LoggerSettings.Instance.IsCategoryEnabled(category.categoryId))
                return;

            // レベルが無効化されている場合は出力しない
            if (!_levelEnabled[level])
                return;

            string categoryName = category.displayName;

            // 現在時刻を取得
            string timestamp = DateTime.Now.ToString(LoggerSettings.Instance.timeFormat);
            string formattedMessage;
            // ログメッセージを整形
            if (_timeEnabled)
                formattedMessage = $"[{timestamp}][{categoryName}] {message}";
            else
                formattedMessage = $"[{categoryName}] {message}";


            // 色付きメッセージの準備
            string colorHex = ColorUtility.ToHtmlStringRGB(category.color);
            string colorizedMessage = $"<color=#{colorHex}>{formattedMessage}</color>";

            // ログレベルに応じて出力
            switch (level)
            {
                case LogLevel.Info:
                    UnityEngine.Debug.Log(colorizedMessage);
                    break;
                case LogLevel.Warning:
                    UnityEngine.Debug.LogWarning(colorizedMessage);
                    break;
                case LogLevel.Error:
                    UnityEngine.Debug.LogError(colorizedMessage);
                    break;
            }
        }
    }
}