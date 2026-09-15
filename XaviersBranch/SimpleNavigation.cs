using System.IO;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class SimpleNavigation : MonoBehaviour
{
    //Apply to characters
    private NavMeshAgent agent;
    private Transform spawnPoint;
    private System.Action onArrivedAtSpawn;

    //path state
    public enum NavState { MovingToStation, MovingToSpawn, Idle }
    public NavState State { get; private set; } = NavState.Idle;

    private void Awake() => agent = GetComponent<NavMeshAgent>();

    //Start journey
    public void Initialize(Transform spawn) => spawnPoint = spawn;

    //Go to work station
    public void GoToStation(Transform stationTarget)
    {
        onArrivedAtSpawn = null;
        State = NavState.MovingToStation;
        agent.isStopped = false;
        agent.SetDestination(stationTarget.position);
    }

    //Go back and despawn
    public void ReturnToSpawnAndDespawn(System.Action onArrived)
    {
        onArrivedAtSpawn = onArrived;
        State = NavState.MovingToSpawn;
        agent.isStopped = false;
        agent.SetDestination(spawnPoint.position);
    }

    //Per frame update
    private void Update()
    {
        if (agent.pathPending || State == NavState.Idle) return;

        bool arrived = agent.remainingDistance <= agent.stoppingDistance
                        && (!agent.hasPath || agent.velocity.sqrMagnitude < 0.01f);

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
        }
    }
}