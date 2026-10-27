using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;


// shows customers order items moving down the belt nooe at a time
// each item should reach the end right when it would be scanned by WorkStation so the conveyor matches serve speed
// should work with upgrades as well
public class conveyorBelt : MonoBehaviour
{
    public foodStation linkedStation;

    public Transform startPoint;
    public Transform endPoint;

    public GameObject fallbackPrefab; // used if the item doesn't have a model prefab yet

    public float beltItemScale = 0.5f; // scale of items on the belt
    public float beltItemSpacing = 0.3f; // how far apart items are on the belt
    public float beltItemRotation = 90; // rotate items on conveyor to face the employee

    private class BeltItem
    {
        public GameObject itemObject;
        public int beltIndex;
    }

    private List<BeltItem> itemsOnBelt = new List<BeltItem>();
    private averageCustomer currentCustomer; // current customer being served at the linked station

    private void Start()
    {
        if (linkedStation == null)
        {
            GetComponentInParent<foodStation>();
        }
    }

    private void Update()
    {
        if (linkedStation == null) return;

        averageCustomer customer = linkedStation.CurrentCustomer;

        if (customer != currentCustomer)
        {
            ClearBelt();
            currentCustomer = customer;
            if (customer != null) SpawnItems(customer);
        }

        if (currentCustomer != null) return;

        Vector3 moveDirection = (endPoint.position - startPoint.position).normalized;
        float totalPathLength = Vector3.Distance(startPoint.position, endPoint.position);

        float itemsScanned = linkedStation.ServeProgress * linkedStation.ServeItemCount;

        // move items on belt
        for (int i = itemsOnBelt.Count - 1; i >= 0; i--)
        {
            BeltItem item = itemsOnBelt[i];

            if (item.itemObject == null)
            {
                itemsOnBelt.RemoveAt(i);
                continue;
            }

            float distanceToEnd = (item.beltIndex + 1 - itemsScanned) * beltItemSpacing;
            if (distanceToEnd < 0f)
            {
                Destroy(item.itemObject);
                itemsOnBelt.RemoveAt(i);
                continue;
            }

            item.itemObject.transform.position = endPoint.position - moveDirection * distanceToEnd;

            // items further back on belt are hidden until they reach the start
            if (distanceToEnd <= totalPathLength)
            {
                item.itemObject.SetActive(false);
            }
        }
    }

    private void SpawnItems(averageCustomer customer)
    {
        // currently spawns in all items at once, not one by one. also dont move along the line
        Debug.Log("SpawnItems Called");
        List<CustomerOrderItem> individualItems = new List<CustomerOrderItem>();

        foreach (CustomerOrderItem line in customer.ItemsFor(linkedStation))
        {
            for (int q = 0; q < line.quantity; q++)
            {
                individualItems.Add(line);
            }
        }

        int count = linkedStation.ServeItemCount;
        for (int i = 0; i < count; i++)
        {
            GameObject model = i < individualItems.Count ? individualItems[i].model : null;
            GameObject prefab = model != null ? model : fallbackPrefab;
            if (prefab == null) continue;

            GameObject obj = Instantiate(prefab, startPoint.position, Quaternion.identity);
            obj.transform.localScale = prefab.transform.localScale * beltItemScale;

            // disable item collision on belt
            // obj.SetActive(false);
            itemsOnBelt.Add(new BeltItem { itemObject = obj, beltIndex = i });
        }
    }

    private void ClearBelt()
    {
        foreach (BeltItem item in itemsOnBelt)
        {
            if (item.itemObject != null)
                Destroy(item.itemObject);
        }
        itemsOnBelt.Clear();
    }
    private void OnDisable()
    {
        ClearBelt();
        currentCustomer = null;
    }
}
