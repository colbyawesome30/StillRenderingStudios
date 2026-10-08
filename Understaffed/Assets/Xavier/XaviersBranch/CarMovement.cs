using System;
using System.Collections.Generic;
using UnityEngine;

public class CarMovement : MonoBehaviour
{
    public CarMovementVar carMoveVar;
    public event Action<CarMovement> Parked;
    public ParkingLot Lot { get; private set; }
    public Transform CustomerSpawn { get; private set; }
    public bool IsMoving => currentPath != null;
    // Straight lines with rounded corners, sampled into small points
    private class Path
    {
        public List<Vector3> points = new List<Vector3>();
        public List<int> controlPointIndices = new List<int>();   // sample index at the middle of each corner
        public List<int> arcStartIndices = new List<int>();       // sample index where each corner's arc begins
        public float[] cumulativeDistance;                        // distance along the path at each point
        public float Length => cumulativeDistance[cumulativeDistance.Length - 1];
        public float ControlPointDistance(int index) => cumulativeDistance[controlPointIndices[Mathf.Clamp(index, 0, controlPointIndices.Count - 1)]];
        public float ArcStartDistance(int index) => cumulativeDistance[arcStartIndices[Mathf.Clamp(index, 0, arcStartIndices.Count - 1)]];
    }
    private static readonly List<CarMovement> allCars = new List<CarMovement>();
    private static readonly Collider[] overlapBuffer = new Collider[16];
    private IList<Transform> route;
    private int branchSegmentIndex, branchControlIndex;
    private Vector3 branchPosition;
    private Path arrivePath;
    private float groundY;
    // Leaving state
    private bool isBackingOut, waitingToMerge, isRetreating, waitingInSlot;
    private float retreatStartTime, retreatWaitDuration;
    // Car awareness
    private CarMovement blockingCar, yieldTarget;
    private float blockedTime, holdUntilTime, holdMaxTime;
    private bool isYielding, pushingThrough, savedEaseIntoEnd;
    private Path savedPath;
    private float savedTargetDistance, savedMaxSpeed;
    private Action savedOnMoveFinished;
    // Current movement
    private Path currentPath;
    private float distanceAlongPath, targetDistance, maxMoveSpeed, currentSpeed;
    private bool easeIntoEnd;
    private Action onMoveFinished;
    private bool IsActive => currentPath != null || waitingToMerge;
    private bool CanYield => currentPath != null && !isYielding && !isRetreating && targetDistance > distanceAlongPath && distanceAlongPath > 0.5f;
    private CarMovement WaitingOn => blockingCar != null ? blockingCar : (IsHolding ? yieldTarget : null);
    private Vector3 Heading => transform.rotation * Quaternion.Euler(0f, 90, 0f) * Vector3.forward;
    // Direction the car is actually travelling in (opposite to Heading while reversing)
    private Vector3 MoveDirection { get { Vector3 heading = Heading; heading.y = 0f; return targetDistance < distanceAlongPath ? -heading : heading; } }
    // True while sitting out after backing up for another car
    private bool IsHolding
    {
        get
        {
            if (Time.time < holdUntilTime) return true;
            if (yieldTarget == null) return false;
            if (Time.time > holdMaxTime || !yieldTarget.IsActive ||
                (yieldTarget.transform.position - transform.position).sqrMagnitude > carMoveVar.yieldClearDistance * carMoveVar.yieldClearDistance)
            { yieldTarget = null; return false; }
            return true;
        }
    }
    private void OnEnable() => allCars.Add(this);
    private void OnDisable() => allCars.Remove(this);
    private void Awake()
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
            if (child.name.Replace(" ", "").Replace("_", "").ToLower().StartsWith("customerspawn")) { CustomerSpawn = child; break; }
    }
    //Establish parking information and route for car
    public void Init(ParkingLot lot, IList<Transform> route)
    {
        Lot = lot;
        this.route = route;
        FindBranch(lot, route);
        groundY = transform.position.y;
        var waypoints = new List<Vector3>();
        for (int i = 0; i <= branchSegmentIndex; i++) waypoints.Add(route[i].position);
        if ((branchPosition - route[branchSegmentIndex].position).sqrMagnitude > 0.25f) waypoints.Add(branchPosition);
        branchControlIndex = waypoints.Count - 1;
        foreach (var approachPoint in lot.approachPoints) waypoints.Add(approachPoint.position);
        waypoints.Add(lot.parkPoint.position);
        arrivePath = BuildPath(waypoints, branchControlIndex);
        StartMove(arrivePath, 0f, arrivePath.Length, carMoveVar.speed, true, () => Parked?.Invoke(this));
    }

    //Called by CustomerCarLink when the customer has despawned
    public void CustomerReturned() { isBackingOut = true; BeginBackOut(); }
    private void BeginBackOut()
    {
        float backOutEnd = Mathf.Max(arrivePath.ArcStartDistance(branchControlIndex), arrivePath.Length - carMoveVar.backOutDistance);
        StartMove(arrivePath, arrivePath.Length, backOutEnd, carMoveVar.speed / 2, true, () => waitingToMerge = true);
    }
    //Drive back into the slot and wait there
    private void Retreat()
    {
        waitingToMerge = false; isRetreating = true; blockingCar = null; blockedTime = 0f; currentSpeed = 0f;
        StartMove(arrivePath, distanceAlongPath, arrivePath.Length, carMoveVar.backoutSpeed, true, () =>
        {
            isRetreating = false; waitingInSlot = true;
            retreatStartTime = Time.time;
            retreatWaitDuration = carMoveVar.backoutMinWait + UnityEngine.Random.Range(0f, 0.5f);
        });
    }
    //Another car is close
    private bool ApproachingCar()
    {
        foreach (CarMovement otherCar in allCars)
        {
            if (otherCar == null || otherCar == this || otherCar.isBackingOut || !otherCar.IsMoving) continue;
            Vector3 otherPosition = otherCar.transform.position;
            Vector3 offsetToBranch = branchPosition - otherPosition, offsetToSelf = transform.position - otherPosition;
            offsetToBranch.y = 0f; offsetToSelf.y = 0f;
            Vector3 offsetToNearest = offsetToBranch.sqrMagnitude < offsetToSelf.sqrMagnitude ? offsetToBranch : offsetToSelf;
            float distanceToNearest = offsetToNearest.magnitude;
            if (distanceToNearest > carMoveVar.backoutRadius) continue;
            if (distanceToNearest < carMoveVar.mergeClearRadius || Vector3.Dot(otherCar.MoveDirection, offsetToNearest) > 0f) return true;
        }
        return false;
    }
    //Start leaving from parking and go home
    private void StartLeave()
    {
        isBackingOut = false;
        Vector3 currentPosition = transform.position;
        var waypoints = new List<Vector3> { currentPosition };
        int nextRouteIndex = branchSegmentIndex + 1;
        if (nextRouteIndex < route.Count)
        {
            Vector3 offsetToNext = route[nextRouteIndex].position - branchPosition;
            offsetToNext.y = 0;
            float segmentLength = offsetToNext.magnitude;
            if (segmentLength > 0.01f)
            {
                Vector3 mergePoint = branchPosition + offsetToNext / segmentLength * Mathf.Min(carMoveVar.mergeAhead, segmentLength * 0.8f);
                mergePoint.y = currentPosition.y;
                waypoints.Add(mergePoint);
            }
            for (int i = nextRouteIndex; i < route.Count; i++) waypoints.Add(route[i].position);
        }
        Path leavePath = BuildPath(waypoints);
        StartMove(leavePath, 0f, leavePath.Length, carMoveVar.speed, false, () => { Lot.Occupied = false; Destroy(gameObject); });
    }
    //Find point where the car merge back into the main path
    private void FindBranch(ParkingLot lot, IList<Transform> route)
    {
        int joinIndex = lot.joinPoint != null ? route.IndexOf(lot.joinPoint) : -1;
        if (joinIndex >= 0) { branchSegmentIndex = joinIndex; branchPosition = route[joinIndex].position; return; }
        Vector3 approachTarget = lot.approachPoints.Count > 0 && lot.approachPoints[0] != null
            ? lot.approachPoints[0].position : lot.parkPoint.position;
        float bestSqrDistance = float.MaxValue;
        for (int segmentIndex = 0; segmentIndex < route.Count - 1; segmentIndex++)
        {
            Vector3 segmentStart = route[segmentIndex].position;
            Vector3 segmentVector = route[segmentIndex + 1].position - segmentStart;
            float fraction = Mathf.Clamp01(Vector3.Dot(approachTarget - segmentStart, segmentVector) / Mathf.Max(segmentVector.sqrMagnitude, 0.0001f));
            Vector3 closestPoint = segmentStart + segmentVector * fraction;
            float sqrDistance = (closestPoint - approachTarget).sqrMagnitude;
            if (sqrDistance < bestSqrDistance) { bestSqrDistance = sqrDistance; branchSegmentIndex = segmentIndex; branchPosition = closestPoint; }
        }
    }
    private Path BuildPath(IList<Vector3> rawPoints, int wideTurnsFromIndex = int.MaxValue)
    {
        //Build path to parking lot
        var corners = new List<Vector3>();
        foreach (var point in rawPoints)
            if (corners.Count == 0 || (point - corners[corners.Count - 1]).sqrMagnitude > 0.0001f) corners.Add(point);
        if (corners.Count == 1) corners.Add(corners[0] + Vector3.forward * 0.1f);
        var path = new Path();
        path.points.Add(corners[0]); path.controlPointIndices.Add(0); path.arcStartIndices.Add(0);
        for (int cornerIndex = 1; cornerIndex < corners.Count - 1; cornerIndex++)
        {
            Vector3 corner = corners[cornerIndex];
            Vector3 incomingVector = corner - corners[cornerIndex - 1], outgoingVector = corners[cornerIndex + 1] - corner;
            float incomingLength = incomingVector.magnitude, outgoingLength = outgoingVector.magnitude;
            float radiusLimit = cornerIndex >= wideTurnsFromIndex ? carMoveVar.parkCornerRadius : carMoveVar.cornerRadius;
            float curveRadius = Mathf.Min(radiusLimit, incomingLength * 0.5f, outgoingLength * 0.5f);
            Vector3 arcEntry = corner - incomingVector / incomingLength * curveRadius;
            Vector3 arcExit = corner + outgoingVector / outgoingLength * curveRadius;
            //Change parking
            Vector3 entryHandle = arcEntry + (corner - arcEntry) * 0.55f, exitHandle = arcExit + (corner - arcExit) * 0.55f;
            const int arcSteps = 16;
            for (int step = 0; step <= arcSteps; step++)
            {
                float progress = step / (float)arcSteps, remaining = 1f - progress;
                Vector3 arcPoint = remaining * remaining * remaining * arcEntry + 3f * remaining * remaining * progress * entryHandle
                                 + 3f * remaining * progress * progress * exitHandle + progress * progress * progress * arcExit;
                if ((arcPoint - path.points[path.points.Count - 1]).sqrMagnitude > 1e-6f) path.points.Add(arcPoint);
                if (step == 0) path.arcStartIndices.Add(path.points.Count - 1);
                if (step == arcSteps / 2) path.controlPointIndices.Add(path.points.Count - 1);
            }
        }
        path.points.Add(corners[corners.Count - 1]);
        path.controlPointIndices.Add(path.points.Count - 1); path.arcStartIndices.Add(path.points.Count - 1);
        path.cumulativeDistance = new float[path.points.Count];
        for (int i = 1; i < path.points.Count; i++)
            path.cumulativeDistance[i] = path.cumulativeDistance[i - 1] + Vector3.Distance(path.points[i - 1], path.points[i]);
        return path;
    }
    //Check current path
    private static void SamplePath(Path path, float distance, out Vector3 position, out Vector3 tangent)
    {
        distance = Mathf.Clamp(distance, 0f, path.Length);
        int segmentIndex = Array.BinarySearch(path.cumulativeDistance, distance);
        if (segmentIndex < 0) segmentIndex = ~segmentIndex - 1;
        segmentIndex = Mathf.Clamp(segmentIndex, 0, path.points.Count - 2);
        float segmentLength = path.cumulativeDistance[segmentIndex + 1] - path.cumulativeDistance[segmentIndex];
        float fraction = segmentLength > 0.0001f ? (distance - path.cumulativeDistance[segmentIndex]) / segmentLength : 0f;
        position = Vector3.Lerp(path.points[segmentIndex], path.points[segmentIndex + 1], fraction);
        tangent = path.points[segmentIndex + 1] - path.points[segmentIndex];
        tangent.y = 0f;
        if (tangent.sqrMagnitude > 1e-6f) tangent.Normalize();
    }
    //Start car path
    private void StartMove(Path path, float fromDistance, float toDistance, float topSpeed, bool easeIn, Action onFinished)
    {
        currentPath = path; distanceAlongPath = fromDistance; targetDistance = toDistance;
        maxMoveSpeed = topSpeed; easeIntoEnd = easeIn; onMoveFinished = onFinished;
    }
    //Ends the current car movement
    private void FinishMove() { Action callback = onMoveFinished; currentPath = null; currentSpeed = 0f; callback?.Invoke(); }
    //Active car in our way
    private CarMovement FindBlocker(bool isReversing)
    {
        Vector3 checkDirection = isReversing ? -Heading : Heading;
        Vector3 checkOrigin = transform.position + Vector3.up * 0.5f;
        float bumperOffset = isReversing ? 2f : 2f;
        //Check a few cars ahead
        for (int step = 0; step < 3; step++)
        {
            Vector3 checkPoint = checkOrigin + checkDirection * (bumperOffset + carMoveVar.stopDistance * (step / 2f));
            int hitCount = Physics.OverlapSphereNonAlloc(checkPoint, carMoveVar.castRadius, overlapBuffer);
            for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                CarMovement otherCar = overlapBuffer[hitIndex].GetComponentInParent<CarMovement>();
                if (otherCar != null && otherCar != this && otherCar.IsActive) return otherCar;
            }
        }
        return null;
    }
    //Find targetCar to who its waiting on
    private bool IsWaitingOn(CarMovement targetCar)
    {
        CarMovement car = this;
        for (int hops = 0; hops < 8 && car != null; hops++) { car = car.WaitingOn; if (car == targetCar) return true; }
        return false;
    }
    private bool InCycle() => IsWaitingOn(this);
    private void AskToYield(CarMovement otherCar, bool isReversing)
    {
        if (!otherCar.CanYield) return;
        bool bothBlockingEachOther = otherCar.blockingCar == this;
        if (isReversing || (bothBlockingEachOther && GetInstanceID() > otherCar.GetInstanceID())) otherCar.Yield(this);
    }
    //Back up along the path to make room
    public void Yield(CarMovement requester)
    {
        if (!CanYield) return;
        savedPath = currentPath; savedTargetDistance = targetDistance; savedMaxSpeed = maxMoveSpeed;
        savedEaseIntoEnd = easeIntoEnd; savedOnMoveFinished = onMoveFinished;
        isYielding = true; yieldTarget = requester;
        targetDistance = Mathf.Max(0f, distanceAlongPath - carMoveVar.yieldDistance); maxMoveSpeed = carMoveVar.yieldSpeed; easeIntoEnd = false;
        onMoveFinished = () =>
        {
            isYielding = false; currentPath = savedPath; targetDistance = savedTargetDistance;
            maxMoveSpeed = savedMaxSpeed; easeIntoEnd = savedEaseIntoEnd; onMoveFinished = savedOnMoveFinished;
            holdUntilTime = Time.time + 0.3f; holdMaxTime = Time.time + carMoveVar.yieldMaxHold;
        };
    }
    private void Update()
    {
        //Wait until nothing is coming and then try again
        if (waitingInSlot)
        {
            float timeWaited = Time.time - retreatStartTime;
            if (timeWaited > carMoveVar.mergeMaxWait || (timeWaited > retreatWaitDuration && !ApproachingCar())) { waitingInSlot = false; BeginBackOut(); }
            return;
        }
        //Go if clear, otherwise give way
        if (waitingToMerge)
        {
            if (ApproachingCar()) Retreat();
            else { waitingToMerge = false; StartLeave(); }
            return;
        }
        if (IsHolding || currentPath == null) { blockingCar = null; return; }
        bool isReversing = targetDistance < distanceAlongPath;
        //If a car is coming, go back into the slot
        if (isBackingOut && !isRetreating && isReversing && ApproachingCar()) { Retreat(); return; }
        //Backing up to make room
        if (isYielding && (yieldTarget == null || !yieldTarget.IsActive || yieldTarget.blockingCar != this))
        { blockingCar = null; FinishMove(); return; }
        CarMovement carInWay = FindBlocker(isReversing);
        if (pushingThrough && carInWay == null) pushingThrough = false;   // keep driving until clear, so we never stop inside a car
        blockingCar = pushingThrough ? null : carInWay;
        if (blockingCar != null)
        {
            currentSpeed = 0f;
            blockedTime += Time.deltaTime;
            if (blockedTime >= 0.3)
            {
                if (isBackingOut && !isRetreating && isReversing) { Retreat(); return; }
                if (isYielding)
                {
                    //Something is behind us, stop
                    blockedTime = 0f; blockingCar = null; FinishMove(); return;
                }
                AskToYield(blockingCar, isReversing);
            }
            //If keeps getting blocked just ignore cars
            if (blockedTime >= 4 && InCycle()) { pushingThrough = true; blockedTime = 0f; }
            return;
        }
        blockedTime = 0f;
        //Car heading
        SamplePath(currentPath, distanceAlongPath - carMoveVar.headingWindow * 0.5f, out Vector3 positionBehind, out _);
        SamplePath(currentPath, distanceAlongPath + carMoveVar.headingWindow * 0.5f, out Vector3 positionAhead, out _);
        Vector3 pathDirection = positionAhead - positionBehind;
        pathDirection.y = 0f;
        float turnError = 0f;
        if (pathDirection.sqrMagnitude > 0.01f)
        {
            Quaternion goalRotation = Quaternion.LookRotation(pathDirection) * Quaternion.Euler(0f, -90, 0f);
            turnError = Quaternion.Angle(transform.rotation, goalRotation);
            Quaternion easedRotation = Quaternion.Slerp(transform.rotation, goalRotation, 1f - Mathf.Exp(-carMoveVar.rotationSmoothing * Time.deltaTime));
            transform.rotation = Quaternion.RotateTowards(transform.rotation, easedRotation, carMoveVar.turnSpeed * Time.deltaTime);
        }
        //Slow while turning, and ease in at the end of parking moves
        float targetSpeed = maxMoveSpeed * Mathf.Lerp(1f, 0.4f, Mathf.Clamp01(turnError / 90f));
        if (easeIntoEnd) targetSpeed = Mathf.Min(targetSpeed, Mathf.Max(0.6f, Mathf.Abs(targetDistance - distanceAlongPath) * 1.5f));
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, carMoveVar.acceleration * Time.deltaTime);
        distanceAlongPath = Mathf.MoveTowards(distanceAlongPath, targetDistance, currentSpeed * Time.deltaTime);
        SamplePath(currentPath, distanceAlongPath, out Vector3 newPosition, out _);
        newPosition.y = groundY;
        transform.position = newPosition;
        if (Mathf.Approximately(distanceAlongPath, targetDistance)) FinishMove();
    }
}