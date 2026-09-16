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

    public void removeCustomer(averageCustomer customer)
    {
        line.Remove(customer);

        updateLinePositions();
    }

    public Vector3 GetPositionForCustomer(averageCustomer customer)
    {
        int index = line.IndexOf(customer);

        Vector3 position = lineStart.position + lineDirection.forward * (index * lineSpacing);

        return position;
    }

    private void updateLinePositions()
    {
         for (int i = 0; i < line.Count; i++)
        {
            Vector3 newPosition = lineStart.position + lineDirection.forward * (i * lineSpacing);

            line[i].moveToLinePosition(newPosition);
        }

    } // should fix customers not filling in line when someone leaves
}
