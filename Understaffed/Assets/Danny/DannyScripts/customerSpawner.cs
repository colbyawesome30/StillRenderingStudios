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
    
    public PlayerInfo playerInfo;
    public float spawnInterval = 5f;

    [System.Serializable]
    public struct pathWays
    {
        public GameObject rageLeaving;
        public GameObject exitPoint;
        public GameObject despawnPoint;
    }
    public List <pathWays> customerPaths;


    private void Start()
    {
        InvokeRepeating(nameof(SpawnCustomer), 1f, spawnInterval);
    }

    private void SpawnCustomer()
    {
        //Choose random customer
        int randomIndex = Random.Range(0, customers.Count);
        customerPrefab = customers[randomIndex];

        GameObject customer = Instantiate(customerPrefab, spawnPoint.position, Quaternion.identity);

        averageCustomer customerScript = customer.GetComponent<averageCustomer>();

        // adds movement point refs from spawner to customers since i cant prefab it
        int randomChosenPath = Random.Range(0, customerPaths.Count);
        customerScript.exitPoint = customerPaths[randomChosenPath].exitPoint;
        customerScript.despawnPoint = customerPaths[randomChosenPath].despawnPoint;
        customerScript.rageLeaving = customerPaths[randomChosenPath].rageLeaving;

        customerScript.storeFrontPoint = storeFrontPoint;
        customerScript.entryPoint = entryPoint;

        //-----------------------------------------------------------------------
        //No longer needed
        // customerScript.station1 = station1;
        //-----------------------------------------------------------------------


    }
}