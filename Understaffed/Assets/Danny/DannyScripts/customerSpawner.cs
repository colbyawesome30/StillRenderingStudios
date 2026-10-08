using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    public GameObject customerPrefab;
    public List<GameObject> customers;
    public Transform spawnPoint;
    [SerializeField] private ParkingManager parkingManager;

    [System.Serializable] public struct StoreFronts
    {
        public GameObject storeFrontPoint;
        public GameObject entryPoint;
    }

    public List<StoreFronts> entryList;

    //-----------------------------------------------------------------------
    //No longer needed: customers find stations through foodStation.AllStations
    // public foodStation station1;
    // public foodStation station2;
    // public foodStation station3;
    //-----------------------------------------------------------------------
    
    public float minSpawnInterval = 0.1f; //MinSpawnInterval before spawning a customer
    [Range(0.5f, 0.99f)] public float dayDecay = 0.5f;   //Lower = difficulty ramps faster

    public PlayerInfo playerInfo;

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
                spawnInterval = 8;
                break;
            case 2:
                spawnInterval = 6;
                break;
            case 3:
                spawnInterval = 4;
                break;
            case 4:
                spawnInterval = 3;
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
        parkingManager.TrySpawnCar();
    }

    // Called by the car when it finishes parking
    public void SpawnCustomerAt(CarMovement car)
    {
        Transform spawn = car.CustomerSpawn;

        if (customers.Count == 0 || entryList.Count == 0 || entryList.Count == 0) return;

        //Choose random customer
        customerPrefab = customers[Random.Range(0, customers.Count)];
        GameObject customer = Instantiate(customerPrefab, spawn.position, Quaternion.identity);

        
        averageCustomer customerScript = customer.GetComponent<averageCustomer>();
        if (customerScript == null)
        {
            Destroy(customer);
            return;
        }
        //Set leaving, rage, and despawn to one gameobject
        customerScript.exitPoint = spawn.gameObject;
        customerScript.rageLeaving = spawn.gameObject;
        customerScript.despawnPoint = spawn.gameObject; // walk back to their own car

        StoreFronts entry = entryList[Random.Range(0, entryList.Count)];
        customerScript.storeFrontPoint = entry.storeFrontPoint;
        customerScript.entryPoint = entry.entryPoint;

        customer.AddComponent<CustomerCarLink>().car = car;
    }
}