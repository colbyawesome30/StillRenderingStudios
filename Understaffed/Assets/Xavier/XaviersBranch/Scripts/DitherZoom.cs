using System.Collections.Generic;
using UnityEngine;

public class DitherZoom : MonoBehaviour
{
    [System.Serializable]
    public class DitherEntry
    {
        public Material material;
        [Range(0f, 1f)] public float ditherMin = 0.2f;
        [Range(0f, 1f)] public float ditherMax = 1f;
    }

    [SerializeField] private List<DitherEntry> ditherMaterials = new List<DitherEntry>();
    [Range(0f, 2f)] [SerializeField] private float innerRadius = 0.5f;
    [Range(0f, 2f)] [SerializeField] private float outerRadius = 0.8f;
    [SerializeField] private Vector2 centerOffset = Vector2.zero;

    private static readonly int DitherCenterId = Shader.PropertyToID("_DitherCenter");
    private static readonly int DitherInnerId = Shader.PropertyToID("_DitherInner");
    private static readonly int DitherOuterId = Shader.PropertyToID("_DitherOuter");
    private static readonly int DitherMinId = Shader.PropertyToID("_DitherMin");
    private static readonly int DitherMaxId = Shader.PropertyToID("_DitherMax");

    private float[] originalMin;
    private float[] originalMax;

    //Save each material's starting dither on set active true
    private void OnEnable()
    {
        originalMin = new float[ditherMaterials.Count];
        originalMax = new float[ditherMaterials.Count];

        for (int i = 0; i < ditherMaterials.Count; i++)
        {
            Material mat = ditherMaterials[i].material;
            if (mat == null) continue;

            if (mat.HasProperty(DitherMinId)) originalMin[i] = mat.GetFloat(DitherMinId);
            if (mat.HasProperty(DitherMaxId)) originalMax[i] = mat.GetFloat(DitherMaxId);
        }
    }

    //Put each material back to its starting dither on set active false
    private void OnDisable()
    {
        if (originalMin == null || originalMax == null) return;

        for (int i = 0; i < ditherMaterials.Count && i < originalMin.Length; i++)
        {
            Material mat = ditherMaterials[i].material;
            if (mat == null) continue;

            if (mat.HasProperty(DitherMinId)) mat.SetFloat(DitherMinId, originalMin[i]);
            if (mat.HasProperty(DitherMaxId)) mat.SetFloat(DitherMaxId, originalMax[i]);
        }
    }

    private void LateUpdate()
    {
        float inner = Mathf.Min(innerRadius, outerRadius) * 0.5f;
        float outer = Mathf.Max(innerRadius, outerRadius) * 0.5f;
        Vector2 center = new Vector2(0.5f + centerOffset.x, 0.5f + centerOffset.y);

        Shader.SetGlobalVector(DitherCenterId, center);
        Shader.SetGlobalFloat(DitherInnerId, inner);
        Shader.SetGlobalFloat(DitherOuterId, outer);

        foreach (DitherEntry entry in ditherMaterials)
        {
            Material mat = entry.material;
            if (mat == null) continue;

            if (mat.HasProperty(DitherCenterId)) mat.SetVector(DitherCenterId, center);
            if (mat.HasProperty(DitherInnerId)) mat.SetFloat(DitherInnerId, inner);
            if (mat.HasProperty(DitherOuterId)) mat.SetFloat(DitherOuterId, outer);
            if (mat.HasProperty(DitherMinId)) mat.SetFloat(DitherMinId, entry.ditherMin);
            if (mat.HasProperty(DitherMaxId)) mat.SetFloat(DitherMaxId, entry.ditherMax);
        }
    }

    private void OnValidate()
    {
        if (Application.isEditor)
        {
            foreach (DitherEntry entry in ditherMaterials)
            {
                if (entry.material == null) continue;

                //Set max alpha for when in editor
                if (entry.material.HasProperty(DitherMinId)) entry.material.SetFloat(DitherMinId, entry.ditherMax);
                if (entry.material.HasProperty(DitherMaxId)) entry.material.SetFloat(DitherMaxId, entry.ditherMax);
            }
        }
    }
}