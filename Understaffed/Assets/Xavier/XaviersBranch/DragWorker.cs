using UnityEngine;
using UnityEngine.AI;

public class DragWorker : MonoBehaviour
{
    public static DragWorker Instance { get; private set; }

    //Checks radius to assign workstation and also a valid nav mesh
    [SerializeField] private float dropRadius = 2f;
    [SerializeField] private float navSampleRadius = 2f;

    private void Awake()
    {
        Instance = this;
    }

    //When player releases picked up worker
    public void ResolveDrop(Worker worker, SimpleNavigation instance, Vector3 dropWorldPosition)
    {
        if (worker == null || WorkerIndex.Instance == null) return;

        // Hand the instance back to normal NavMesh control before resolving the assignment
        if (instance != null)
        {
            NavMeshAgent agent = instance.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.enabled = true;

                if (NavMesh.SamplePosition(dropWorldPosition, out NavMeshHit navHit, navSampleRadius, NavMesh.AllAreas))
                    agent.Warp(navHit.position);
                else
                    agent.Warp(instance.transform.position);
            }

            instance.IsSuspended = false;
        }

        //Get reference to nearest station
        UpgradeManager targetStation = FindNearestStation(dropWorldPosition);

        if (targetStation != null)
        {
            // Dropped near a station
            WorkerIndex.Instance.NewWorkerTask(worker, targetStation);
        }
        else
        {
            // Dropped in empty space
            UpgradeManager sourceStation = WorkerIndex.Instance.GetCurrentStationFor(worker);
            if (sourceStation != null)
            {
                Worker defaultWorker = WorkerIndex.Instance.GetDefaultWorker();
                sourceStation.ChangeWorker(defaultWorker);
                sourceStation.RefreshDropdownSelection();
            }
        }
    }

    //Find nearest station function
    private UpgradeManager FindNearestStation(Vector3 worldPosition)
    {
        UpgradeManager nearest = null;
        float nearestDist = dropRadius;

        //Check through all the workstations
        foreach (var station in UpgradeManager.AllStations)
        {
            if (station.WorkerStandPoint == null) continue;

            //Checks the distance of each and takes note of the strongest
            float dist = Vector3.Distance(worldPosition, station.WorkerStandPoint.position);
            if (dist <= nearestDist)
            {
                nearestDist = dist;
                nearest = station;
            }
        }

        return nearest;
    }
}