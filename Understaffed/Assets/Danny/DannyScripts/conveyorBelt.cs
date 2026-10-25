using System.Collections.Generic;
using UnityEngine;

public class conveyorBelt : MonoBehaviour
{
    public float workstationSpeedMultiplier = 1f; // Speed multiplier for the conveyor belt
    public float baseBeltSpeed = 2f; // Base speed of the conveyor belt

    public Transform startPoint;
    public Transform endPoint;

    private List<Transform> itemsOnBelt = new List<Transform>();

    private void Update()
    {
        float currentSpeed = baseBeltSpeed * workstationSpeedMultiplier;
        float moveSpeed = currentSpeed * Time.deltaTime;

        Vector3 moveDirection = (endPoint.position - startPoint.position).normalized;

        // move all items
        for (int i = itemsOnBelt.Count - 1; i >= 0; i--)
        {
            Transform item = itemsOnBelt[i];

            if (item == null)
                { 
                    itemsOnBelt.RemoveAt(i);
                    continue;
                }

            // moves item forward
            item.position += moveDirection * moveSpeed;
            
            // check if item has reached endpoint
            float distanceToEnd = Vector3.Distance(item.position, endPoint.position);
            float totalPathLength = Vector3.Distance(startPoint.position, endPoint.position);
            float distanceFromStart = Vector3.Distance(startPoint.position, item.position);
            
            if (distanceFromStart >= totalPathLength)
            {
                itemsOnBelt.RemoveAt(i);
                Destroy(item.gameObject);
            }
        }
    }

    public void CurrentRegisterItem(GameObject newItem)
    {
        newItem.transform.position = startPoint.position;
        itemsOnBelt.Add(newItem.transform);
    } // changes currently displayed item on conveyor belt
}
