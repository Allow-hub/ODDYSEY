using UnityEngine;
using TechC.Core.Manager;
using TechC.Core.Setup;
using TechC.ODDESEY.Core.Manager;

namespace TechC.Core.Setup
{
    /// <summary>
    /// ManagerSceneの初期化を行うクラス
    /// マネージャーの初期化処理をここで行う
    /// </summary>
    public class ManagerSceneSetup : SceneSetupBase
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private AudioManager audioManager;
        [SerializeField] private PauseManager pauseManager;
        /// <summary>
        /// ManagerSceneの初期化処理
        /// </summary>
        protected override void SetupScene()
        {
            gameManager.Init();
            audioManager.Init();
            pauseManager.Init();
        }
    }
}