using UnityEngine;
using System.Collections.Generic;

public class foodStation : MonoBehaviour
{
    public List<averageCustomer> line = new List<averageCustomer>();

    public Transform lineStart;
    public Transform lineDirection; // point the z axis of this in the direction the line goes
    public float lineSpacing = 1.5f; 
    public void addCustomer(averageCustomer customer)
    {
        line.Add(customer);
    }

    public Vector3 GetPositionForCustomer(averageCustomer customer)
    {
        int index = line.IndexOf(customer);

        Vector3 position = lineStart.position + lineDirection.forward * (index * lineSpacing);

        Debug.Log("LineStart: " + lineStart.position);
        Debug.Log("Customer Position: " + position);

        return position;
    }
}
