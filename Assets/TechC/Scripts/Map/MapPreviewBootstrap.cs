using TechC.ODDESEY.Event;
using TechC.ODDESEY.Reward;
using TechC.ODDESEY.Battle;
using UnityEngine;

namespace TechC.ODDESEY.Map
{
    /// <summary>
    /// Designer-facing bootstrap for previewing the map UI in a work scene.
    /// Runtime flow still goes through MainManager.
    /// </summary>
    public sealed class MapPreviewBootstrap : MonoBehaviour
    {
        [SerializeField] private MapController mapController;
        [SerializeField] private StageMapData stageMapData;
        [SerializeField, Min(0)] private int currentNodeIndex;
        [SerializeField] private bool initializeOnStart = true;
        [SerializeField] private bool refreshAfterSelection = true;

        private readonly MapProgressState previewProgress = new();
        private bool isSubscribed;

        private void Reset()
        {
            mapController = FindFirstObjectByType<MapController>();
        }

        private void Start()
        {
            if (ResolveReferences())
            {
                Subscribe();
            }

            if (initializeOnStart)
            {
                InitializePreview();
            }
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        [ContextMenu("Initialize Preview")]
        public void InitializePreview()
        {
            if (!ResolveReferences())
            {
                return;
            }

            previewProgress.currentNodeIndex = ClampNodeIndex(currentNodeIndex);
            mapController.Initialize(stageMapData, previewProgress);
        }

        [ContextMenu("Reset Preview Progress")]
        public void ResetPreviewProgress()
        {
            currentNodeIndex = 0;
            InitializePreview();
        }

        private bool ResolveReferences()
        {
            if (mapController == null)
            {
                mapController = FindFirstObjectByType<MapController>();
            }

            if (mapController == null)
            {
                Debug.LogError("[MapPreviewBootstrap] MapController is not assigned.");
                return false;
            }

            if (stageMapData == null)
            {
                Debug.LogError("[MapPreviewBootstrap] StageMapData is not assigned.");
                return false;
            }

            return true;
        }

        private void Subscribe()
        {
            if (isSubscribed || mapController == null)
            {
                return;
            }

            mapController.OnBattleRequested += HandleBattleRequested;
            mapController.OnEventRequested += HandleEventRequested;
            mapController.OnStageCompleted += HandleStageCompleted;
            isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!isSubscribed || mapController == null)
            {
                return;
            }

            mapController.OnBattleRequested -= HandleBattleRequested;
            mapController.OnEventRequested -= HandleEventRequested;
            mapController.OnStageCompleted -= HandleStageCompleted;
            isSubscribed = false;
        }

        private void HandleBattleRequested(BattleRewardData rewardData, bool isBossNode, EnemyData enemyData)
        {
            Debug.Log($"[MapPreviewBootstrap] Battle selected. IsBossNode: {isBossNode}");
            RefreshAfterSelection();
        }

        private void HandleEventRequested(EventData eventData)
        {
            string eventName = eventData != null ? eventData.EventName : "None";
            Debug.Log($"[MapPreviewBootstrap] Event selected. Event: {eventName}");
            RefreshAfterSelection();
        }

        private void HandleStageCompleted()
        {
            Debug.Log("[MapPreviewBootstrap] Stage completed.");
            RefreshAfterSelection();
        }

        private void RefreshAfterSelection()
        {
            currentNodeIndex = ClampNodeIndex(previewProgress.currentNodeIndex);

            if (refreshAfterSelection)
            {
                mapController.Initialize(stageMapData, previewProgress);
            }
        }

        private int ClampNodeIndex(int index)
        {
            int max = stageMapData != null ? stageMapData.nodes.Count : 0;
            return Mathf.Clamp(index, 0, Mathf.Max(0, max - 1));
        }
    }
}
