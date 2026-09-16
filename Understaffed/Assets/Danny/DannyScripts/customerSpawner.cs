using UnityEngine;

public class CustomerSpawner : MonoBehaviour
{
    public GameObject customerPrefab;
    public Transform spawnPoint;
    public GameObject storeFrontPoint;
    public GameObject entryPoint;
    public foodStation station1;

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
    }
}
