using UnityEngine;

namespace TechC.Core.Setup
{
    /// <summary>
    /// MainSceneの初期化を行うクラス
    /// </summary>
    public class MainSceneSetup : SceneSetupBase
    {
        /// <summary>
        /// MainSceneの初期化処理
        /// </summary>
        protected override void SetupScene()
        {
            Debug.Log("MainSceneSetup: MainSceneの初期化開始");
        }
    }
}