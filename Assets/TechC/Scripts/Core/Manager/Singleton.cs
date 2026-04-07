using UnityEngine;

namespace TechC.Core.Manager
{
    /// <summary>
    /// シングルトンの基底クラス
    /// SceneSetupBaseで外部から初期化すること
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        protected virtual bool DontDestroy => false;

        public static T I { get; private set; } = null;

        // Singletonが有効か
        public static bool IsValid() => I != null;

        private void OnDestroy()
        {
            if (I == this)
            {
                I = null;
                OnRelease();
            }
        }

        /// <summary>
        /// 外部から明示的に初期化を呼び出すメソッド
        /// SceneSetupBaseから呼び出される
        /// </summary>
        public void Init()
        {
            if (I == null)
            {
                I = this as T;
                if (DontDestroy)
                {
                    DontDestroyOnLoad(gameObject);
                }
                OnInit();
            }
            else if (I != this)
            {
                // 既にシングルトンが存在する場合は削除
                if (DontDestroy)
                {
                    Destroy(gameObject);
                }
                else
                {
                    Destroy(this);
                }
            }
        }

        /// <summary>
        /// 派生クラスで初期化処理を実装
        /// </summary>
        protected virtual void OnInit()
        {
        }

        /// <summary>
        /// 派生クラス用のOnDestroy
        /// </summary>
        protected virtual void OnRelease()
        {
        }
    }
}