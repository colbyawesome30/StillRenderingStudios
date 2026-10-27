using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

public class BuildItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Camera worldCamera;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask blockingLayers;      // stations, walls, shelves
    [SerializeField] private GameObject stationPrefab;      // the ROOT station prefab
    [SerializeField] private GameObject ghostPrefab;        // visual only: no colliders, no scripts
    [SerializeField] private Vector3 footprintHalfExtents = new Vector3(1f, 1f, 1f);
    [SerializeField] private float gridSize = 1f;
    [SerializeField] private int cost = 100;
    [SerializeField] private PlayerInfo playerInfo;
    [SerializeField] private Rebake rebake;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip placeSound;
    [SerializeField] private AudioClip failSound;
    [SerializeField] private StationMover stationMover;

    private GameObject ghost;
    private bool valid;
    private Quaternion rot = Quaternion.identity;

    //Set references on awake
    private void Awake()
    {
        if (stationMover == null) stationMover = FindAnyObjectByType<StationMover>();
        if (worldCamera == null) worldCamera = Camera.main;
        if (playerInfo == null) playerInfo = FindAnyObjectByType<PlayerInfo>();
        if (rebake == null) rebake = FindAnyObjectByType<Rebake>();
    }

    //play sound on begin drag and start the clone
    public void OnBeginDrag(PointerEventData e)
    {
        if (!BuildMode.Active) return;
        if (playerInfo.playerFunds < cost)
        {
            audioSource.PlayOneShot(failSound);
            return;
        }
        rot = Quaternion.Euler(0f, 90f * BuildMode.PlaceRotationSteps, 0f);
        ghost = Instantiate(ghostPrefab);
        ghost.SetActive(false);
    }
    //On Drag set item position and clear visual clone
    public void OnDrag(PointerEventData eventData)
    {
        if (ghost == null) return;
        if (Input.GetKeyDown(KeyCode.R)) rot *= Quaternion.Euler(0f, 90f, 0f);

        Ray ray = worldCamera.ScreenPointToRay(eventData.position);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
        {
            Vector3 pos = Snap(hit.point);
            ghost.SetActive(true);
            ghost.transform.SetPositionAndRotation(pos, rot);
            valid = IsValid(pos, rot);
            Tint(valid);
        }
        else
        {
            ghost.SetActive(false);
            valid = false;
        }
    }
    //On end drag destroy clones and try to set placement
    public void OnEndDrag(PointerEventData eventData)
    {
        if (ghost == null) return;

        // Dropping back on the palette cancels
        bool overUI = EventSystem.current.IsPointerOverGameObject(eventData.pointerId);

        if (valid && !overUI && playerInfo.playerFunds >= cost)
        {
            GameObject placed = Instantiate(stationPrefab, ghost.transform.position, ghost.transform.rotation);
            foreach (var workStation in placed.GetComponentsInChildren<WorkStation>(true))
                workStation.SetRoot(placed.transform);

            ghost.SetActive(false);
            stationMover.BeginPlacement(placed, cost);
        }
        else
        {
            audioSource.PlayOneShot(failSound);
        }

        Destroy(ghost);
        ghost = null;
        valid = false;
    }

    //Snap object to grid for movement
    private Vector3 Snap(Vector3 p) =>
        new Vector3(Mathf.Round(p.x / gridSize) * gridSize, p.y, Mathf.Round(p.z / gridSize) * gridSize);

    //Check if position for validity
    private float navSampleRadius = 3f;

    private bool IsValid(Vector3 pos, Quaternion rotation)
    {
        Vector3 center = pos + Vector3.up * footprintHalfExtents.y;
        Collider[] hits = Physics.OverlapBox(center, footprintHalfExtents, rotation, blockingLayers, QueryTriggerInteraction.Ignore);
        if (hits.Length > 0)
        {
            return false;
        }

        bool onMesh = NavMesh.SamplePosition(pos, out _, navSampleRadius, NavMesh.AllAreas);
        return onMesh;
    }

    //Change color of item
    private void Tint(bool ok)
    {
        Color color = ok ? new Color(0.4f, 1f, 0.4f, 0.6f) : new Color(1f, 0.4f, 0.4f, 0.6f);
        foreach (var rot in ghost.GetComponentsInChildren<Renderer>()) rot.material.color = color;
    }

    private void OnEnable()  => BuildMode.Rotated += OnRotated;
    private void OnDisable() => BuildMode.Rotated -= OnRotated;

    //Rotate item

    private void OnRotated(int dir)
    {
        if (ghost == null) return;
        rot = Quaternion.Euler(0f, 90f * BuildMode.PlaceRotationSteps, 0f);
        ghost.transform.rotation = rot;
        valid = IsValid(ghost.transform.position, rot);
        Tint(valid);
    }
}