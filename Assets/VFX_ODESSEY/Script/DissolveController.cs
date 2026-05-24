using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

public class DissolveController : MonoBehaviour
{
    public MeshRenderer meshRenderer;
    public VisualEffect VFXGraph;
    public float dissolveRate = 0.0125f;
    public float refreshRate = 0.025f;

    private Material[] materials;

    void Start()
    {
        if (meshRenderer != null)
        {
            materials = meshRenderer.materials;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(DissolveCo());
        }
    }

    IEnumerator DissolveCo()
    {
        if (VFXGraph != null)
        {
            VFXGraph.SendEvent("Onplay");
        }

        if (materials != null && materials.Length > 0)
        {
            float counter = 0f;

            while (counter < 1f)
            {
                counter += dissolveRate;

                foreach (var mat in materials)
                {
                    mat.SetFloat("_Dissolve_Amount", counter);
                }

                yield return new WaitForSeconds(refreshRate);
            }
        }
    }
}