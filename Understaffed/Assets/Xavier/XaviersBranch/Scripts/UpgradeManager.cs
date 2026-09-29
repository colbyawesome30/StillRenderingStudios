using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class UpgradeManager : MonoBehaviour
{
    //Reference to player info
    public PlayerInfo playerInfo;
    [SerializeField] private GameObject upgradeButton;

    //Reference to worker
    public Worker currentWorker;
    public Text workerName;

    //Get list of all work stations
    public static readonly List<UpgradeManager> AllStations = new List<UpgradeManager>();

    //Remove this or add this to the list
    private void OnEnable() => AllStations.Add(this);
    private void OnDisable() => AllStations.Remove(this);

    //Possible Upgrades
    [System.Serializable]
    public struct Upgrades
    {
        //Upgrade name
        public string upgradeName;
        //Speed of worker or station
        public float localWorkSpeed;
        //Line capacity of station
        public int localLineCapacity;
        //Upgrade cost
        public int localUpgradeCost;
    }

    public List<Upgrades> upgrades;

    [SerializeField]private int currentUpgradeLevel = 0;

    //Upgrades stack on top making easier to change things.
    protected float workSpeed = 0;
    protected int upgradeCost = 0;
    
    [SerializeField]private Text speedFuture;
    [SerializeField]private Text speedCurrent;
    [SerializeField]private Text lineCapacityFuture;
    [SerializeField]private Text lineCapacityCurrent;
    [SerializeField]private Text upgradeCostCurrent;
    //Old dropdown logic
    [SerializeField] private TMP_Dropdown workerDropdown;

    //Where the worker stands when they're working this station
    [SerializeField] private Transform workerStandPoint;
    public Transform WorkerStandPoint => workerStandPoint;

    //Could multiple by difficulty to make line shorter or longer
    protected int lineCapacity;

    //Set player info at start
    private void Start()
    {
        playerInfo = FindFirstObjectByType<PlayerInfo>();
        UpgradeShop();
        Debug.Log("Player Info Found");

        if (currentWorker == null)
        {
            currentWorker = WorkerIndex.Instance?.GetDefaultWorker();
        }

        //Load stats for whoever the starting worker is
        if (currentWorker != null)
        {
            checkWorker();
        }
        else
        {
            chooseRandomName();
        }

        //Send worker at start
        WorkerIndex.Instance?.Task(currentWorker, this);
        PopulateDropdown();
    }

    //Upgrade shop if player has enough funds
    public void UpgradeShop()
    {
        //Upgrade shop
        if (playerInfo.playerFunds >= upgradeCost)
        {
            playerInfo.playerFunds = playerInfo.playerFunds - upgradeCost;
            Debug.Log("Shop Upgraded");

            //If max level keep current stats
            if (currentUpgradeLevel == upgrades.Count - 1)
            {
                Debug.Log("Max Level!!!");
                upgradeButton.GetComponent<UnityEngine.UI.Button>().interactable = false;

                speedCurrent.text = speedCurrent.ToString();
                lineCapacityCurrent.text = lineCapacity.ToString();
                upgradeCostCurrent.text = "Max";
            }
            else
            {
                //Set upgrade stats
                currentUpgradeLevel++;
                upgradeCost = upgradeCost + upgrades[currentUpgradeLevel].localUpgradeCost;
                workSpeed = workSpeed + upgrades[currentUpgradeLevel].localWorkSpeed;
                //Multiple difficulty eventually to make line shorter or longer
                lineCapacity = lineCapacity + upgrades[currentUpgradeLevel].localLineCapacity;

                //Set current variables for UI
                speedCurrent.text = workSpeed.ToString();
                lineCapacityCurrent.text = lineCapacity.ToString();
                upgradeCostCurrent.text = upgradeCost + "$".ToString();

                RefreshStats();

                if (!(currentUpgradeLevel + 1 < upgrades.Count))
                {
                    upgradeCostCurrent.text = "Max";
                    upgradeButton.GetComponent<UnityEngine.UI.Button>().interactable = false;

                }
            }

        }
        else
        {
            Debug.Log("Not enough funds to upgrade shop");
        }
    }

    //Show what the next upgrade level would give
    private void RefreshStats()
    {
        bool hasNextLevel = currentUpgradeLevel + 1 < upgrades.Count;

        if (hasNextLevel)
        {
            float nextSpeed = workSpeed + upgrades[currentUpgradeLevel + 1].localWorkSpeed;
            int nextLineCapacity = lineCapacity + upgrades[currentUpgradeLevel + 1].localLineCapacity;

            speedFuture.text = nextSpeed.ToString();
            lineCapacityFuture.text = nextLineCapacity.ToString();
        }
        else
        {
            speedFuture.text = "Max";
            lineCapacityFuture.text = "Max";
        }
    }

    //Load the current worker's stats into the station
    public void checkWorker()
    {
        if (currentWorker.affectsStation)
        {
            workSpeed = currentWorker.workSpeed + upgrades[currentUpgradeLevel].localWorkSpeed;
            if (currentWorker.affectsLine)
            {
                lineCapacity = currentWorker.lineCapacity + upgrades[currentUpgradeLevel].localLineCapacity;
                lineCapacityCurrent.text = lineCapacity.ToString();
            }
            speedCurrent.text = workSpeed.ToString();
        }
        
        workerName.text = "Clerk Name: " + currentWorker.workerName;  
        

    }
    
    //Swap this station's worker out for a new one
    public void ChangeWorker(Worker newWorker)
    {
        Worker previousWorker = currentWorker;

        //Take away the old worker stats
        if (currentWorker != null && currentWorker.affectsStation)
        {
            workSpeed -= currentWorker.workSpeed;
            if (currentWorker.affectsLine) lineCapacity -= currentWorker.lineCapacity;
        }

        currentWorker = newWorker;
        WorkerIndex.Instance?.UpdateTask(this, previousWorker, newWorker);

        //Add the new worker's stat bonus
        if (currentWorker != null && currentWorker.affectsStation)
        {
            workSpeed += currentWorker.workSpeed;
            if (currentWorker.affectsLine) lineCapacity += currentWorker.lineCapacity;
        }

        //Default worker gets a random name
        if (currentWorker != null && currentWorker != WorkerIndex.Instance?.GetDefaultWorker())
        {
            workerName.text = "Clerk Name: " + currentWorker.workerName;
        }
        else
        {
            chooseRandomName();
        }

        speedCurrent.text = workSpeed.ToString();
        lineCapacityCurrent.text = lineCapacity.ToString();
        RefreshStats();
    }

    //Pick a random name for worker
    public void chooseRandomName()
    {
        string[] workerNames = {"Timmy","Karen","James","George","Maddie"};
        System.Random random = new System.Random();
        int randomIndex = random.Next(workerNames.Length);

        workerName.text = "Clerk Name: " + workerNames[randomIndex];
    }

    //Fill the dropdown with all available worker names
    private void PopulateDropdown()
    {
        if (workerDropdown == null || WorkerIndex.Instance == null)
        {
            Debug.LogWarning("Worker dropdown or WorkerIndex.Instance not set.");
            return;
        }

        workerDropdown.ClearOptions();
        workerDropdown.AddOptions(WorkerIndex.Instance.GetWorkerNames());

        workerDropdown.onValueChanged.AddListener(OnWorkerDropdownChanged);
    }

    //Player picks a worker from the dropdown
    private void OnWorkerDropdownChanged(int selectedIndex)
    {
        Worker selectedWorker = WorkerIndex.Instance.GetWorkerAt(selectedIndex);

        if (selectedWorker == null)
        {
            Debug.LogWarning($"No worker found at dropdown index {selectedIndex}.");
            return;
        }

        WorkerIndex.Instance.NewWorkerTask(selectedWorker, this);
    }

    //update workers available
    public void RefreshDropdownSelection()
    {
        if (workerDropdown == null || WorkerIndex.Instance == null) return;

        int index = WorkerIndex.Instance.GetIndexOfWorker(currentWorker);
        workerDropdown.SetValueWithoutNotify(index);
    }

    //Stop when this station is destroyed
    private void OnDestroy()
    {
        if (workerDropdown != null)
        {
            workerDropdown.onValueChanged.RemoveListener(OnWorkerDropdownChanged);
        }
    }
}
