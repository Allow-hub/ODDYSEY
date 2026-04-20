using System;
using System.Collections.Generic;
using UnityEngine;

namespace TechC.ODDESEY.Map
{
    public class MapNodeView : MonoBehaviour
    {
        [SerializeField] private Transform buttonContainer;
        [SerializeField] private GameObject clearedIndicator;
        [SerializeField] private GameObject currentIndicator;

        private readonly List<NodeChoiceButton> spawnedButtons = new();

        public enum NodeState { Locked, Active, Cleared }

        public void Setup(
            StageNodeData data,
            NodeState state,
            GameObject buttonPrefab,
            Action<NodeType> onChoiceSelected)
        {
            ClearButtons();

            // --- ガード ---
            if (buttonPrefab == null)
            {
                Debug.LogError($"[MapNodeView] {gameObject.name}: choiceButtonPrefab が null です。MapController の Inspector をご確認ください。");
                return;
            }
            if (data == null)
            {
                Debug.LogError($"[MapNodeView] {gameObject.name}: StageNodeData が null です。");
                return;
            }

            foreach (NodeType type in data.choices)
            {
                // buttonContainer が未設定なら自分自身の下に生成
                Transform parent = buttonContainer != null ? buttonContainer : transform;

                GameObject go = Instantiate(buttonPrefab, parent);

                // Prefab のルートになければ子を含めて探す
                NodeChoiceButton btn = go.GetComponentInChildren<NodeChoiceButton>(includeInactive: true);

                if (btn == null)
                {
                    Debug.LogError(
                        $"[MapNodeView] {gameObject.name}: Instantiate した '{go.name}' に " +
                        $"NodeChoiceButton が見つかりません。Prefab にコンポーネントをアタッチしてください。");
                    Destroy(go);
                    continue;
                }

                btn.Setup(type, onChoiceSelected);
                btn.SetInteractable(state == NodeState.Active);
                spawnedButtons.Add(btn);
            }

            if (clearedIndicator != null)
                clearedIndicator.SetActive(state == NodeState.Cleared);

            if (currentIndicator != null)
                currentIndicator.SetActive(state == NodeState.Active);
        }

        private void ClearButtons()
        {
            foreach (NodeChoiceButton btn in spawnedButtons)
            {
                if (btn != null) Destroy(btn.gameObject);
            }
            spawnedButtons.Clear();
        }

        private void OnDestroy() => ClearButtons();
    }
}