using UnityEngine;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TechC.VBattle.Core.Extensions
{
    /// <summary>
    /// 再コンパイル後も設定を維持するロガー設定クラス
    /// </summary>
    [CreateAssetMenu(fileName = "LoggerSettings", menuName = "TechC/LoggerSettings")]
    public class LoggerSettings : ScriptableObject
    {
        private static LoggerSettings _instance;
        private const string ASSET_PATH = "Assets/Resources/LoggerSettings.asset";
        private const string RESOURCES_PATH = "LoggerSettings";

        public static LoggerSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    // リソースからロード試行
                    _instance = Resources.Load<LoggerSettings>(RESOURCES_PATH);

                    // インスタンスがなければ作成
                    if (_instance == null)
                    {
                        CreateAndSaveSettings();
                    }
                }
                return _instance;
            }
        }

        [System.Serializable]
        public class LogCategoryInfo
        {
            public string categoryId;
            public string displayName;
            public Color color = Color.white;
            public bool enabledByDefault = true;
        }

        // 設定項目
        public string timeFormat = "HH:mm:ss.fff";
        public List<LogCategoryInfo> availableCategories = new List<LogCategoryInfo>();
        public LogCategoryInfo defaultCategory;

        // カテゴリのオンオフ状態を保持する辞書（シリアライズ可能な形式）
        [SerializeField, ReadOnly]
        private List<string> _enabledCategoryIds = new List<string>();
        [SerializeField, ReadOnly]
        private List<string> _disabledCategoryIds = new List<string>();

        // カテゴリの有効化状態を取得
        public bool IsCategoryEnabled(string categoryId)
        {
            if (_disabledCategoryIds.Contains(categoryId))
                return false;

            if (_enabledCategoryIds.Contains(categoryId))
                return true;

            // デフォルト値を返す
            foreach (var category in availableCategories)
            {
                if (category.categoryId == categoryId)
                    return category.enabledByDefault;
            }

            return true; // カテゴリが見つからない場合はデフォルトで有効
        }

        // カテゴリの有効化状態を設定
        public void SetCategoryEnabled(string categoryId, bool enabled)
        {
            // 既存のリストから削除
            _enabledCategoryIds.Remove(categoryId);
            _disabledCategoryIds.Remove(categoryId);

            // 適切なリストに追加
            if (enabled)
                _enabledCategoryIds.Add(categoryId);
            else
                _disabledCategoryIds.Add(categoryId);

            // 変更を保存
            SaveSettings();
        }

        /// <summary>
        /// カテゴリが存在するか確認
        /// </summary>
        /// <param name="categoryId"></param>
        /// <returns></returns>
        public bool CategoryExists(string categoryId) => GetCategory(categoryId) != null;

        /// <summary>
        /// カテゴリ情報を取得
        /// </summary>
        /// <param name="categoryId"></param>
        /// <returns></returns>
        public LogCategoryInfo GetCategory(string categoryId)
        {
            foreach (var category in availableCategories)
            {
                if (category.categoryId == categoryId)
                    return category;
            }
            return defaultCategory;
        }

        /// <summary>
        /// 設定を保存
        /// </summary>
        private void SaveSettings()
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
#endif
        }

        /// <summary>
        /// インスタンス作成と保存（エディタ専用）
        /// </summary>
        private static void CreateAndSaveSettings()
        {
#if UNITY_EDITOR
            // フォルダ存在確認と作成
            string resourcesFolder = "Assets/Resources";
            if (!Directory.Exists(resourcesFolder))
            {
                Directory.CreateDirectory(resourcesFolder);
            }

            // 新規作成
            _instance = CreateInstance<LoggerSettings>();

            // デフォルト設定
            _instance.InitializeDefaultCategories();

            // アセットとして保存
            AssetDatabase.CreateAsset(_instance, ASSET_PATH);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#else
            // ランタイムでは新規インスタンス作成のみ
            _instance = CreateInstance<LoggerSettings>();
            _instance.InitializeDefaultCategories();
#endif
        }

        /// <summary>
        /// デフォルトのカテゴリを初期化
        /// </summary>
        private void InitializeDefaultCategories()
        {
            // デフォルトカテゴリ
            defaultCategory = new LogCategoryInfo
            {
                categoryId = "default",
                displayName = "Default",
                color = Color.white,
                enabledByDefault = true
            };

            // 利用可能なカテゴリ一覧を初期化
            availableCategories = new List<LogCategoryInfo>
            {
                defaultCategory,
                new LogCategoryInfo
                {
                    categoryId = "state",
                    displayName = "State Machine",
                    color = new Color(0.3f, 0.8f, 0.3f),
                    enabledByDefault = true
                },
                new LogCategoryInfo
                {
                    categoryId = "input",
                    displayName = "Input System",
                    color = new Color(0.8f, 0.3f, 0.3f),
                    enabledByDefault = true
                }
                // 他のカテゴリを追加
            };
        }

#if UNITY_EDITOR
        // エディタでドメインリロード後に呼ばれる
        [InitializeOnLoadMethod]
        private static void OnEditorReload()
        {
            // インスタンスの再取得（これによりエディタリロード後も設定が維持される）
            EditorApplication.delayCall += () =>
            {
                var instance = Instance;
            };
        }
#endif
    }

}