using System.Collections.Generic;
using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    public GameObject customerPrefab;
    public List<GameObject> customers;
    public Transform spawnPoint;
    public GameObject storeFrontPoint;
    public GameObject entryPoint;

    //-----------------------------------------------------------------------
    //No longer needed: customers find stations through foodStation.AllStations
    // public foodStation station1;
    // public foodStation station2;
    // public foodStation station3;
    //-----------------------------------------------------------------------
    public GameObject rageLeaving;
    public GameObject exitPoint;
    public List<GameObject> exitPoints;
    public GameObject despawnPoint;

    public PlayerInfo playerInfo;
    public List<GameObject> despawnPoints;
    public float spawnInterval = 5f;

    private void Start()
    {
        InvokeRepeating(nameof(SpawnCustomer), 1f, spawnInterval);
    }

    private void SpawnCustomer()
    {
        int randomIndex = Random.Range(0, customers.Count);
        customerPrefab = customers[randomIndex];

        GameObject customer = Instantiate(customerPrefab, spawnPoint.position, Quaternion.identity);

        averageCustomer customerScript = customer.GetComponent<averageCustomer>();

        // adds movement point refs from spawner to customers since i cant prefab it
        customerScript.storeFrontPoint = storeFrontPoint;
        customerScript.entryPoint = entryPoint;
        customerScript.rageLeaving = rageLeaving.gameObject;
        int randomExit = Random.Range(0, exitPoints.Count);
        customerScript.exitPoint = exitPoints[randomExit];
        int randomDespawn = Random.Range(0, despawnPoints.Count);
        customerScript.despawnPoint = despawnPoints[randomDespawn];

        //-----------------------------------------------------------------------
        //No longer needed
        // customerScript.station1 = station1;
        //-----------------------------------------------------------------------


    }
}