using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class CustomerOrderItem
{
    public int stationNumber; // 0 = burger 1 = shake etc or whatever we do
    public string itemName;
    public int quantity;
}

public class averageCustomer : MonoBehaviour
{
    // add references to line array, station spawn, difficulty
    public int difficulty;
    [SerializeField] private Animator animator;
    public float patienceTimer;
    public int rageStep = 0; // for rage movement before leaving
    private float maxPatience; // used for the patience bar fill amount in decreasePatience
    public int lineCapacity; // xavier had idea to change line cap based on difficulty

    public GameObject storeFrontPoint;
    public GameObject entryPoint;
    public GameObject rageLeaving;
    public GameObject exitPoint;
    public GameObject despawnPoint;

    private static System.Random _random = new System.Random();
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip happySound;
    [SerializeField] private AudioClip angrySound;
    [SerializeField] private AudioClip moneySound;

    public float overflowAngerMultiplier = 2f;    // patience drains faster when over line capacity
    public bool refillPatienceAfterStation = false;
    public PlayerInfo playerInfo;
    public float waypointArriveRadius = 2;
    private Vector3 currentDestination;
    private bool hasDestination;
    private foodStation currentStation;

    private bool reachedDoor = false;
    private bool beingServed = false;

    public GameObject patienceBar;
    public Slider patienceSlider;
    public GameObject rageSmoke;

    //-----------------------------------------------------------------------
    // Order / grocery list
    public customerOrderDisplay orderDisplay;
    public int desire; // random int that determines how many items customer wants
    public List<CustomerOrderItem> order = new List<CustomerOrderItem>(); // list of items customer wants
    //-----------------------------------------------------------------------

    private enum CustomerState
    {
        entering,
        toCurrentLineEnd,
        inLine,
        atStation,
        rage,
        leaving   // normal exit
    }

    private CustomerState currentState;
    private NavMeshAgent agent;

    //-----------------------------------------------------------------------
    // Sprite animation / camera facing
    [Tooltip("True if the side animation is drawn facing left (Koi).")]
    [SerializeField] private bool sideSpriteFacesLeft = true;
    [SerializeField] private float moveThreshold = 0.1f;
    [Tooltip("How much stronger one axis must be before switching between side and front.")]
    [SerializeField] private float axisSwitchBias = 1.3f;
    [Tooltip("Time before switching between side and front.")]
    [SerializeField] private float axisHoldTime = 0.25f;
    [Tooltip("Sideways speed before the sprite flips.")]
    [SerializeField] private float flipDeadzone = 0.3f;
    private float nextAxisSwitchTime;
    private static readonly int IsMovingSide = Animator.StringToHash("isMovingSide");
    private static readonly int IsMovingFront = Animator.StringToHash("isMovingFront");
    private bool wasMovingSide;
    [SerializeField] private SpriteRenderer spriteRenderer;
    private Camera cam;
    //-----------------------------------------------------------------------

    private void OnEnable()
    {
        PlayerInfo.DifficultyChanged += OnDifficultyChanged;
    }

    private void OnDestroy()
    {
        PlayerInfo.DifficultyChanged -= OnDifficultyChanged;
    }

    private void LateUpdate() => UpdateAnimation();

    //Update sprite anim
    private void UpdateAnimation()
    {
        if (animator == null || agent == null) return;

        Vector3 vel = agent.velocity;
        vel.y = 0f;

        // Not moving set both bools false
        if (vel.magnitude < moveThreshold)
        {
            animator.SetBool(IsMovingSide, false);
            animator.SetBool(IsMovingFront, false);
            return;
        }

        //Movement relative to the camera, so "side" means left/right on screen
        Vector3 right = cam != null ? cam.transform.right : Vector3.right;
        Vector3 forward = cam != null ? cam.transform.forward : Vector3.forward;
        right.y = 0f; forward.y = 0f;
        right.Normalize(); forward.Normalize();

        float sideAmount = Vector3.Dot(vel, right);
        float frontAmount = Vector3.Dot(vel, forward);

        //Pick moving side
        bool wantSide = wasMovingSide
            ? Mathf.Abs(sideAmount) * axisSwitchBias >= Mathf.Abs(frontAmount)
            : Mathf.Abs(sideAmount) > Mathf.Abs(frontAmount) * axisSwitchBias;

        //Only switch after time
        bool movingSide = wasMovingSide;
        if (wantSide != wasMovingSide && Time.time >= nextAxisSwitchTime)
        {
            movingSide = wantSide;
            nextAxisSwitchTime = Time.time + axisHoldTime;
        }
        wasMovingSide = movingSide;

        animator.SetBool(IsMovingSide, movingSide);
        animator.SetBool(IsMovingFront, !movingSide);

        // Flip only when clearly moving sideways, so small drift doesn't mirror the sprite
        if (movingSide && spriteRenderer != null && Mathf.Abs(sideAmount) > flipDeadzone)
            spriteRenderer.flipX = (sideAmount > 0f) == sideSpriteFacesLeft;
    }

    private void Start()
    {
        if (playerInfo == null) playerInfo = FindFirstObjectByType<PlayerInfo>();
        agent = GetComponent<NavMeshAgent>();

        cam = Camera.main;
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (orderDisplay == null) orderDisplay = GetComponentInChildren<customerOrderDisplay>();

        OnDifficultyChanged(PlayerInfo.CurrentDifficulty);
        rollDesire();
        generateOrder();

        Debug.Log("After generateOrder: desire = " + desire + ", order.Count = " + order.Count);

        if (order.Count == 0)
        {
            // No stations available: the customer will just go to checkout / leave instead of freezing
            Debug.LogWarning(gameObject.name + " has an EMPTY order.");
        }
        else if (orderDisplay != null)
        {
            orderDisplay.UpdateDisplay();
        }

        // deals with stopping in line
        agent.stoppingDistance = Mathf.Max(agent.stoppingDistance, 0.3f);

        changeState(CustomerState.entering);
    }

    private void OnDifficultyChanged(int newDifficulty)
    {
        difficulty = newDifficulty;
        switch (difficulty)
        {
            default:
                overflowAngerMultiplier = 1.1f;
                maxPatience = 40;
                break;
            case 1:
                overflowAngerMultiplier = 1.5f;
                maxPatience = 30;
                break;
            case 2:
                overflowAngerMultiplier = 2f;
                maxPatience = 25;
                break;
            case 3:
                overflowAngerMultiplier = 4f;
                maxPatience = 15;
                break;
            case 4:
                overflowAngerMultiplier = 10f;
                maxPatience = 10;
                break;
        }

        patienceTimer = maxPatience;
    }

    private void Update()
    {
        // update's currentState switch holds checks for each state and what to do when they reach their destination
        switch (currentState)
        {
            case CustomerState.entering:
                agent.autoBraking = false;
                if (!reachedDoor && hasReachedDestination())
                {
                    reachedDoor = true;
                    moveTo(entryPoint.transform);
                }
                else if (reachedDoor && hasReachedDestination())
                {
                    changeState(CustomerState.toCurrentLineEnd);
                    reachedDoor = false;
                }
                break;

            case CustomerState.toCurrentLineEnd:
                agent.autoBraking = true;
                if (hasReachedDestination())
                {
                    changeState(CustomerState.inLine);
                }
                break;

            case CustomerState.inLine:
                agent.autoBraking = true;
                decreasePatience();
                break;

            case CustomerState.atStation:
                agent.autoBraking = true;
                decreasePatience(); // employee interaction capable, patience stops decreasing
                break;

            case CustomerState.leaving:
            case CustomerState.rage:
                agent.autoBraking = false;
                if (hasReachedDestination())
                {
                    if (rageStep == 0)
                    {
                        rageStep = 1;
                        moveTo(exitPoint.transform);
                    }
                    else if (rageStep == 1)
                    {
                        rageStep = 2;
                        moveTo(despawnPoint.transform);
                    }
                    else if (rageStep == 2)
                    {
                        Destroy(gameObject);
                    }
                }
                break;
        }
    }

    private void changeState(CustomerState newState)
    {
        currentState = newState;

        switch (currentState)
        {
            case CustomerState.entering:
                Debug.Log("Entering");
                agent.autoBraking = false;
                reachedDoor = false;
                moveTo(storeFrontPoint.transform);
                break;

            case CustomerState.toCurrentLineEnd:
                Debug.Log("Going to line end");
                agent.autoBraking = true;
                currentStation = PickStation();
                if (currentStation == null)
                {
                    Debug.LogWarning("No checkout found: add a foodStation on an object with no WorkStation.");
                    changeState(CustomerState.leaving);
                    break;
                }
                currentStation.addCustomer(this);
                Vector3 myLinePosition = currentStation.GetPositionForCustomer(this);
                moveToPosition(myLinePosition);
                break;

            case CustomerState.inLine:
                agent.autoBraking = true;
                Debug.Log("In line");
                decreasePatience();
                break;

            case CustomerState.atStation:
                agent.autoBraking = true;
                Debug.Log("At station");
                decreasePatience();
                // employee can interact with customer and sell item (patience stops going down while being helped)
                break;

            case CustomerState.rage:
                agent.autoBraking = false;
                Debug.Log("Rage");
                rageStep = 0;
                rageSmoke.SetActive(true);
                playerInfo.Result(-100);
                int roll = _random.Next(1, 4);
                if (roll == 1)
                {
                    audioSource.PlayOneShot(angrySound);
                }

                patienceBar.SetActive(false);
                beingServed = false;
                if (currentStation != null) currentStation.removeCustomer(this);
                currentStation = null;

                moveTo(rageLeaving.transform);
                break;

            case CustomerState.leaving:
                agent.autoBraking = false;
                Debug.Log("Leaving");
                patienceBar.SetActive(false);
                rageStep = 1;   // skips rageLeaving: goes exitPoint then despawnPoint
                moveTo(exitPoint.transform);
                break;
        }
    } // changeState handles the logic for each state and what to do when first entering that state

    private void decreasePatience()
    {
        if (beingServed)
        {
            return; // patience does not decrease while being served
        }

        patienceBar.SetActive(true);

        float mult = (currentStation != null && currentStation.IsOverflowing(this)) ? overflowAngerMultiplier : 1f;
        patienceTimer -= Time.deltaTime * mult;

        patienceSlider.value = patienceTimer / maxPatience;

        if (patienceTimer <= 0)
        {
            patienceTimer = 0;
            patienceSlider.value = 0;

            changeState(CustomerState.rage);
        }
    }

    // used by foodStation to know when the front customer has actually arrived
    public bool IsAtLineSpot => currentState == CustomerState.inLine && hasReachedDestination();

    // called by foodStation while this customer is being helped (pauses patience)
    public void SetBeingServed(bool served)
    {
        beingServed = served;
    }

    // picks the station for the next item on the list, then the shortest checkout when the list is empty
    private foodStation PickStation()
    {
        Debug.Log("PickStation called, order.Count = " + order.Count);

        // go through the list in order; skip items whose station no longer exists
        foreach (CustomerOrderItem item in order)
        {
            foodStation orderedStation = foodStation.AllStations.FirstOrDefault(
                s => !s.isCheckout && s.stationNumber == item.stationNumber);

            if (orderedStation != null) return orderedStation;
        }

        return foodStation.AllStations
            .Where(s => s.isCheckout)
            .OrderBy(s => s.Count)
            .FirstOrDefault();
    }

    // called by foodStation when a customer finishes being served
    public void OnServiceFinished(foodStation station)
    {
        beingServed = false;

        if (station.isCheckout)
        {
            currentStation = null;
            changeState(CustomerState.leaving);
            float graceSeconds = 3f;
            float patienceRatio = Mathf.Clamp01((patienceTimer + graceSeconds) / maxPatience);
            int scoreToAdd = Mathf.RoundToInt(patienceRatio * playerInfo.maxScorePerCustomer);
            playerInfo.Result(scoreToAdd);
            Debug.Log("Added " + scoreToAdd + "score");
            int roll = _random.Next(1, 9);
            if (roll == 1 || roll == 2)
            {
                audioSource.PlayOneShot(happySound);
            }
            if (roll == 8 || roll == 9)
            {
                audioSource.PlayOneShot(moneySound);
            }
            return;
        }

        // remove the finished item(s) from the list and refresh the display
        order.RemoveAll(orderItem => orderItem.stationNumber == station.stationNumber);
        if (orderDisplay != null) orderDisplay.UpdateDisplay();

        currentStation = null;
        //if (refillPatienceAfterStation) patienceTimer = maxPatience;

        changeState(CustomerState.toCurrentLineEnd);   // picks the next station or checkout
    }

    private bool hasReachedDestination()
    {
        if (agent.pathPending) return false;

        if (currentState == CustomerState.entering && hasDestination ||
            currentState == CustomerState.leaving && hasDestination)
        {
            Vector3 delta = currentDestination - transform.position;
            delta.y = 0f;
            if (delta.magnitude <= waypointArriveRadius) return true;
        }

        if (agent.remainingDistance > agent.stoppingDistance) return false;
        return !agent.hasPath || agent.velocity.sqrMagnitude < 0.01f;
    }

    private void rollDesire()
    {
        // difficulty affects how many items a customer could ask for (difficulty is 0-4)
        switch (difficulty)
        {
            case 0: desire = Random.Range(1, 3); break;
            case 1: desire = Random.Range(1, 5); break;
            case 2: desire = Random.Range(1, 6); break;
            case 3: desire = Random.Range(1, 7); break;
            default: desire = Random.Range(1, 9); break;
        }
    }

    private void generateOrder()
    {
        order.Clear();

        List<foodStation> openStations = foodStation.AllStations.FindAll(
            station => !station.isCheckout && station.gameObject.activeInHierarchy
        );

        if (openStations.Count == 0)
        {
            Debug.LogError("generateOrder: No open food stations available!");
            return;
        }

        for (int i = 0; i < desire; i++)
        {
            foodStation pick = openStations[Random.Range(0, openStations.Count)];

            CustomerOrderItem existingOrderItem =
                order.Find(orderItem => orderItem.stationNumber == pick.stationNumber);

            if (existingOrderItem != null)
            {
                existingOrderItem.quantity++;
            }
            else
            {
                order.Add(new CustomerOrderItem
                {
                    stationNumber = pick.stationNumber,
                    itemName = pick.foodStationType,
                    quantity = 1
                });
            }
        }
    }

    private void moveTo(Transform destination)
    {
        currentDestination = destination.position;
        hasDestination = true;
        agent.SetDestination(destination.position);
    } // for movement to transform points

    private void moveToPosition(Vector3 destination)
    {
        currentDestination = destination;
        hasDestination = true;
        agent.SetDestination(destination);
    } // for movement in line

    public void moveToLinePosition(Vector3 position)
    {
        moveToPosition(position);
    } // to move up in line when someone leaves
}