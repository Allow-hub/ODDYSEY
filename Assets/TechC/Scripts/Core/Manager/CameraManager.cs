using System.Collections;
using System.Collections.Generic;
using TechC.Core.Manager;
using UnityEngine;

namespace TechC.ODDESEY.Core.Manager
{
    /// <summary>
    /// カメラを管理するクラス
    /// </summary>
    public class CameraManager : Singleton<CameraManager>
    {
        [SerializeField] private Cinemachine.CinemachineVirtualCamera vcamA;
        [SerializeField] private Cinemachine.CinemachineVirtualCamera vcamB;

        [ContextMenu("SwitchToB")]
        public void SwitchToB()
        {
            vcamA.Priority = 10;
            vcamB.Priority = 20;
        }
    }
}
