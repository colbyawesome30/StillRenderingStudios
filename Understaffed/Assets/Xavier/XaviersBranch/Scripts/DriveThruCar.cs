using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DriveThruCar : MonoBehaviour, IStationCustomer
{
    public static readonly List<DriveThruCar> All = new List<DriveThruCar>();
    public static int ActiveCount => All.Count;

    //Dannys visuals
    [SerializeField] private TextMeshProUGUI orderText;
    [SerializeField] private GameObject patienceBar;
    [SerializeField] private Slider patienceSlider;
    [SerializeField] private GameObject rageSmoke;
    [SerializeField] private float modelYawOffset = -90f;

    //car move var
    public CarMovementVar carMoveVar;
    [HideInInspector] public PlayerInfo playerInfo;

    public List<CustomerOrderItem> order = new List<CustomerOrderItem>();
    private int desire, totalPrice;
    private float patience, maxPatience, overflowAnger;
    private bool beingServed, joinedLine, enraged;
    private enum State { Queueing, Leaving }
    private State state = State.Queueing;
    private DriveThruLane lane;
    private foodStation station;
    private CarMovement.Path entryPath, exitPath, path;
    private float dist, speed, slotDistance, holdDistance, groundY;
    private bool mergeOpen;
    public bool IsMerging => state == State.Leaving && mergeOpen;
    private bool AtSpot => Mathf.Abs(dist - slotDistance) < 0.15f && speed < 0.1f;

    //Where my car is going forward
    private Vector3 Heading
    {
        get
        {
            Vector3 heading = transform.rotation * Quaternion.Euler(0f, -modelYawOffset, 0f) * Vector3.forward;
            heading.y = 0f;
            return heading.normalized;
        }
    }

    private void OnEnable() => All.Add(this);
    private void OnDisable() => All.Remove(this);
    private void OnDestroy() { if (station != null) station.removeCustomer(this); }

    //Initialize car
    public void Init(DriveThruLane driveThruLane)
    {
        lane = driveThruLane;
        station = lane.station;
        if (carMoveVar == null) carMoveVar = GetComponent<CarMovementVar>();
        if (carMoveVar == null)
        {
            Debug.LogError("DriveThruCar needs a CarMovementVar on the same prefab.", this);
            Destroy(gameObject);
            return;
        }
        if (playerInfo == null) playerInfo = FindAnyObjectByType<PlayerInfo>();
        groundY = transform.position.y;

        entryPath = CarMovement.BuildPath(lane.EntryWaypoints(), carMoveVar.cornerRadius);
        exitPath = CarMovement.BuildPath(lane.ExitWaypoints(), carMoveVar.cornerRadius);
        holdDistance = exitPath.ArcStartDistance(lane.MergeCornerIndex);
        path = entryPath;

        CarMovement.SamplePath(entryPath, 0f, out Vector3 start, out Vector3 tangent);
        start.y = groundY;
        transform.position = start;
        if (tangent.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(tangent) * Quaternion.Euler(0f, modelYawOffset, 0f);

        SetDifficulty(PlayerInfo.CurrentDifficulty);
        GenerateOrder();

        station.addCustomer(this);
        RefreshSlot();
    }

    //Dannys patience stuff

    private void SetDifficulty(int difficulty)
    {
        switch (difficulty)
        {
            default: overflowAnger = 1.1f; maxPatience = 40; break;
            case 1:  overflowAnger = 1.5f; maxPatience = 30; break;
            case 2:  overflowAnger = 2f;   maxPatience = 25; break;
            case 3:  overflowAnger = 4f;   maxPatience = 15; break;
            case 4:  overflowAnger = 10f;  maxPatience = 10; break;
        }
        patience = maxPatience;
    }

    //Dannys make oreder
    private void GenerateOrder()
    {
        int maxItems = PlayerInfo.CurrentDifficulty switch { 0 => 3, 1 => 5, 2 => 6, 3 => 7, _ => 9 };
        desire = Random.Range(1, maxItems);

        for (int i = 0; i < desire; i++)
        {
            StationItem wanted = station.items[Random.Range(0, station.items.Count)];
            CustomerOrderItem existing = order.Find(o => o.itemName == wanted.itemName);
            if (existing != null) { existing.quantity++; existing.totalPriceOfItems += wanted.price; }
            else order.Add(new CustomerOrderItem
            {
                stationNumber = station.stationNumber,
                itemName = wanted.itemName,
                quantity = 1,
                totalPriceOfItems = wanted.price,
                model = wanted.itemModel
            });
            totalPrice += wanted.price;
        }

        if (orderText != null)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var order in order) sb.AppendLine(order.itemName + " x" + order.quantity);
            sb.Append("$" + totalPrice * 10);
            orderText.text = sb.ToString();
        }
    }

    //Dannys drain patience
    private void DrainPatience()
    {
        if (patienceBar != null) patienceBar.SetActive(true);
        float mult = station.IsOverflowing(this) ? overflowAnger : 1f;
        patience -= Time.deltaTime * mult;
        if (patienceSlider != null) patienceSlider.value = Mathf.Max(0f, patience / maxPatience);
        if (patience <= 0f) Enrage();
    }

    //cars cant leave while in line
    private void Enrage()
    {
        enraged = true;
        patience = 0f;
        if (patienceBar != null) patienceBar.SetActive(false);
        if (rageSmoke != null) rageSmoke.SetActive(true);
        playerInfo.Result(0, 0f);
    }

    public bool IsAtLineSpot => !enraged && state == State.Queueing && AtSpot;
    public void SetBeingServed(bool served) => beingServed = served;
    public int QuantityFor(foodStation station) => Mathf.Max(1, desire);
    public void moveToLinePosition(Vector3 ignored) => RefreshSlot();   // slots come from the path, not the station's line positions
    public void OnStationRemoved(foodStation station) => Destroy(gameObject); // simple: the station is gone, so is the car

    public void OnServiceFinished(foodStation station)
    {
        beingServed = false;

        float graceSeconds = 3f;
        float patienceRatio = Mathf.Clamp01((patience + graceSeconds) / maxPatience);
        int payout = totalPrice * 10;
        playerInfo.Result(payout, patienceRatio);
        moneyPopupSpawner.SpawnUIPopup(payout);
        moneyPopupSpawner.SpawnWorldPopup(payout, station.transform.position);

        StartLeaving();
    }

    //Refresh point

    private void RefreshSlot()
    {
        int index = station.line.IndexOf(this);
        if (index < 0) return;
        slotDistance = Mathf.Max(0f, entryPath.Length - index * lane.carSpacing);
    }

    private void StartLeaving()
    {
        state = State.Leaving;
        path = exitPath;
        dist = 0f; speed = 0f; mergeOpen = false;
        if (patienceBar != null) patienceBar.SetActive(false);
        if (orderText != null) orderText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (state == State.Queueing) TickQueueing(); else TickLeaving();
    }

    //Check if car is angry and leave
    private void TickQueueing()
    {
        if (AtSpot) joinedLine = true;
        if (joinedLine && !beingServed && !enraged) DrainPatience();

        if (enraged && AtSpot && station.line.IndexOf(this) == 0)
        {
            station.removeCustomer(this);
            StartLeaving();
            return;
        }
        Drive(slotDistance, true);
    }

    private void TickLeaving()
    {
        if (!mergeOpen && dist >= holdDistance - 1f && lane.IsMergeClear(this)) mergeOpen = true;

        Drive(mergeOpen ? exitPath.Length : holdDistance, !mergeOpen);
        if (mergeOpen && Mathf.Approximately(dist, exitPath.Length)) Destroy(gameObject);
    }

    //Follow path like parking lots
    private void Drive(float target, bool easeIn)
    {
        if (IsBlocked()) { speed = 0f; return; }

        CarMovement.SamplePath(path, dist - carMoveVar.headingWindow * 0.5f, out Vector3 behind, out _);
        CarMovement.SamplePath(path, dist + carMoveVar.headingWindow * 0.5f, out Vector3 ahead, out _);
        Vector3 dir = ahead - behind; dir.y = 0f;
        float turnError = 0f;
        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion goal = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, modelYawOffset, 0f);
            turnError = Quaternion.Angle(transform.rotation, goal);
            Quaternion eased = Quaternion.Slerp(transform.rotation, goal, 1f - Mathf.Exp(-carMoveVar.rotationSmoothing * Time.deltaTime));
            transform.rotation = Quaternion.RotateTowards(transform.rotation, eased, carMoveVar.turnSpeed * Time.deltaTime);
        }

        float top = carMoveVar.speed * Mathf.Lerp(1f, 0.4f, Mathf.Clamp01(turnError / 90f));
        if (easeIn) top = Mathf.Min(top, Mathf.Max(0.6f, Mathf.Abs(target - dist) * 1.5f));
        speed = Mathf.MoveTowards(speed, top, carMoveVar.acceleration * Time.deltaTime);
        dist = Mathf.MoveTowards(dist, target, speed * Time.deltaTime);
        if (Mathf.Abs(target - dist) < 0.001f) speed = 0f;   // arrived: stop, so AtSpot can become true

        CarMovement.SamplePath(path, dist, out Vector3 pos, out _);
        pos.y = groundY;
        transform.position = pos;
    }

    //Check if car is blocked
    private bool IsBlocked()
    {
        foreach (var car in CarMovement.AllCars)
            if (car != null && car.IsMoving && IsAhead(car.transform.position)) return true;
        if (state == State.Leaving)
            foreach (var order in All)
                if (order != this && IsAhead(order.transform.position)) return true;
        return false;
    }

    private bool IsAhead(Vector3 point)
    {
        Vector3 off = point - transform.position; off.y = 0f;
        float distance = off.magnitude;
        return distance > 0.01f && distance < carMoveVar.stopDistance + 2f && Vector3.Dot(off / distance, Heading) > 0.7f;
    }

    //Cars wait for a car that is actively merging
    public static bool MergingAhead(Vector3 position, Vector3 heading, float range)
    {
        foreach (var car in All)
        {
            if (!car.IsMerging) continue;
            Vector3 off = car.transform.position - position; off.y = 0f;
            float dis = off.magnitude;
            if (dis > 0.01f && dis < range && Vector3.Dot(off / dis, heading) > 0.5f) return true;
        }
        return false;
    }
}