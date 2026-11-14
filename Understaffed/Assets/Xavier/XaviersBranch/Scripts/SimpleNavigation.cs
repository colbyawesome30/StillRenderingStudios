using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class SimpleNavigation : MonoBehaviour
{
    private NavMeshAgent agent;
    private Transform spawnPoint;
    private System.Action onArrivedAtSpawn;
    private UpgradeManager targetStation;
    private float stoppedTimer;
    private SpriteRenderer spriteRenderer;
    public bool IsSuspended { get; set; }
    public bool workerLeftOrRight { get; set; }

    private const float arriveTolerance = 0.1f;
    private const float stuckTime = 0.75f;

    public enum NavState { MovingToStation, MovingToSpawn, Idle }
    public NavState State { get; private set; } = NavState.Idle;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = 0.1f;
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void Initialize(Transform spawn) => spawnPoint = spawn;

    //Go to work station
    public void GoToStation(Transform stationTarget, UpgradeManager station)
    {
        targetStation = station;
        onArrivedAtSpawn = null;
        State = NavState.MovingToStation;
        stoppedTimer = 0f;
        ResetIdleOrientation();
        agent.isStopped = false;
        agent.SetDestination(stationTarget.position);
    }

    //Called when the worker is picked up: forget the old station completely
    public void DetachFromStation()
    {
        targetStation = null;
        State = NavState.Idle;
        workerLeftOrRight = false;
        ResetIdleOrientation();
    }

    //Go back and despawn
    public void ReturnToSpawnAndDespawn(System.Action onArrived)
    {
        targetStation = null;
        onArrivedAtSpawn = onArrived;
        State = NavState.MovingToSpawn;
        stoppedTimer = 0f;
        ResetIdleOrientation();
        agent.isStopped = false;
        agent.SetDestination(spawnPoint.position);
    }

    private void Update()
    {
        if (IsSuspended) return;

        //While idle at a station, always match the station's current facing
        if (State == NavState.Idle)
        {
            if (targetStation != null) SetIdleOrientation();
            return;
        }

        if (agent.pathPending) return;

        bool stopped = !agent.hasPath || agent.velocity.sqrMagnitude < 0.01f;
        stoppedTimer = stopped ? stoppedTimer + Time.deltaTime : 0f;

        bool arrived = stopped &&
                       (agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, arriveTolerance)
                        || stoppedTimer >= stuckTime);

        if (!arrived) return;

        if (State == NavState.MovingToSpawn)
        {
            State = NavState.Idle;
            onArrivedAtSpawn?.Invoke();
            onArrivedAtSpawn = null;
        }
        else
        {
            State = NavState.Idle;
            SetIdleOrientation();
        }
    }

    public void SetIdleOrientation()
    {
        if (targetStation != null)
            workerLeftOrRight = targetStation.workerLeftOrRight;

        //Clear any leftover walking flip so only the station flag decides facing
        if (spriteRenderer != null) spriteRenderer.flipX = false;

        transform.localScale = new Vector3(workerLeftOrRight ? -1f : 1f, 1f, 1f);
    }

    public void ResetIdleOrientation()
    {
        transform.localScale = new Vector3(1f, 1f, 1f);
    }
}