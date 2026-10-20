using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class WorkStation : MonoBehaviour, IInteractable
{
    public string stationName;
    public Color markerTint = Color.white;
    public string markerLabelOverride;
    public Sprite markerImage;

    public string MarkerLabel =>
        string.IsNullOrEmpty(markerLabelOverride) ? stationName : markerLabelOverride;

    public GameObject stationOutline;
    public GameObject stationUI;
    [SerializeField]private Text stationText;
    public UpgradeManager upgradeManager;
    private Tutorial tutorial;
    public int buildCost = 100;

    public List<WorkStation> workStationsList = new List<WorkStation>();
    private static WorkStation selected;
    public static bool UIOpen => selected != null && selected.stationUI != null && selected.stationUI.activeSelf;

    [SerializeField] private Transform stationRoot;
    private Transform runtimeRoot;
    public bool HasRoot => runtimeRoot != null || stationRoot != null;
    public bool lockedInBuildMode = false;

    //Unmanned station.. get to it!!!
    public Color alertColor = new Color(1f, 0.25f, 0.25f, 1f);
    public float blinkInterval = 0.4f;

    // set by whatever spawns the nav marker for this station
    public StationNavMarker Marker { get; set; }

    private readonly HashSet<Object> attentionSources = new HashSet<Object>();
    public bool NeedsAttention => attentionSources.Count > 0;

    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private readonly List<Renderer> blinkRenderers = new List<Renderer>();
    private MaterialPropertyBlock blinkBlock;
    private bool blinkingVisuals;

    //Station blinks while any of them needs attention
    public void SetNeedsAttention(Object source, bool needs)
    {
        if (needs) attentionSources.Add(source);
        else attentionSources.Remove(source);
    }

    //Station root
    public Transform Root
    {
        get
        {
            if (runtimeRoot != null) return runtimeRoot;
            if (stationRoot != null) return stationRoot;
            Debug.LogWarning("WorkStation: Station Root not set on " + name + ", guessing the parent.", this);
            return transform.parent != null ? transform.parent : transform;
        }
    }

    public void SetRoot(Transform t) => runtimeRoot = t;

    public static readonly List<WorkStation> All = new List<WorkStation>();
    private void OnEnable()  => All.Add(this);
    private void OnDisable()
    {
        All.Remove(this);
        StopBlink();
    }
    //Close upgrade tab
    public static void CloseSelected() { if (selected != null) selected.ToggleSelected(false); }
    //Add other workstations refs and initialize basic variables
    public void Start()
    {
        stationText.text = stationName.ToString();
        stationUI.SetActive(false);
        stationOutline.SetActive(false);
        
        tutorial = FindAnyObjectByType<Tutorial>();
    }

    private void Update()
    {
        //No blinking while building
        bool want = NeedsAttention && !BuildMode.Active;
        if (!want)
        {
            if (blinkingVisuals) StopBlink();
            return;
        }

        if (!blinkingVisuals) StartBlink();

        bool red = Mathf.FloorToInt(Time.unscaledTime / blinkInterval) % 2 == 0;
        ApplyBlink(red);
        if (Marker != null) Marker.SetAlertState(red, alertColor);
    }

    private void StartBlink()
    {
        blinkingVisuals = true;
        if (blinkBlock == null) blinkBlock = new MaterialPropertyBlock();

        // gather now so upgrade visuals spawned later are included
        blinkRenderers.Clear();
        Transform rootT = BlinkRoot();
        foreach (var root in rootT.GetComponentsInChildren<Renderer>(true))
        {
            if (!(root is MeshRenderer || root is SkinnedMeshRenderer)) continue;
            if (stationOutline != null && root.transform.IsChildOf(stationOutline.transform)) continue;
            blinkRenderers.Add(root);
        }
    }
    //Blinking come from
    private Transform BlinkRoot()
    {
        var placeable = GetComponentInParent<Placeable>();
        if (placeable != null) return placeable.transform;

        var mover = FindAnyObjectByType<StationMover>();
        if (mover != null) return mover.ResolveRoot(this);

        return HasRoot ? Root : transform;
    }
    //Apply colors
    private void ApplyBlink(bool red)
    {
        if (red)
        {
            blinkBlock.Clear();
            blinkBlock.SetColor(ColorId, alertColor);
            blinkBlock.SetColor(BaseColorId, alertColor);
        }
        foreach (var r in blinkRenderers)
            if (r != null) r.SetPropertyBlock(red ? blinkBlock : null);   // null = original colors
    }
    //Stop blinking right now!!!!
    private void StopBlink()
    {
        if (!blinkingVisuals) return;
        blinkingVisuals = false;
        foreach (var root in blinkRenderers)
            if (root != null) root.SetPropertyBlock(null);
        blinkRenderers.Clear();
        if (Marker != null) Marker.ClearAlert();
    }

    //What happens when interacted with
    public void Interact(RaycastHit hit)
    {
        if (PayWall.TabOpen) return; 
        if (UIOpen && selected != this) return;

        Debug.Log($"{gameObject.name} was tapped at {hit.point}");
        ToggleSelected(true);

        //Tutorial
        if (tutorial != null && !tutorial.tutorialComplete && tutorial.TutorialStep == 2)
        {
            tutorial.tutorialSteps[tutorial.TutorialStep].stepComplete = true;
            tutorial.NextStep();
            tutorial.CheckStepCompletion();
        }
        //Tutorial
    }

    //Toggle view
    public void ToggleSelected(bool isSelected)
    {
        if (isSelected)
        {
            // Deselect every other station's visuals directly (no recursion)
            foreach (var station in All)
                if (station != this) station.SetVisuals(false);
            
            upgradeManager.audioSource.PlayOneShot(upgradeManager.openSound, 0.5f);
        }
        else
        {
            upgradeManager.audioSource.PlayOneShot(upgradeManager.closeSound, 0.5f);
        }
        SetVisuals(isSelected);
    }

    private void SetVisuals(bool isSelected)
    {
        stationUI.SetActive(isSelected);
        stationOutline.SetActive(isSelected);

        if (isSelected) selected = this;
        else if (selected == this) selected = null;
    }

    private void OnDestroy()
    {
        if (selected == this) selected = null;
    }

    public void SetHighlight(bool on)
    {
        if (stationOutline != null) stationOutline.SetActive(on);
    }
}