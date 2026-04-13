#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using TechC.VBattle.Core.Extensions;

namespace TechC.VBattle.Editor
{
    public class LoggerWindow : EditorWindow
    {
        [MenuItem("Tools/Custom Logger")]
        public static void ShowWindow()
        {
            GetWindow<LoggerWindow>("ログ設定");
        }

        private bool _allLogging = true;
        private bool _timeLogging = true;
        private Vector2 _scrollPosition;
        private LoggerSettings _settings;
        private SerializedObject _serializedSettings;
        private bool _isDirty = false;

        private void OnEnable()
        {
            _settings = LoggerSettings.Instance;
            _serializedSettings = new SerializedObject(_settings);
            _allLogging = true; // デフォルトで全ログ有効
        }

        private void OnGUI()
        {
            if (_settings == null)
            {
                _settings = LoggerSettings.Instance;
                if (_settings == null)
                {
                    EditorGUILayout.HelpBox("LoggerSettingsアセットが見つかりません。", MessageType.Error);
                    if (GUILayout.Button("設定アセットを作成"))
                    {
                        CreateDefaultSettings();
                        _settings = LoggerSettings.Instance;
                    }
                    return;
                }
                _serializedSettings = new SerializedObject(_settings);
            }

            _serializedSettings.Update();

            EditorGUILayout.LabelField("ログ設定", EditorStyles.boldLabel);

            // 全ログの有効/無効
            bool newAllLogging = EditorGUILayout.Toggle("すべてのログを有効化", _allLogging);
            if (newAllLogging != _allLogging)
            {
                _allLogging = newAllLogging;
                CustomLogger.EnableLogging(_allLogging);
            }
            // 時間ログの有効/無効
            bool timeLogging = EditorGUILayout.Toggle("時間を有効か", _timeLogging);
            if (timeLogging != _timeLogging)
            {
                _timeLogging = timeLogging;
                CustomLogger.TimeLogging(_timeLogging);
            }
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("タイムスタンプ形式", EditorStyles.boldLabel);
            string timeFormat = EditorGUILayout.TextField("形式", _settings.timeFormat);
            if (timeFormat != _settings.timeFormat)
            {
                _settings.timeFormat = timeFormat;
                _isDirty = true;
            }

            EditorGUILayout.HelpBox("形式例: HH:mm:ss.fff - 時:分:秒.ミリ秒", MessageType.Info);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("カテゴリ設定", EditorStyles.boldLabel);

            if (GUILayout.Button("新規カテゴリを追加"))
            {
                AddNewCategory();
                _isDirty = true;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // カテゴリごとの設定
            for (int i = 0; i < _settings.availableCategories.Count; i++)
            {
                var category = _settings.availableCategories[i];
                bool isDefault = (_settings.defaultCategory != null && _settings.defaultCategory.categoryId == category.categoryId);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(category.displayName, EditorStyles.boldLabel);

                bool newIsDefault = EditorGUILayout.ToggleLeft("デフォルト", isDefault, GUILayout.Width(80));
                if (newIsDefault && !isDefault)
                {
                    _settings.defaultCategory = category;
                    _isDirty = true;
                }

                // デフォルトカテゴリは削除不可
                GUI.enabled = !isDefault;
                if (GUILayout.Button("削除", GUILayout.Width(60)))
                {
                    if (EditorUtility.DisplayDialog("カテゴリを削除",
                        $"本当に「{category.displayName}」カテゴリを削除しますか？", "削除", "キャンセル"))
                    {
                        _settings.availableCategories.RemoveAt(i);
                        _isDirty = true;
                        i--;
                        continue;
                    }
                }
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();

                // カテゴリIDは変更不可（システム内で参照されるため）
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("ID", category.categoryId);
                EditorGUI.EndDisabledGroup();

                // 表示名は変更可能
                string newName = EditorGUILayout.TextField("表示名", category.displayName);
                if (newName != category.displayName)
                {
                    category.displayName = newName;
                    _isDirty = true;
                }

                // カラー設定
                Color newColor = EditorGUILayout.ColorField("カラー", category.color);
                if (newColor != category.color)
                {
                    category.color = newColor;
                    _isDirty = true;
                }

                // デフォルト有効設定
                bool newEnabledByDefault = EditorGUILayout.Toggle("デフォルトで有効", category.enabledByDefault);
                if (newEnabledByDefault != category.enabledByDefault)
                {
                    category.enabledByDefault = newEnabledByDefault;
                    _isDirty = true;
                }

                // 実行時の有効/無効状態
                bool isEnabled = _settings.IsCategoryEnabled(category.categoryId);
                bool newIsEnabled = EditorGUILayout.Toggle("現在の有効状態", isEnabled);
                if (newIsEnabled != isEnabled)
                {
                    _settings.SetCategoryEnabled(category.categoryId, newIsEnabled);
                    _isDirty = true;
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("ログレベル設定", EditorStyles.boldLabel);

            // レベルごとの設定
            foreach (CustomLogger.LogLevel level in System.Enum.GetValues(typeof(CustomLogger.LogLevel)))
            {
                bool enabled = EditorPrefs.GetBool("Logger_Level_" + level.ToString(), true);
                bool newEnabled = EditorGUILayout.Toggle(level.ToString(), enabled);

                if (newEnabled != enabled)
                {
                    EditorPrefs.SetBool("Logger_Level_" + level.ToString(), newEnabled);
                    CustomLogger.EnableLevel(level, newEnabled);
                    _isDirty = true;
                }
            }

            EditorGUILayout.EndScrollView();

            if (_isDirty)
            {
                EditorUtility.SetDirty(_settings);
                AssetDatabase.SaveAssets();
                _isDirty = false;
            }

            _serializedSettings.ApplyModifiedProperties();
        }

        // 新しいカテゴリを追加
        private void AddNewCategory()
        {
            // カテゴリIDを生成（uniqueになるよう数字を付加）
            string baseId = "new_category";
            string categoryId = baseId;
            int counter = 1;

            while (CategoryIdExists(categoryId))
            {
                categoryId = $"{baseId}_{counter}";
                counter++;
            }

            // 新しいカテゴリ情報を作成
            var newCategory = new LoggerSettings.LogCategoryInfo
            {
                categoryId = categoryId,
                displayName = "新規カテゴリ",
                color = new Color(Random.value, Random.value, Random.value), // ランダムな色
                enabledByDefault = true
            };

            // リストに追加
            _settings.availableCategories.Add(newCategory);
            EditorUtility.SetDirty(_settings);
        }

        // カテゴリIDが既に存在するか確認
        private bool CategoryIdExists(string categoryId)
        {
            foreach (var category in _settings.availableCategories)
            {
                if (category.categoryId == categoryId)
                {
                    return true;
                }
            }
            return false;
        }

        // デフォルト設定を作成
        private void CreateDefaultSettings()
        {
            // Resources フォルダ確認
            if (!System.IO.Directory.Exists("Assets/Resources"))
            {
                System.IO.Directory.CreateDirectory("Assets/Resources");
            }

            // 設定作成
            var settings = CreateInstance<LoggerSettings>();

            // デフォルトカテゴリ作成
            var defaultCategory = new LoggerSettings.LogCategoryInfo
            {
                categoryId = "default",
                displayName = "Default",
                color = Color.white,
                enabledByDefault = true
            };

            // ステート用カテゴリ
            var stateCategory = new LoggerSettings.LogCategoryInfo
            {
                categoryId = "state",
                displayName = "State Machine",
                color = new Color(0.3f, 0.8f, 0.3f),
                enabledByDefault = true
            };

            // 設定に追加
            settings.defaultCategory = defaultCategory;
            settings.availableCategories = new List<LoggerSettings.LogCategoryInfo>
            {
                defaultCategory,
                stateCategory
            };

            // Resources内に保存
            AssetDatabase.CreateAsset(settings, "Assets/Resources/LoggerSettings.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
#endif