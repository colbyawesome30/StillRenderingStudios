using System.Collections.Generic;
using UnityEngine;

public class WorkerIndex : MonoBehaviour
{
    public static WorkerIndex Instance { get; private set; }

    [System.Serializable]
    public struct workerIndex
    {
        public string workerName;
        public GameObject workerPrefab;
    }

    //List of workers
    public List<workerIndex> availableWorkers;

    //Temp list for names
    private List<Worker> fixNames = new List<Worker>();

    //Destroy duplicates
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple WorkerIndex instances found — destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        //Set worker names automatically from their prefab
        for (int i = 0; i < availableWorkers.Count; i++)
        {
            workerIndex entry = availableWorkers[i];
            entry.workerName = entry.workerPrefab.GetComponent<Worker>().workerName;
            availableWorkers[i] = entry;
        }

        fixNames.Clear();
        foreach (var entry in availableWorkers)
        {
            fixNames.Add(entry.workerPrefab.GetComponent<Worker>());
        }
    }

    //Get all worker names for the dropdown list
    public List<string> GetWorkerNames()
    {
        List<string> names = new List<string>();
        foreach (var worker in availableWorkers)
        {
            names.Add(worker.workerName);
        }
        return names;
    }

    //Get a worker by their dropdown index
    public Worker GetWorkerAt(int index)
    {
        if (index < 0 || index >= fixNames.Count)
        {
            Debug.LogWarning($"WorkerIndex: index {index} out of range.");
            return null;
        }

        return fixNames[index];
    }

    //Check workers
    private Dictionary<Worker, UpgradeManager> workerStationMap = new Dictionary<Worker, UpgradeManager>();

    //Check if default work special worker
    public Worker GetDefaultWorker()
    {
        return fixNames.Count > 0 ? fixNames[0] : null;
    }

    //Find a worker's dropdown index
    public int GetIndexOfWorker(Worker worker)
    {
        return fixNames.IndexOf(worker);
    }

    //Preset workers()
    //Called once at Start(), sets a station's starting worker and sends them walking there
    //
    //BUGGED IS NOT WORKING RN CAUSING DUPLICATE NAMES
    //
    public void Task(Worker worker, UpgradeManager station)
    {
        if (worker == null || station == null) return;

        Worker defaultWorker = GetDefaultWorker();

        //Track special workers
        if (worker != defaultWorker)
        {
            workerStationMap[worker] = station;
        }

        //Send worker to walk to this station
        WorkerSpawnManager.Instance?.UpdateWorkerDestination(worker, station, station.WorkerStandPoint);
    }

    //Dropdown selects a worker.
    //Change worker roles for new worker
    public void NewWorkerTask(Worker requestedWorker, UpgradeManager requestingStation)
    {
        Worker defaultWorker = GetDefaultWorker();

        if (requestedWorker == defaultWorker)
        {
            requestingStation.ChangeWorker(requestedWorker);
            return;
        }

        //Find out who's currently at this station and who currently wants the worker
        Worker outgoingWorker = requestingStation.currentWorker;
        workerStationMap.TryGetValue(requestedWorker, out UpgradeManager previousStation);

        //If the worker is already at another station, swap them
        if (previousStation != null && previousStation != requestingStation)
        {
            Worker swapWorker = outgoingWorker != null ? outgoingWorker : defaultWorker;
            previousStation.ChangeWorker(swapWorker);
            previousStation.RefreshDropdownSelection();
        }

        requestingStation.ChangeWorker(requestedWorker);
        //Change worker roles for new worker
    }

    //Change worker task
    //Used when worker moves
    public void UpdateTask(UpgradeManager station, Worker oldWorker, Worker newWorker)
    {
        Worker defaultWorker = GetDefaultWorker();

        //Worker leaving
        if (oldWorker != null)
        {
            if (oldWorker != defaultWorker)
            {
                //Remove if this station still owns them
                if (workerStationMap.TryGetValue(oldWorker, out UpgradeManager mappedStation) && mappedStation == station)
                {
                    workerStationMap.Remove(oldWorker);
                    WorkerSpawnManager.Instance?.UpdateWorkerDestination(oldWorker, station, null);
                }
            }
            else
            {
                // Default worker leaving this station
                WorkerSpawnManager.Instance?.UpdateWorkerDestination(oldWorker, station, null);
            }
        }

        //Worker arriving
        if (newWorker != null)
        {
            if (newWorker != defaultWorker)
                workerStationMap[newWorker] = station;

            //Send worker to walk to this station
            WorkerSpawnManager.Instance?.UpdateWorkerDestination(newWorker, station, station.WorkerStandPoint);
        }
    }
}