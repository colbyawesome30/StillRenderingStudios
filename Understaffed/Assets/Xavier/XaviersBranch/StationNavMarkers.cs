using System.Collections.Generic;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.UI;

public class StationNavMarkers : MonoBehaviour
{
    [SerializeField] private Camera worldCamera;
    [SerializeField] private StationNavMarker markerPrefab;
    [SerializeField] private float edgePadding = 60f;
    [SerializeField] private float onScreenMargin = 0f;
    [SerializeField] private bool showLabels = true;

    private RectTransform rect;
    private Camera canvasCamera;
    private readonly Dictionary<Transform, StationNavMarker> markers = new Dictionary<Transform, StationNavMarker>();
    private readonly HashSet<Transform> seen = new HashSet<Transform>();
    private readonly List<Transform> removeMarkers = new List<Transform>();

    //Find active camera
    private void Awake()
    {
        rect = (RectTransform)transform;
        if (worldCamera == null) worldCamera = Camera.main;

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas = canvas.rootCanvas;
            canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }
    }
    //Update markers every frame
    private void LateUpdate()
    {
        if (worldCamera == null)
        {
            worldCamera = Camera.main;
            if (worldCamera == null) return;
        }

        bool hideAll = true && BuildMode.Active;

        //Check markers and update their positions, delete them, or create new ones
        seen.Clear();
        foreach (var station in WorkStation.All)
        {
            if (station == null) continue;

            //One marker per station
            Transform key = station.HasRoot ? station.Root : station.transform;
            if (key == null) continue;

            if (!markers.TryGetValue(key, out StationNavMarker marker) || marker == null)
            {
                marker = CreateMarker();
                markers[key] = marker;
            }

            station.Marker = marker;
            if (!seen.Add(key)) continue;

            //Update marker position and label
            marker.SetLabel(station.stationName);
            marker.SetArrow(station.markerImage);
            marker.SetTint(station.markerTint);

            if (marker.label != null) marker.label.gameObject.SetActive(showLabels);

            UpdateMarker(marker, key.position, hideAll);
        }

        //Remove markers whose station no longer exists
        removeMarkers.Clear();
        foreach (var pair in markers)
            if (pair.Key == null || !seen.Contains(pair.Key)) removeMarkers.Add(pair.Key);

        foreach (var key in removeMarkers)
        {
            if (markers.TryGetValue(key, out StationNavMarker marker) && marker != null)
                Destroy(marker.gameObject);
            markers.Remove(key);
        }
    }
    //Create a new marker
    private StationNavMarker CreateMarker()
    {
        StationNavMarker marker = Instantiate(markerPrefab, rect);

        //Markers are only visual
        foreach (var graphic in marker.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;

        marker.IsShown = true;
        return marker;
    }
    //Update the position of a marker based on the world position of the station
    private void UpdateMarker(StationNavMarker marker, Vector3 worldPos, bool hideAll)
    {
        Vector3 sp = worldCamera.WorldToScreenPoint(worldPos);
        bool behind = sp.z < 0f;

        bool onScreen = !behind &&
                        sp.x >= -onScreenMargin && sp.x <= Screen.width + onScreenMargin &&
                        sp.y >= -onScreenMargin && sp.y <= Screen.height + onScreenMargin;

        bool show = !hideAll && !onScreen;
        if (marker.IsShown != show)
        {
            marker.IsShown = show;
            marker.gameObject.SetActive(show);
        }
        if (!show) return;

        //Area markers may sit in area
        Rect safe = Screen.safeArea;
        Rect bounds = new Rect(safe.x + edgePadding, safe.y + edgePadding, Mathf.Max(1f, safe.width - edgePadding * 2f),
                                Mathf.Max(1f, safe.height - edgePadding * 2f));

        Vector2 center = bounds.center;
        Vector2 dir = new Vector2(sp.x, sp.y) - center;
        if (behind) dir = -dir;
        if (dir.sqrMagnitude < 0.01f) dir = Vector2.up;

        //Slide along the direction until it hits the edge of the bounds
        float scaleX = Mathf.Abs(dir.x) > 0.0001f ? bounds.width * 0.5f / Mathf.Abs(dir.x) : float.MaxValue;
        float scaleY = Mathf.Abs(dir.y) > 0.0001f ? bounds.height * 0.5f / Mathf.Abs(dir.y) : float.MaxValue;
        Vector2 edgePoint = center + dir * Mathf.Min(scaleX, scaleY);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, edgePoint, canvasCamera, out Vector2 local);
        marker.transform.localPosition = local;
    }
}
