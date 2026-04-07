using UnityEngine;

namespace TechC.Core.Setup
{
    /// <summary>
    /// 各シーンの初期化を行うセットアップの基底クラス
    /// シーン内でこれを継承したクラスを作成し、各種マネージャーの初期化を行う
    /// </summary>
    public abstract class SceneSetupBase : MonoBehaviour
    {
        protected virtual void Awake()
        {
            SetupScene();
        }

        /// <summary>
        /// シーンの初期化処理
        /// 派生クラスで各シーンに必要な初期化を行う
        /// </summary>
        protected abstract void SetupScene();
    }
}
