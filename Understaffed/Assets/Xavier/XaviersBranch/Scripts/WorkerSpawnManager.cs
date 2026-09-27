using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class WorkerSpawnManager : MonoBehaviour
{
    public static WorkerSpawnManager Instance { get; private set; }

    //Set Spawn and worker
    [System.Serializable]
    public struct SpawnPointEntry
    {
        public Worker worker;
        public Transform spawnPoint;
    }

    //Different possible spawns
    [SerializeField] private List<SpawnPointEntry> spawnPoints;

    //Find worker spawns
    private Dictionary<Worker, Transform> spawnLookup = new Dictionary<Worker, Transform>();

    //One worker per each type
    private Dictionary<Worker, SimpleNavigation> activeAgents = new Dictionary<Worker, SimpleNavigation>();

    //Same workers, but default worker gets one copy per station instead of one total
    private Dictionary<UpgradeManager, SimpleNavigation> defaultAgents = new Dictionary<UpgradeManager, SimpleNavigation>();

    //Set spawns
    private void Awake()
    {
        Instance = this;

        foreach (var entry in spawnPoints)
            spawnLookup[entry.worker] = entry.spawnPoint;
    }

    //Change worker path
    public void UpdateWorkerDestination(Worker worker, UpgradeManager station, Transform stationTarget)
    {
        if (worker == null) return;

        //Check if this is the same worker
        bool isDefault = worker == WorkerIndex.Instance?.GetDefaultWorker();

        //Default workers have their location changed differently, one per station
        if (isDefault)
        {
            UpdateDefaultWorkerDestination(worker, station, stationTarget);
            return;
        }

        //Spawn or find the worker and send them to new station
        if (stationTarget != null)
        {
            SimpleNavigation agent = GetOrSpawnAgent(worker);
            agent?.GoToStation(stationTarget);
        }
        //None means they go to spawn and despawn
        else if (activeAgents.TryGetValue(worker, out SimpleNavigation agent))
        {
            agent.ReturnToSpawnAndDespawn(() =>
            {
                activeAgents.Remove(worker);
                Destroy(agent.gameObject);
            });
        }
    }

    //One copy per station
    private void UpdateDefaultWorkerDestination(Worker worker, UpgradeManager station, Transform stationTarget)
    {
        if (station == null)
        {
            Debug.LogWarning("UpdateDefaultWorkerDestination called with a null station reference.");
            return;
        }

        //Update station and find a path
        if (stationTarget != null)
        {
            if (!defaultAgents.TryGetValue(station, out SimpleNavigation agent))
            {
                agent = SpawnInstance(worker);
                if (agent == null) return;
                defaultAgents[station] = agent;
            }
            agent.GoToStation(stationTarget);
        }
        //None means they go to spawn and despawn
        else if (defaultAgents.TryGetValue(station, out SimpleNavigation agent))
        {
            agent.ReturnToSpawnAndDespawn(() =>
            {
                defaultAgents.Remove(station);
                Destroy(agent.gameObject);
            });
        }
    }

    //Check if worker is alive or dead
    private SimpleNavigation GetOrSpawnAgent(Worker worker)
    {
        if (activeAgents.TryGetValue(worker, out SimpleNavigation existing))
            return existing;

        SimpleNavigation navAgent = SpawnInstance(worker);
        if (navAgent != null)
            activeAgents[worker] = navAgent;

        return navAgent;
    }

    //Creates a new worker in the scene at their spawn point
    private SimpleNavigation SpawnInstance(Worker worker)
    {
        //Worker spawn
        if (!spawnLookup.TryGetValue(worker, out Transform spawn))
        {
            Debug.LogWarning($"No spawn point registered for worker {worker.workerName}");
            return null;
        }

        //Spawn worker game object
        GameObject instance = Instantiate(worker.gameObject, spawn.position, spawn.rotation);

        //Make sure it has a navigation script and set its spawn
        SimpleNavigation navAgent = instance.GetComponent<SimpleNavigation>() ?? instance.AddComponent<SimpleNavigation>();
        navAgent.Initialize(spawn);

        WorldDrag draggable = instance.GetComponent<WorldDrag>() ?? instance.AddComponent<WorldDrag>();
        draggable.Initialize(worker, navAgent);


        return navAgent;
    }

    // Called when a UI roster drag begins: gets (or spawns) this worker's world instance
    // and disables its NavMeshAgent so the caller can move its transform freely.
    public SimpleNavigation BeginManualControl(Worker worker, Vector3 initialWorldPosition)
    {
        bool isDefault = worker == WorkerIndex.Instance?.GetDefaultWorker();
        SimpleNavigation nav;

        if (!isDefault && activeAgents.TryGetValue(worker, out nav))
        {
            // Already exists somewhere in the world — pick up that same instance
        }
        else
        {
            nav = SpawnInstance(worker);
            if (nav == null) return null;

            if (!isDefault) activeAgents[worker] = nav;
        }

        NavMeshAgent agent = nav.GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = false;

        nav.IsSuspended = true;
        nav.transform.position = initialWorldPosition;

        return nav;
    }
}