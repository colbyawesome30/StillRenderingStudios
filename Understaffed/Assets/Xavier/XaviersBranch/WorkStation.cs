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

    public List<WorkStation> workStationsList = new List<WorkStation>();

    //Add other workstations refs and initialize basic variables
    public void Start()
    {
        stationText.text = stationName.ToString();
        stationUI.SetActive(false);
        stationOutline.SetActive(false);

        workStationsList = new List<WorkStation>(FindObjectsOfType<WorkStation>());
        workStationsList.Remove(this);
    }


    //What happens when interacted with
    public void Interact(RaycastHit hit)
    {
        Debug.Log($"{gameObject.name} was tapped at {hit.point}");
        ToggleSelected(true);
    }

    //Toggle view
    public void ToggleSelected(bool isSelected)
    {
        if (isSelected)
        {
            // Deselect every other station's visuals directly (no recursion)
            foreach (var station in workStationsList)
            {
                station.SetVisuals(false);
            }
        }
        SetVisuals(isSelected);
    }

    // Only sets this station's own visuals, never calls ToggleSelected on anyone
    private void SetVisuals(bool isSelected)
    {
        stationUI.SetActive(isSelected);
        stationOutline.SetActive(isSelected);
    }
}