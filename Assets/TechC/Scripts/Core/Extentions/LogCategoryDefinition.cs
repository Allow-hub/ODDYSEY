using UnityEngine;

namespace TechC.VBattle.Core.Extensions
{

    [CreateAssetMenu(fileName = "LogCategory", menuName = "TechC/LogCategory")]
    public class LogCategoryDefinition : ScriptableObject
    {
        public string categoryId;
        public Color color = Color.white;

        /// <summary>
        /// 現在の有効状態を取得
        /// </summary>
        /// <returns></returns>
        public bool IsEnabled()
        {
            return LoggerSettings.Instance.IsCategoryEnabled(categoryId);
        }

        /// <summary>
        /// 有効状態を設定
        /// </summary>
        /// <param name="enabled"></param>
        public void SetEnabled(bool enabled)
        {
            LoggerSettings.Instance.SetCategoryEnabled(categoryId, enabled);
        }
    }
}
