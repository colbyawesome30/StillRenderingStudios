using System.Collections.Generic;
using UnityEngine;

public class ParkingLot : MonoBehaviour
{
    //Points for when car starts parking
    public List<Transform> approachPoints = new List<Transform>();

    [HideInInspector]public Transform joinPoint;

    //Where the car stops
    public Transform parkPoint;
    //Customer spawn point
    public Transform CustomerSpawn { get; private set; }
    //Is spot occupied
    public bool Occupied { get; set; }

    //Find customer spawn from car
    private void Awake()
    {
        foreach (Transform transform in GetComponentsInChildren<Transform>(true))
            if (transform.name.StartsWith("CustomerSpawn")) { CustomerSpawn = transform; break; }
    }
}