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
    //Time left till done
    private float serveTimer;
    private float serveTotal;   // saved when serving starts so the conveyor belt can work out progress
    public bool lockedInBuildMode = false;
    private WorkStation alertTarget;
    private bool alerting;

    public enum StationType
    {
        Deli,
        Toilet_Paper,
        Seafood,
        Candy,
        Frozen_Food,
        Juice,
        Canned_Items,
        Fruits,
        Vegetables,
        Milk,
        Meat,
        Pasta,
        Snacks,
        Bakery
    }

    public int itemSalePrice = 1; //Price of item sold at station
    public int stationNumber;
    public StationType stationType = StationType.Canned_Items;    // pick this in the Inspector
    public string foodStationType => stationType.ToString();   // "Burger", "Shake", "Fries"

    private static int nextStationNumber = 0;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => nextStationNumber = 0;
    //Change end

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

    }

    //New Changes
    private void Awake()
    {
        if (!AllStations.Contains(this))
        {
            if (!isCheckout) stationNumber = nextStationNumber++;
            AllStations.Add(this);
        }
    
        //Looks for stats
        if (stats == null && linkedStation != null) stats = linkedStation.upgradeManager as StationStats;
        if (stats == null) stats = GetComponentInParent<StationStats>();
        if (stats == null) stats = GetComponentInChildren<StationStats>();
        if (stats == null && linkedStation != null) stats = linkedStation.GetComponentInChildren<StationStats>();

        //Alert: which WorkStation blinks for this food station
        alertTarget = linkedStation != null ? linkedStation : GetComponentInParent<WorkStation>();
    }

    //Blink while customers are waiting and nobody is working here
    private void UpdateAlert()
    {
        bool need = stats != null && !stats.HasRealWorker && line.Count > 0;
        if (need == alerting) return;
        alerting = need;
        if (alertTarget != null) alertTarget.SetNeedsAttention(this, need);
    }

    private void OnDisable()
    {
        if (alerting && alertTarget != null) alertTarget.SetNeedsAttention(this, false);
        alerting = false;
    }

    private void OnDestroy() 
    {
        AllStations.Remove(this); 
        if (alertTarget != null) alertTarget.SetNeedsAttention(this, false);
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
        UpdateAlert();

        if (stats != null && !stats.HasRealWorker) return;
        if (beingServed == null && line.Count > 0 && line[0].IsAtLineSpot)
        {
            beingServed = line[0];
            beingServed.SetBeingServed(true);
            serveTimer = ServeTime * beingServed.QuantityFor(this);
            serveTotal = serveTimer;
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
    //Change end

    // conveyor belt uses these to know how many items to send down
    public bool IsServing => beingServed != null;
    public int ServeItemCount => beingServed == null ? 0 : beingServed.QuantityFor(this);
    public float ServeProgress => (beingServed == null || serveTotal <= 0f) ? 0f : 1f - Mathf.Clamp01(serveTimer / serveTotal);   // 0 at start, 1 when done

    //Change lines
    public void RefreshLine() => updateLinePositions();

    //Destroying the station update everything
    public void Decommission()
    {
        AllStations.Remove(this);
        var waiting = line.ToArray();
        line.Clear();
        beingServed = null;
        UpdateAlert();   //Alert: line is empty now, stop blinking

        // Strip this station's items from every order, so no one pays for food they never got
        if (!isCheckout)
            foreach (var customer in FindObjectsByType<averageCustomer>())
                customer.DropOrderFor(stationNumber);

        foreach (var customer in waiting) customer.OnStationRemoved(this);
    }
}