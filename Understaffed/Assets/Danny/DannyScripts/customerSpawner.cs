using System;
using System.Collections.Generic;
using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    public Transform spawnPoint;
    [SerializeField] private ParkingManager parkingManager;

    [Serializable]
    public struct StoreFronts
    {
        public GameObject storeFrontPoint;
        public GameObject entryPoint;
    }

    public List<StoreFronts> entryList;

    [Serializable]
    public struct CustomerWaveItem
    {
        public GameObject customer;
        public int amountToBeSpawned;
        public int cost;
        [Min(0f)] public float spawnChance;
    }

    [Serializable]
    public class Wave
    {
        public string name;
        public List<CustomerWaveItem> items = new List<CustomerWaveItem>();
    }

    [Header("Waves")]
    public List<Wave> waves = new List<Wave>();
    public float timeBetweenWaves = 5f;

    [Header("Endless")]
    public bool endlessMode;
    public List<CustomerWaveItem> endlessPool = new List<CustomerWaveItem>(); // uses customer, cost, spawnChance
    public int startingBudget = 10;
    public int budgetPerWave = 5;

    public int CurrentWave { get; private set; } = -1;
    public event Action<int> WaveStarted;
    public event Action<int> WaveCompleted;
    public event Action AllWavesCompleted;

    public float minSpawnInterval = 0.1f;
    [Range(0.5f, 0.99f)] public float dayDecay = 0.5f;

    public PlayerInfo playerInfo;
    public float spawnInterval = 5;

    [SerializeField] private int currentDifficulty = 1;
    private bool spawningStopped;
    public bool SpawningStopped => spawningStopped;
    private bool started;
    private readonly Queue<GameObject> spawnQueue = new Queue<GameObject>();
    private int pendingCars;       // cars requested but not parked yet
    private bool waveActive;
    private bool allDone;
    private float nextWaveTimer;

    private Tutorial tutorial;

    private void OnEnable()  { PlayerInfo.DifficultyChanged += OnDifficultyChanged; }
    private void OnDisable() { PlayerInfo.DifficultyChanged -= OnDifficultyChanged; }

    private void OnDifficultyChanged(int newDifficulty)
    {
        currentDifficulty = newDifficulty;

        switch (newDifficulty)
        {
            case 0: spawnInterval = 10; break;
            case 1: spawnInterval = 8; break;
            case 2: spawnInterval = 6; break;
            case 3: spawnInterval = 4; break;
            case 4: spawnInterval = 3; break;
        }

        spawnInterval = GetInterval(spawnInterval, playerInfo.day);

        if (started && !spawningStopped)
        {
            CancelInvoke(nameof(SpawnCustomer));
            InvokeRepeating(nameof(SpawnCustomer), 1f, spawnInterval);
        }
    }

    private float GetInterval(float baseInterval, int day)
    {
        float floor = Mathf.Min(minSpawnInterval, baseInterval);
        return floor + (baseInterval - floor) * Mathf.Pow(dayDecay, Mathf.Max(0, day - 1));
    }

    private void Start()
    {
        tutorial = FindAnyObjectByType<Tutorial>();
        playerInfo = FindAnyObjectByType<PlayerInfo>();
        OnDifficultyChanged(PlayerInfo.CurrentDifficulty);
        started = true;
        InvokeRepeating(nameof(SpawnCustomer), 1f, spawnInterval);
        StartNextWave();
    }
    private void Update()
    {
        if (!started || spawningStopped || allDone) return;

        if (waveActive)
        {
            bool finished = spawnQueue.Count == 0 && pendingCars == 0 && averageCustomer.ActiveCount == 0;
            if (!finished) return;

            waveActive = false;
            WaveCompleted?.Invoke(CurrentWave);

            if (!endlessMode && CurrentWave >= waves.Count - 1)
            {
                allDone = true;
                AllWavesCompleted?.Invoke();
                return;
            }
            nextWaveTimer = timeBetweenWaves;
        }
        else if (!TutorialBlocksSpawning())
        {
            nextWaveTimer -= Time.deltaTime;
            if (nextWaveTimer <= 0f) StartNextWave();
        }
    }

    public void StartNextWave()
    {
        int index = CurrentWave + 1;
        List<GameObject> roll = BuildWave(index);

        if (roll == null || roll.Count == 0)
        {
            Debug.LogWarning("Wave " + index + " is empty or doesn't exist. Stopping waves.");
            allDone = true;
            if (!endlessMode) AllWavesCompleted?.Invoke();
            return;
        }

        CurrentWave = index;
        spawnQueue.Clear();
        foreach (GameObject c in roll) spawnQueue.Enqueue(c);

        waveActive = true;
        WaveStarted?.Invoke(CurrentWave);
    }

    //Normal waves are the same every time, endless waves are rerolled
    public List<GameObject> BuildWave(int index)
    {
        var result = new List<GameObject>();

        if (endlessMode)
        {
            int budget = startingBudget + budgetPerWave * index;
            while (true)
            {
                int pick = PickAffordable(budget);
                if (pick < 0) break;
                result.Add(endlessPool[pick].customer);
                budget -= Mathf.Max(1, endlessPool[pick].cost);
            }
            return result;
        }

        if (index >= waves.Count) return null;

        foreach (CustomerWaveItem item in waves[index].items)
        {
            if (item.customer == null) continue;
            for (int i = 0; i < item.amountToBeSpawned; i++) result.Add(item.customer);
        }

        //Shuffle so the wave isn't spawned in blocks
        for (int i = result.Count - 1; i > 0; i--)
        {
            int s = UnityEngine.Random.Range(0, i + 1);
            (result[i], result[s]) = (result[s], result[i]);
        }
        return result;
    }

    //Weighted pick among items whose cost fits in the remaining budget. Returns -1 if none.
    private int PickAffordable(int budget)
    {
        float totalWeight = 0f;
        for (int i = 0; i < endlessPool.Count; i++)
        {
            if (!CanAfford(endlessPool[i], budget)) continue;
            totalWeight += endlessPool[i].spawnChance;
        }
        if (totalWeight <= 0f) return -1;

        float roll = UnityEngine.Random.value * totalWeight;
        for (int i = 0; i < endlessPool.Count; i++)
        {
            if (!CanAfford(endlessPool[i], budget)) continue;
            roll -= endlessPool[i].spawnChance;
            if (roll <= 0f) return i;
        }
        return -1;
    }

    private bool CanAfford(CustomerWaveItem item, int budget)
    {
        return item.customer != null && item.spawnChance > 0f && Mathf.Max(1, item.cost) <= budget;
    }

    private void SpawnCustomer()
    {
        if (spawningStopped || !waveActive || TutorialBlocksSpawning()) return;

        //don't send more cars than there are customers left in the queue
        if (spawnQueue.Count - pendingCars <= 0) return;

        pendingCars++;
        if (!parkingManager.TrySpawnCar()) pendingCars--;   // lot full, try again next tick
    }

    private bool TutorialBlocksSpawning()
    {
        if (playerInfo == null || tutorial == null) return false;
        if (playerInfo.TutorialComplete) return false;
        return tutorial.TutorialStep < 6;
    }

    //Called by the car when it finishes parking
    public void SpawnCustomerAt(CarMovement car)
    {
        pendingCars = Mathf.Max(0, pendingCars - 1);

        if (spawningStopped || spawnQueue.Count == 0 || entryList.Count == 0) return;

        Transform spawn = car.CustomerSpawn;
        GameObject prefab = spawnQueue.Dequeue();
        GameObject customer = Instantiate(prefab, spawn.position, Quaternion.identity);

        averageCustomer customerScript = customer.GetComponent<averageCustomer>();
        if (customerScript == null)
        {
            Destroy(customer);
            return;
        }

        customerScript.exitPoint = spawn.gameObject;
        customerScript.rageLeaving = spawn.gameObject;
        customerScript.despawnPoint = spawn.gameObject;

        StoreFronts entry = entryList[UnityEngine.Random.Range(0, entryList.Count)];
        customerScript.storeFrontPoint = entry.storeFrontPoint;
        customerScript.entryPoint = entry.entryPoint;

        customer.AddComponent<CustomerCarLink>().car = car;
    }

    public void StopSpawning()
    {
        spawningStopped = true;
        CancelInvoke(nameof(SpawnCustomer));
    }
}