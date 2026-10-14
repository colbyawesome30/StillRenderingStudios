using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class StationMover : MonoBehaviour
{
    //Basic variables to set usually pre done
    [SerializeField] private Camera worldCamera;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask blockingLayers;
    [SerializeField] private Vector3 footprintHalfExtents = new Vector3(1f, 1f, 1f);
    [SerializeField] private float gridSize = 1f;
    [SerializeField] private float refundPercent = 0.5f;
    [SerializeField] private PlayerInfo playerInfo;
    [SerializeField] private Rebake rebake;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip placeSound;
    [SerializeField] private AudioClip sellSound;
    [SerializeField] private AudioClip failSound;
    [SerializeField] private bool debugValidity = false;
    [SerializeField] private float navSampleRadius = 3f;
    [SerializeField] private bool requireNavMesh = true;
    //Leave empty
    [SerializeField] private Transform stationContainer;
    //Buttons
    [SerializeField] private GameObject confirmPanel;   // holds the Confirm and Cancel buttons
    [SerializeField] private Button confirmButton;      // disabled while the spot is invalid
    [SerializeField] private GameObject sellButton;     // shown while a station is selected or being edited
    [SerializeField] private Color validColor = new Color(0.4f, 1f, 0.4f, 1f);
    [SerializeField] private Color invalidColor = new Color(1f, 0.35f, 0.35f, 1f);
    private bool isNew;
    private int newCost;
    private WorkStation selected;
    private WorkStation editing;
    private bool dragging;
    private Transform root;
    private Vector3 startPos, grabOffset;
    private Quaternion startRot, rot;
    private bool valid;
    private readonly Collider[] overlapBuffer = new Collider[32];
    //Color settings
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private MaterialPropertyBlock block;
    private List<Renderer> tinted;
    private Coroutine blinkRoutine;

    //Set references
    private void Awake()
    {
        block = new MaterialPropertyBlock();
        tinted = new List<Renderer>();

        if (worldCamera == null) worldCamera = Camera.main;
        if (playerInfo == null) playerInfo = FindAnyObjectByType<PlayerInfo>();
        if (rebake == null) rebake = FindAnyObjectByType<Rebake>();
        if (confirmPanel != null) confirmPanel.SetActive(false);
        if (sellButton != null) sellButton.SetActive(false);
    }

    private void OnEnable()  => BuildMode.Rotated += OnRotated;
    private void OnDisable() => BuildMode.Rotated -= OnRotated;

    //On build mode active check selection
    private void Update()
    {
        if (!BuildMode.Active)
        {
            if (editing != null) Cancel(false);
            Select(null);
            if (sellButton != null) sellButton.SetActive(false);
            return;
        }

        if (sellButton != null) sellButton.SetActive(selected != null || editing != null);

        if (dragging)
        {
            if (!BuildMode.MoveMode && !isNew) { dragging = false; return; }
            Drag();
            if (ReleasedThisFrame()) Drop();
            return;
        }

        if (editing != null) Revalidate();

        if (PressedThisFrame(out Vector2 pos) && !PointerOverUI()) HandlePress(pos);
    }

    //Find root of stations
    private Transform ResolveRoot(WorkStation station)
    {
        if (station.HasRoot)
        {
            Transform rot = station.Root;
            if (station.transform.IsChildOf(rot)) return rot;
        }

        // Fallback: climb to the top object of this station, stopping below the container
        Transform trans = station.transform;
        while (trans.parent != null && trans.parent != stationContainer) trans = trans.parent;
        return trans;
    }

    //Select stations
    private void Select(WorkStation s)
    {
        if (editing != null) return;
        if (selected == s) return;
        if (selected != null) selected.SetHighlight(false);
        selected = s;
        if (selected != null) selected.SetHighlight(true);
    }

    //Check click and player touch
    private void HandlePress(Vector2 screenPos)
    {
        Ray ray = worldCamera.ScreenPointToRay(screenPos);
        WorkStation hitStation = null;

        foreach (var hit in Physics.RaycastAll(ray, 1000f, ~0, QueryTriggerInteraction.Ignore).OrderBy(hit => hit.distance))
        {
            var station = hit.collider.GetComponentInParent<WorkStation>();
            if (station != null) { hitStation = station; break; }
        }

        //Only the edited station can be moved
        if (editing != null)
        {
            if (isNew)
                StartDrag(ray);
            else if (BuildMode.MoveMode && hitStation != null && ResolveRoot(hitStation) == root)
                StartDrag(ray);
            return;
        }

        if (hitStation == null) { Select(null); return; }

        Transform hitRoot = ResolveRoot(hitStation);
        if (IsLocked(hitRoot))
        {
            Select(null);
            BlinkRed(hitRoot);
            return;
        }

        Select(hitStation);
        if (BuildMode.MoveMode)
        {
            BeginEdit(hitStation);
            StartDrag(ray);
        }
    }

    //Start editing transform of station and rotation
    private void BeginEdit(WorkStation station)
    {
        editing = station;
        root = ResolveRoot(station);
        foreach (var ws in root.GetComponentsInChildren<WorkStation>(true))
            ws.SetRoot(root);

        startPos = root.position;
        startRot = root.rotation;
        rot = startRot;
        valid = true;

        tinted.Clear();
        foreach (var rotation in root.GetComponentsInChildren<Renderer>(true))
            if (rotation is MeshRenderer || rotation is SkinnedMeshRenderer) tinted.Add(rotation);
    }

    //Start moving object
    private void StartDrag(Ray ray)
    {
        dragging = true;
        grabOffset = Physics.Raycast(ray, out RaycastHit g, 1000f, groundLayer)
            ? root.position - g.point : Vector3.zero;
        grabOffset.y = 0f;
    }

    //Finish editing in build mode
    private void EndEdit()
    {
        ClearTint();
        if (confirmPanel != null) confirmPanel.SetActive(false);
        editing = null;
        root = null;
        dragging = false;
        isNew = false;
    }

    //Dragging object
    private void Drag()
    {
        Ray ray = worldCamera.ScreenPointToRay(Pointer());
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
        {
            Vector3 pos = Snap(hit.point + grabOffset);
            pos.y = startPos.y;
            root.SetPositionAndRotation(pos, rot);
            Revalidate();
        }
    }

    //Stopped dragging object and station is set down
    private void Drop()
    {
        dragging = false;

        bool changed = (root.position - startPos).sqrMagnitude > 0.01f || Quaternion.Angle(root.rotation, startRot) > 1f;
        if (!changed && !isNew) { EndEdit(); return; }

        Revalidate();
        if (confirmPanel != null) confirmPanel.SetActive(true);
    }

    //Confirm change in rotation and location
    public void Confirm()
    {
        if (editing == null) return;
        if (!valid) { audioSource.PlayOneShot(failSound); return; }

        if (isNew)
        {
            if (playerInfo.playerFunds < newCost) { audioSource.PlayOneShot(failSound); return; }
            playerInfo.playerFunds -= newCost;
        }

        Finish(editing);
        EndEdit();
    }

    public void Cancel() => Cancel(true);

    //Sell station
    public void Sell()
    {
        if (editing == null)
        {
            if (selected == null) return;
            BeginEdit(selected);
        }
        TrySell();
    }

    //Begin placing object
    public void BeginPlacement(GameObject placed, int cost)
    {
        if (editing != null) Cancel(false);
        var ws = placed.GetComponentInChildren<WorkStation>(true);
        if (ws == null) { Destroy(placed); return; }

        isNew = true;
        newCost = cost;
        BeginEdit(ws);
        Revalidate();
        if (confirmPanel != null) confirmPanel.SetActive(true);
    }

    //Cancel changes in build mode
    private void Cancel(bool playSound)
    {
        if (editing == null) return;
        if (playSound) audioSource.PlayOneShot(failSound);

        if (isNew)
        {
            GameObject go = root.gameObject;
            EndEdit();
            Destroy(go);
            return;
        }

        root.SetPositionAndRotation(startPos, startRot);
        Physics.SyncTransforms();
        EndEdit();
    }

    //Change based on rotation check
    private void OnRotated(int dir)
    {
        if (editing == null)
        {
            if (selected == null) return;   // nothing selected: BuildItem handles it
            BeginEdit(selected);
        }

        rot *= Quaternion.Euler(0f, 90f * dir, 0f);
        if (dragging) return;   // Drag() applies it next frame

        root.rotation = rot;
        Revalidate();
        if (confirmPanel != null) confirmPanel.SetActive(true);
    }

    //Check placement is good and does not need to be reworked
    private void Revalidate()
    {
        Physics.SyncTransforms();
        valid = IsValid(root);
        ApplyFeedback();
    }
    //Show placement visual
    private void ApplyFeedback()
    {
        Color color = valid ? validColor : invalidColor;
        block.Clear();
        block.SetColor(ColorId, color);
        block.SetColor(BaseColorId, color);
        foreach (var r in tinted) if (r != null) r.SetPropertyBlock(block);

        if (confirmButton != null) confirmButton.interactable = valid;
    }

    //Stop coloring object
    private void ClearTint()
    {
        foreach (var material in tinted) if (material != null) material.SetPropertyBlock(null);
        tinted.Clear();
    }

    //Finish making changes in station
    private void Finish(WorkStation station)
    {
        UpgradeManager upgradeManager = station.upgradeManager;
        foodStation[] stations = root.GetComponentsInChildren<foodStation>(true);

        System.Action onRebaked = null;
        onRebaked = () =>
        {
            rebake.Rebaked -= onRebaked;   // one-shot
            foreach (var foodStation in stations) if (foodStation != null) foodStation.RefreshLine();
            if (upgradeManager != null && upgradeManager.currentWorker != null)
                WorkerIndex.Instance.Task(upgradeManager.currentWorker, upgradeManager);
        };
        rebake.Rebaked += onRebaked;
        rebake.RebakeMesh();

        audioSource.PlayOneShot(placeSound);
    }

    //Try to sell the station
    private void TrySell()
    {
        if (isNew) { Cancel(true); return; }

        var foodStation = root.GetComponentInChildren<foodStation>(true);

        if (foodStation != null && foodStation.isCheckout && foodStation.AllStations.Count(s => s.isCheckout) <= 1)
        {
            Cancel(true);
            return;
        }

        UpgradeManager upgradeManager = editing.upgradeManager;
        if (upgradeManager != null) WorkerIndex.Instance.ReleaseStation(upgradeManager);
        foreach (var f in root.GetComponentsInChildren<foodStation>(true)) f.Decommission();

        playerInfo.playerFunds += Mathf.RoundToInt(editing.buildCost * refundPercent);
        audioSource.PlayOneShot(sellSound);

        GameObject toDestroy = root.gameObject;
        EndEdit();
        Select(null);
        Destroy(toDestroy);
        rebake.RebakeMesh();
    }

    //Snap to grid for easier movement
    private Vector3 Snap(Vector3 p) =>
        new Vector3(Mathf.Round(p.x / gridSize) * gridSize, p.y, Mathf.Round(p.z / gridSize) * gridSize);

    //Check is change valid
    private bool IsValid(Transform self)
    {
        bool foundOwnBox = false;
        foreach (var box in self.GetComponentsInChildren<BoxCollider>())
        {
            if (((1 << box.gameObject.layer) & blockingLayers.value) == 0) continue;
            foundOwnBox = true;

            Transform t = box.transform;
            Vector3 s = t.lossyScale;
            Vector3 half = Vector3.Scale(box.size, new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z))) * 0.5f;
            half = Vector3.Max(half - Vector3.one * 0.02f, Vector3.one * 0.01f);

            if (Blocked(t.TransformPoint(box.center), half, t.rotation, self)) return false;
        }

        if (!foundOwnBox &&
            Blocked(self.position + Vector3.up * footprintHalfExtents.y, footprintHalfExtents, self.rotation, self))
            return false;

        if (!requireNavMesh) return true;

        bool onMesh = UnityEngine.AI.NavMesh.SamplePosition(self.position, out _, navSampleRadius, UnityEngine.AI.NavMesh.AllAreas);
        if (!onMesh && debugValidity) Debug.Log("Invalid: no nav mesh within " + navSampleRadius + " of " + self.position);
        return onMesh;
    }
    //station is blocked during move
    private bool Blocked(Vector3 center, Vector3 half, Quaternion orientation, Transform self)
    {
        int count = Physics.OverlapBoxNonAlloc(center, half, overlapBuffer, orientation,
                                               blockingLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            if (overlapBuffer[i].transform.IsChildOf(self)) continue;
            if (debugValidity) Debug.Log("Invalid: blocked by " + overlapBuffer[i].name, overlapBuffer[i]);
            return true;
        }
        return false;
    }

    //Check if object is selected
    private bool PressedThisFrame(out Vector2 pos)
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) { pos = Input.GetTouch(0).position; return true; }
        if (Input.GetMouseButtonDown(0)) { pos = Input.mousePosition; return true; }
        pos = default; return false;
    }

    //Release object selected
    private bool ReleasedThisFrame()
    {
        if (Input.touchCount > 0)
            return Input.GetTouch(0).phase == TouchPhase.Ended || Input.GetTouch(0).phase == TouchPhase.Canceled;
        return Input.GetMouseButtonUp(0);
    }

    private Vector2 Pointer() => Input.touchCount > 0 ? Input.GetTouch(0).position : (Vector2)Input.mousePosition;

    private bool PointerOverUI() =>
        EventSystem.current != null &&
        (Input.touchCount > 0
            ? EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)
            : EventSystem.current.IsPointerOverGameObject());

    //Check is station is locked
    private bool IsLocked(Transform stationRoot)
    {
        foreach (var ws in stationRoot.GetComponentsInChildren<WorkStation>(true))
            if (ws.lockedInBuildMode) return true;
        foreach (var fs in stationRoot.GetComponentsInChildren<foodStation>(true))
            if (fs.lockedInBuildMode) return true;
        return false;
    }

    //Call blink red when selected
    private void BlinkRed(Transform target)
    {
        if (blinkRoutine != null) return;   // already blinking
        if (audioSource != null) audioSource.PlayOneShot(failSound);
        blinkRoutine = StartCoroutine(BlinkRoutine(target));
    }

    //Blink red
    private IEnumerator BlinkRoutine(Transform target)
    {
        var renderers = new List<Renderer>();
        foreach (var r in target.GetComponentsInChildren<Renderer>(true))
            if (r is MeshRenderer || r is SkinnedMeshRenderer) renderers.Add(r);

        var redBlock = new MaterialPropertyBlock();
        redBlock.SetColor(ColorId, invalidColor);
        redBlock.SetColor(BaseColorId, invalidColor);

        for (int i = 0; i < 2; i++)
        {
            foreach (var r in renderers) if (r != null) r.SetPropertyBlock(redBlock);
            yield return new WaitForSecondsRealtime(0.15f);
            foreach (var r in renderers) if (r != null) r.SetPropertyBlock(null);
            yield return new WaitForSecondsRealtime(0.15f);
        }
        blinkRoutine = null;
    }
}