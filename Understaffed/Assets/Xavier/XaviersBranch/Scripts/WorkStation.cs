using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class WorkStation : MonoBehaviour, IInteractable
{
    public GameObject stationOutline;
    public GameObject stationUI;
    public string stationName;
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
    private void OnDisable() => All.Remove(this);
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