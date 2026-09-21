using System.Collections.Generic;
using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    public GameObject customerPrefab;
    public List<GameObject> customers;
    public Transform spawnPoint;
    public GameObject storeFrontPoint;
    public GameObject entryPoint;

    //-----------------------------------------------------------------------
    //No longer needed: customers find stations through foodStation.AllStations
    // public foodStation station1;
    // public foodStation station2;
    // public foodStation station3;
    //-----------------------------------------------------------------------
    
    public float minSpawnInterval = 0.1f; //MinSpawnInterval before spawning a customer
    [Range(0.5f, 0.99f)] public float dayDecay = 0.5f;   //Lower = difficulty ramps faster

    public PlayerInfo playerInfo;

    [System.Serializable]
    public struct pathWays
    {
        public GameObject rageLeaving;
        public GameObject exitPoint;
        public GameObject despawnPoint;
    }
    public List <pathWays> customerPaths;

    public float spawnInterval = 5;

    [SerializeField]private int currentDifficulty = 1;
    private bool started;

    private void OnEnable()
    {
        PlayerInfo.DifficultyChanged += OnDifficultyChanged;
    }


    private void OnDisable()
    {
        PlayerInfo.DifficultyChanged -= OnDifficultyChanged;
    }

    private void OnDifficultyChanged(int newDifficulty)
    {
        currentDifficulty = newDifficulty;

        switch (newDifficulty)
        {
            case 0:
                spawnInterval = 10;
                break;
            case 1:
                spawnInterval = 7;
                break;
            case 2:
                spawnInterval = 4;
                break;
            case 3:
                spawnInterval = 1;
                break;
            case 4:
                spawnInterval = 0.1f;
                break;
        }

        // restart the repeating spawn so the new interval takes effect
        spawnInterval = GetInterval(spawnInterval, playerInfo.day);

        if (started)
        {
            CancelInvoke(nameof(SpawnCustomer));
            InvokeRepeating(nameof(SpawnCustomer), 1f, spawnInterval);
        }
    }

    // Returns the spawn interval for a given base interval and day
    private float GetInterval(float baseInterval, int day)
    {
        //Keeps minimum spawn rate
        float floor = Mathf.Min(minSpawnInterval, baseInterval);
        //Difficulty gets harder the more days go on and makes sure it never gets to min spawn rate
        return floor + (baseInterval - floor) * Mathf.Pow(dayDecay, Mathf.Max(0, day - 1));
    }

    private void Start()
    {
        OnDifficultyChanged(PlayerInfo.CurrentDifficulty);
        started = true;
        InvokeRepeating(nameof(SpawnCustomer), 1f, spawnInterval);
    }

    private void SpawnCustomer()
    {
        //Choose random customer
        int randomIndex = Random.Range(0, customers.Count);
        customerPrefab = customers[randomIndex];

        GameObject customer = Instantiate(customerPrefab, spawnPoint.position, Quaternion.identity);

        averageCustomer customerScript = customer.GetComponent<averageCustomer>();

        // adds movement point refs from spawner to customers since i cant prefab it
        int randomChosenPath = Random.Range(0, customerPaths.Count);
        customerScript.exitPoint = customerPaths[randomChosenPath].exitPoint;
        customerScript.despawnPoint = customerPaths[randomChosenPath].despawnPoint;
        customerScript.rageLeaving = customerPaths[randomChosenPath].rageLeaving;

        customerScript.storeFrontPoint = storeFrontPoint;
        customerScript.entryPoint = entryPoint;

        //-----------------------------------------------------------------------
        //No longer needed
        // customerScript.station1 = station1;
        //-----------------------------------------------------------------------


    }
}