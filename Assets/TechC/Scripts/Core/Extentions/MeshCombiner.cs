using System.Collections.Generic;
using UnityEngine;

namespace TechC.VBattle.Core.Util
{
    /// <summary>
    /// メッシュ結合ユーティリティ
    /// </summary>
    public class MeshCombiner : MonoBehaviour
    {
        [Header("結合設定")]
        [Tooltip("マテリアルごとにメッシュを分ける（false=全て1つに統合）")]
        [SerializeField] private bool keepMaterialsSeparate = true;

        [Tooltip("結合後に元のオブジェクトを削除")]
        [SerializeField] private bool destroyOriginalObjects = true;

        [Tooltip("結合後のメッシュを保存（Assets/CombinedMeshes/に保存）")]
        [SerializeField] private bool saveMeshAsAsset = false;

        [Header("詳細設定")]
        [Tooltip("65535頂点を超える場合、自動的に分割")]
        [SerializeField] private bool use32BitIndices = true;

        [Tooltip("結合するレイヤー（空=全レイヤー）")]
        [SerializeField] private LayerMask layersToInclude = -1;

        [Header("除外設定")]
        [Tooltip("結合から除外するオブジェクト")]
        [SerializeField] private GameObject[] excludedObjects;

        [Tooltip("このタグを持つオブジェクトを除外")]
        [SerializeField] private string[] excludedTags;

        [Tooltip("このレイヤーのオブジェクトを除外")]
        [SerializeField] private LayerMask excludedLayers;

        private HashSet<GameObject> excludedSet;

        [ContextMenu("メッシュを結合")]
        public void CombineMeshes()
        {
            // 除外リストを作成
            BuildExclusionSet();

            // 子オブジェクトのMeshFilterを取得
            MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();

            if (meshFilters.Length == 0)
            {
                Debug.LogWarning("結合するメッシュが見つかりません");
                return;
            }

            Debug.Log($"結合開始: {meshFilters.Length}個のメッシュを処理");

            if (keepMaterialsSeparate)
            {
                CombineByMaterial(meshFilters);
            }
            else
            {
                CombineAll(meshFilters);
            }

            Debug.Log("メッシュ結合完了！");
        }

        /// <summary>
        /// 除外リストを構築
        /// </summary>
        void BuildExclusionSet()
        {
            excludedSet = new HashSet<GameObject>();

            // 除外オブジェクトを追加
            if (excludedObjects != null)
            {
                foreach (var obj in excludedObjects)
                {
                    if (obj != null)
                    {
                        excludedSet.Add(obj);
                        // 子オブジェクトも除外
                        foreach (Transform child in obj.GetComponentsInChildren<Transform>())
                        {
                            excludedSet.Add(child.gameObject);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// オブジェクトが除外対象かチェック
        /// </summary>
        bool IsExcluded(GameObject obj)
        {
            // 除外リストに含まれている
            if (excludedSet.Contains(obj))
                return true;

            // 除外タグをチェック
            if (excludedTags != null)
            {
                foreach (string tag in excludedTags)
                {
                    if (!string.IsNullOrEmpty(tag) && obj.CompareTag(tag))
                        return true;
                }
            }

            // 除外レイヤーをチェック
            if (excludedLayers == (excludedLayers | (1 << obj.layer)))
                return true;

            return false;
        }

        /// <summary>
        /// マテリアルごとに分けて結合
        /// </summary>
        void CombineByMaterial(MeshFilter[] meshFilters)
        {
            Dictionary<Material, List<CombineInstance>> materialToMeshes = new Dictionary<Material, List<CombineInstance>>();

            // このオブジェクトのワールドスケールを保存
            Vector3 originalScale = transform.lossyScale;

            // マテリアルごとにグループ化
            foreach (MeshFilter mf in meshFilters)
            {
                if (mf == null || mf.sharedMesh == null) continue;

                // 除外チェック
                if (IsExcluded(mf.gameObject))
                {
                    Debug.Log($"除外: {mf.gameObject.name}");
                    continue;
                }

                MeshRenderer mr = mf.GetComponent<MeshRenderer>();
                if (mr == null) continue;

                // レイヤーチェック
                if (layersToInclude != (layersToInclude | (1 << mf.gameObject.layer)))
                    continue;

                foreach (Material mat in mr.sharedMaterials)
                {
                    if (mat == null) continue;

                    if (!materialToMeshes.ContainsKey(mat))
                    {
                        materialToMeshes[mat] = new List<CombineInstance>();
                    }

                    CombineInstance ci = new CombineInstance();
                    ci.mesh = mf.sharedMesh;
                    // ワールド座標系での変換行列を使用（親のスケールは無視）
                    ci.transform = transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                    materialToMeshes[mat].Add(ci);
                }
            }

            // マテリアルごとに結合されたメッシュを作成
            List<CombineInstance> finalCombine = new List<CombineInstance>();
            List<Material> materials = new List<Material>();

            foreach (var kvp in materialToMeshes)
            {
                Mesh combinedMesh = new Mesh();
                if (use32BitIndices)
                {
                    combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                }

                combinedMesh.CombineMeshes(kvp.Value.ToArray(), true, true);

                CombineInstance ci = new CombineInstance();
                ci.mesh = combinedMesh;
                ci.transform = Matrix4x4.identity;

                finalCombine.Add(ci);
                materials.Add(kvp.Key);

                Debug.Log($"マテリアル '{kvp.Key.name}': {kvp.Value.Count}個のメッシュを結合");
            }

            // 最終的なメッシュを作成
            CreateCombinedObject(finalCombine.ToArray(), materials.ToArray(), false);

            // 元のオブジェクトを削除
            if (destroyOriginalObjects)
            {
                DestroyOriginalMeshes(meshFilters);
            }
        }

        /// <summary>
        /// 全て1つに結合（マテリアル無視）
        /// </summary>
        void CombineAll(MeshFilter[] meshFilters)
        {
            List<CombineInstance> combine = new List<CombineInstance>();

            foreach (MeshFilter mf in meshFilters)
            {
                if (mf == null || mf.sharedMesh == null) continue;

                // 除外チェック
                if (IsExcluded(mf.gameObject))
                {
                    Debug.Log($"除外: {mf.gameObject.name}");
                    continue;
                }

                // レイヤーチェック
                if (layersToInclude != (layersToInclude | (1 << mf.gameObject.layer)))
                    continue;

                CombineInstance ci = new CombineInstance();
                ci.mesh = mf.sharedMesh;
                // ワールド座標系での変換行列を使用
                ci.transform = transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                combine.Add(ci);
            }

            CreateCombinedObject(combine.ToArray(), null, true);

            if (destroyOriginalObjects)
            {
                DestroyOriginalMeshes(meshFilters);
            }
        }

        void CreateCombinedObject(CombineInstance[] combine, Material[] materials, bool mergeSubmeshes)
        {
            GameObject combinedObj = new GameObject("CombinedMesh");
            combinedObj.transform.SetParent(transform);
            combinedObj.transform.localPosition = Vector3.zero;
            combinedObj.transform.localRotation = Quaternion.identity;
            combinedObj.transform.localScale = Vector3.one;

            MeshFilter mf = combinedObj.AddComponent<MeshFilter>();
            MeshRenderer mr = combinedObj.AddComponent<MeshRenderer>();

            Mesh finalMesh = new Mesh();
            if (use32BitIndices)
            {
                finalMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }

            finalMesh.name = "CombinedMesh";
            finalMesh.CombineMeshes(combine, mergeSubmeshes, false);

            // メッシュの最適化
            finalMesh.RecalculateBounds();
            finalMesh.RecalculateNormals();
            finalMesh.RecalculateTangents();
            finalMesh.Optimize();

            mf.sharedMesh = finalMesh;

            if (materials != null && materials.Length > 0)
            {
                mr.sharedMaterials = materials;
            }

            // メッシュを保存
            if (saveMeshAsAsset)
            {
                SaveMeshAsAsset(finalMesh);
            }

            Debug.Log($"結合完了: 頂点数 {finalMesh.vertexCount}, トライアングル数 {finalMesh.triangles.Length / 3}");
        }

        void DestroyOriginalMeshes(MeshFilter[] meshFilters)
        {
            foreach (MeshFilter mf in meshFilters)
            {
                if (mf != null && mf.gameObject != gameObject && !IsExcluded(mf.gameObject))
                {
                    DestroyImmediate(mf.gameObject);
                }
            }
        }

        void SaveMeshAsAsset(Mesh mesh)
        {
#if UNITY_EDITOR
            string path = "Assets/CombinedMeshes/";
            if (!System.IO.Directory.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }

            string assetPath = path + mesh.name + "_" + System.DateTime.Now.Ticks + ".asset";
            UnityEditor.AssetDatabase.CreateAsset(mesh, assetPath);
            UnityEditor.AssetDatabase.SaveAssets();

            Debug.Log($"メッシュを保存しました: {assetPath}");
#endif
        }
    }
}