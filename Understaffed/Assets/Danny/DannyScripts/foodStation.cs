using UnityEngine;
using System.Collections.Generic;

public class foodStation : MonoBehaviour
{
    public List<averageCustomer> line = new List<averageCustomer>();

    public Transform lineStart;
    public Transform lineDirection; // point the z axis of this in the direction the line goes
    public float lineSpacing = 1.5f; 

    //New Changes
    public static readonly List<foodStation> AllStations = new List<foodStation>();

    //Establish basic items
    public bool isCheckout;
    public WorkStation linkedStation;
    public StationStats stats;
    public float baseServeTime = 0f; 
    private averageCustomer beingServed;
    public int stationNumber;
    //Time left till done
    private float serveTimer;
    
    public int stationNumber;          // must match the numbers customers order by (0 = burger, 1 = shake, etc.)
    public string foodStationType;     // shown as the item name in the customer's order
    //Change end

    [SerializeField]
    public string foodStationType;

    public void addCustomer(averageCustomer customer)
    {
        line.Add(customer);
    }

    public void removeCustomer(averageCustomer customer)
    {
        //New Changes
        if (!line.Remove(customer)) return;

        if (beingServed == customer)
        {
            beingServed = null;
            customer.SetBeingServed(false);
        }
        //Change end

        updateLinePositions();
    }

    public Vector3 GetPositionForCustomer(averageCustomer customer)
    {
        int index = line.IndexOf(customer);

        //New Changes
        Vector3 position = SnapToNavMesh(lineStart.position + lineDirection.forward * (index * lineSpacing));
        //Change end

        return position;
    }

    private void updateLinePositions()
    {
         for (int i = 0; i < line.Count; i++)
        {
            //New Changes
            Vector3 newPosition = SnapToNavMesh(lineStart.position + lineDirection.forward * (i * lineSpacing));
            //Change end

            line[i].moveToLinePosition(newPosition);
            
        }

    } // should fix customers not filling in line when someone leaves

    //New Changes
    private void Awake()
    {
        // register once for the object's whole life, so toggling active state can't drop a lane
        if (!AllStations.Contains(this)) AllStations.Add(this);

        //Looks for stats
        if (stats == null && linkedStation != null) stats = linkedStation.upgradeManager as StationStats;
        if (stats == null) stats = GetComponentInParent<StationStats>();
        if (stats == null) stats = GetComponentInChildren<StationStats>();
        if (stats == null && linkedStation != null) stats = linkedStation.GetComponentInChildren<StationStats>();
    }

    private void OnDestroy() 
    {
        AllStations.Remove(this); 
    }

    //Counts customer line
    public int Capacity
    {
        get
        {
            if (stats == null) return int.MaxValue;
            return Mathf.Max(1, stats.LineCapacity);
        }
    }

    //High workspeed makes them faster
    public float ServeTime
    {
        get
        {
            float s = stats != null ? stats.WorkSpeed : 0f;
            return s > 0f ? baseServeTime / s : baseServeTime;
        }
    }

    public int Count => line.Count;
    public bool IsFull => line.Count >= Capacity;
    public bool IsOverflowing(averageCustomer customer) => line.IndexOf(customer) >= Capacity;

    // start serving whoever is at the front
    private void Update()
    {
        if (stats != null && !stats.HasRealWorker) return;
        if (beingServed == null && line.Count > 0 && line[0].IsAtLineSpot)
        {
            beingServed = line[0];
            beingServed.SetBeingServed(true);
            serveTimer = ServeTime;
        }

        if (beingServed != null)
        {
            serveTimer -= Time.deltaTime;
            if (serveTimer <= 0f)
            {
                averageCustomer done = beingServed;
                removeCustomer(done);
                done.OnServiceFinished(this);
            }
        }
    }

    //nothing nearby reset position
    private Vector3 SnapToNavMesh(Vector3 position)
    {
        if (UnityEngine.AI.NavMesh.SamplePosition(position, out UnityEngine.AI.NavMeshHit hit, 2f, UnityEngine.AI.NavMesh.AllAreas))
            return hit.position;
        return position;
    }
    
    public void stationType()
    {
        // set type of station for customers to reference
        // foodStationType;
    }
}