using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    public GameObject customerPrefab;
    public Transform spawnPoint;
    public GameObject storeFrontPoint;
    public GameObject entryPoint;
    public foodStation station1;
    public foodStation station2;
    public foodStation station3;
    public GameObject rageLeaving;
    public GameObject exitPoint;
    public GameObject despawnPoint;

    public float spawnInterval = 5f;

    private void Start()
    {
        InvokeRepeating(nameof(SpawnCustomer), 1f, spawnInterval);
    }

    private void SpawnCustomer()
    {
        GameObject customer = Instantiate(customerPrefab, spawnPoint.position, Quaternion.identity);

        averageCustomer customerScript = customer.GetComponent<averageCustomer>();

        // adds movement point refs from spawner to customers since i cant prefab it
        customerScript.storeFrontPoint = storeFrontPoint;
        customerScript.entryPoint = entryPoint;
        customerScript.station1 = station1;
        customerScript.rageLeaving = rageLeaving.gameObject;
        customerScript.exitPoint = exitPoint.gameObject;
        customerScript.despawnPoint = despawnPoint.gameObject;
    }
}
