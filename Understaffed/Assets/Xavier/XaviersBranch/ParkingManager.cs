using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class ParkingManager : MonoBehaviour
{
    //Variables auto set unless shown in inspector
    private List<ParkingLot> parkingLots = new List<ParkingLot>();
    [SerializeField] private List<CarMovement> carPrefabs = new List<CarMovement>();
    [SerializeField] private CustomerSpawner customerSpawner;
    [SerializeField] private Transform routeParent;
    private List<Transform> entryRoute = new List<Transform>();

    private static readonly Regex PointName = new(@"^A(?: \((\d+)\))?$");

    private void Awake()
    {
        parkingLots = FindObjectsByType<ParkingLot>(FindObjectsSortMode.None).ToList();
        BuildRoute();
    }

    //Makes path for cars
    private void BuildRoute()
    {
        IEnumerable<Transform> all = routeParent != null
            ? routeParent.GetComponentsInChildren<Transform>(true)
            : FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        Transform spawn = null, leave = null;
        var middle = new List<(int index, Transform t)>();

        foreach (Transform transform in all)
        {
            if (transform.name == "CarSpawn") spawn = transform;
            else if (transform.name == "CarLeave") leave = transform;
            else
            {
                Match match = PointName.Match(transform.name);
                if (match.Success)
                    middle.Add((match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : 0, transform));
            }
        }

        entryRoute.Clear();
        if (spawn != null) entryRoute.Add(spawn);
        entryRoute.AddRange(middle.OrderBy(x => x.index).Select(x => x.t));
        if (leave != null) entryRoute.Add(leave);
    }

    //No Cars spawn if all lots are full
    public bool TrySpawnCar()
    {
        if (carPrefabs.Count == 0 || entryRoute.Count == 0) return false;

        var free = parkingLots.Where(l => !l.Occupied).ToList();
        if (free.Count == 0) return false;

        ParkingLot lot = free[Random.Range(0, free.Count)];
        lot.Occupied = true;

        CarMovement prefab = carPrefabs[Random.Range(0, carPrefabs.Count)];
        CarMovement car = Instantiate(prefab, entryRoute[0].position, entryRoute[0].rotation);

        car.Parked += customerSpawner.SpawnCustomerAt;
        car.Init(lot, entryRoute);
        return true;
    }
}