using System.Collections.Generic;
using UnityEngine;

public class DitherZoom : MonoBehaviour
{
    //A material and the dither range it uses at each end of the zoom
    [System.Serializable]
    public class DitherEntry
    {
        public Material material;
        //Dither amount when the camera is fully zoomed in
        [Range(0f, 1f)] public float ditherZoomedIn = 0.2f;
        //Dither amount when the camera is fully zoomed out
        [Range(0f, 1f)] public float ditherZoomedOut = 1f;
    }

    [SerializeField] private Camera _camera;
    [SerializeField] private List<DitherEntry> ditherMaterials = new List<DitherEntry>();
    private CameraPan cameraPan;
    private float zoomedInSize = 3f;
    private float zoomedOutSize = 8f;

    private static readonly int DitherAmountId = Shader.PropertyToID("_DitherAmount");
    private float[] originalValues;
    private float lastZoomT = -1f;

    //Set references and read the zoom limits from the camera pan
    private void Awake()
    {
        if (_camera == null) _camera = Camera.main;
        cameraPan = FindAnyObjectByType<CameraPan>();
        if (cameraPan != null)
        {
            zoomedInSize = cameraPan.minZoom;
            zoomedOutSize = cameraPan.maxZoom;
        }
    }

    //Save each material's starting dither on set active true
    private void OnEnable()
    {
        originalValues = new float[ditherMaterials.Count];
        for (int i = 0; i < ditherMaterials.Count; i++)
        {
            Material mat = ditherMaterials[i].material;
            if (mat != null && mat.HasProperty(DitherAmountId))
                originalValues[i] = mat.GetFloat(DitherAmountId);
        }
        lastZoomT = -1f;
    }

    //Put each material back to its starting dither on set active false
    private void OnDisable()
    {
        if (originalValues == null) return;
        for (int i = 0; i < ditherMaterials.Count && i < originalValues.Length; i++)
        {
            Material mat = ditherMaterials[i].material;
            if (mat != null && mat.HasProperty(DitherAmountId))
                mat.SetFloat(DitherAmountId, originalValues[i]);
        }
        lastZoomT = -1f;
    }

    //Based on zoom set each material's dither
    private void LateUpdate()
    {
        if (_camera == null || ditherMaterials.Count == 0) return;

        // 0 = fully zoomed in, 1 = fully zoomed out
        float zoomT = Mathf.InverseLerp(zoomedInSize, zoomedOutSize, _camera.orthographicSize);

        //Skip if the zoom hasn't changed
        if (Mathf.Approximately(zoomT, lastZoomT)) return;
        lastZoomT = zoomT;

        foreach (DitherEntry entry in ditherMaterials)
        {
            Material mat = entry.material;
            if (mat == null || !mat.HasProperty(DitherAmountId)) continue;

            mat.SetFloat(DitherAmountId, Mathf.Lerp(entry.ditherZoomedIn, entry.ditherZoomedOut, zoomT));
        }
    }
}