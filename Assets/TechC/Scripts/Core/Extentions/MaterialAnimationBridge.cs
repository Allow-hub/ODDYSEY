using UnityEngine;

namespace TechC.ODDESEY.Battle
{
    /// <summary>
    /// Animator からマテリアルのプロパティを操作するためのブリッジ。
    /// SpriteRenderer にアタッチして使う。
    ///
    /// 使い方：
    ///   1. このコンポーネントを SpriteRenderer と同じ GameObject にアタッチ
    ///   2. Animator ウィンドウで dissolveAmount をキーフレームする
    ///   3. Update() が毎フレームマテリアルに値を反映する
    ///
    /// ※ SpriteRenderer.material はインスタンス化されるため
    ///    他のオブジェクトのマテリアルには影響しない。
    /// </summary>
    public class MaterialAnimationBridge : MonoBehaviour
    {
        [Header("プロパティ設定")]
        [Tooltip("操作するシェーダープロパティ名")]
        [SerializeField] private string propertyName = "_DissolveAmount";

        [Header("Animator がキーフレームする値")]
        [Range(0f, 1f)]
        public float dissolveAmount = 0f;

        [SerializeField] private SpriteRenderer spriteRenderer;
        private Material instancedMaterial;
        private static readonly int PropertyId = Shader.PropertyToID("_DissolveAmount");
        private int cachedPropertyId;

        private void Awake()
        {
            // SpriteRenderer.material はアクセス時に自動でインスタンス化される
            instancedMaterial = spriteRenderer.material;
            cachedPropertyId  = Shader.PropertyToID(propertyName);
        }

        private void Update()
        {
            if (instancedMaterial == null) return;
            instancedMaterial.SetFloat(cachedPropertyId, dissolveAmount);
        }

        private void OnDestroy()
        {
            // インスタンス化されたマテリアルを破棄
            if (instancedMaterial != null)
                Destroy(instancedMaterial);
        }
    }
}