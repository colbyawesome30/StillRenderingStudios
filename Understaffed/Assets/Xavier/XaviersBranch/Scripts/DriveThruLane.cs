using System.Collections.Generic;
using UnityEngine;

public class DriveThruLane : MonoBehaviour
{
    public foodStation station;          //Work station
    public Transform spawnPoint;         //Road spawn point

    public List<Transform> entryPoints = new List<Transform>();
    public List<Transform> exitPoints = new List<Transform>();
    public int mergeExitIndex = 1;
    public float carSpacing = 5f;
    public float spawnClearRadius = 4f;
    public float mergeClearRadius = 8f;
    public int MergeCornerIndex => Mathf.Clamp(mergeExitIndex, 0, Mathf.Max(0, exitPoints.Count - 1)) + 1;
    public Vector3 MergePosition => exitPoints[MergeCornerIndex - 1].position;

    public List<Vector3> EntryWaypoints()
    {
        var points = new List<Vector3> { spawnPoint.position };
        foreach (var t in entryPoints) points.Add(t.position);
        points.Add(station.lineStart.position);
        return points;
    }

    //Exit point for cars in drive thru
    public List<Vector3> ExitWaypoints()
    {
        var points = new List<Vector3> { station.lineStart.position };
        foreach (var t in exitPoints) points.Add(t.position);
        return points;
    }

    //How many cars physically fit between the spawn point and the window
    private int MaxQueue()
    {
        var points = EntryWaypoints();
        float length = 0f;
        for (int i = 1; i < points.Count; i++) length += Vector3.Distance(points[i - 1], points[i]);
        return Mathf.Max(1, Mathf.FloorToInt(length / carSpacing));
    }

    //Cab spawn car
    public bool CanSpawn()
    {
        if (station == null || spawnPoint == null || station.lineStart == null || exitPoints.Count == 0) return false;
        if (station.items.Count == 0 || station.IsFull || station.Count >= MaxQueue()) return false;

        Vector3 spawn = spawnPoint.position;
        float r2 = spawnClearRadius * spawnClearRadius;
        foreach (var car in CarMovement.AllCars)
            if (car != null && Flat(car.transform.position - spawn).sqrMagnitude < r2) return false;
        foreach (var dis in DriveThruCar.All)
            if (Flat(dis.transform.position - spawn).sqrMagnitude < r2) return false;
        return true;
    }

    //Nothing moving near the merge point, and no other drive-thru car already merging
    public bool IsMergeClear(DriveThruCar self)
    {
        Vector3 pos = MergePosition;
        float r2 = mergeClearRadius * mergeClearRadius;
        foreach (var car in CarMovement.AllCars)
            if (car != null && car.IsMoving && Flat(car.transform.position - pos).sqrMagnitude < r2) return false;
        foreach (var dis in DriveThruCar.All)
            if (dis != self && dis.IsMerging && Flat(dis.transform.position - pos).sqrMagnitude < r2) return false;
        return true;
    }
    //Actual spawn car
    public void Spawn(GameObject prefab)
    {
        GameObject go = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        DriveThruCar car = go.GetComponent<DriveThruCar>();
        if (car == null) { Destroy(go); return; }
        car.Init(this);
    }

    private static Vector3 Flat(Vector3 vector) { vector.y = 0f; return vector; }
}