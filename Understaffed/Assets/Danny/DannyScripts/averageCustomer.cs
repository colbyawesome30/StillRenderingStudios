using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;

public class averageCustomer : MonoBehaviour
{
    // add references to line array, station spawn, difficulty
    public int difficulty;

    public float patienceTimer;
    public int rageStep = 0; // for rage movement before leaving
    private float maxPatience; // used for the patience bar fill amount in decreasePatience
    public int lineCapacity; // xavier had idea to change line cap based on difficulty

    public GameObject storeFrontPoint;
    public GameObject entryPoint;
    //-------------------------------------------------------------------------
    //No longer needed: stations are found through foodStation
    public foodStation station1;
    public foodStation station2;
    public foodStation station3;
    //-------------------------------------------------------------------------
    public GameObject rageLeaving;
    public GameObject exitPoint;
    public GameObject despawnPoint;

    //New Changes
    private static System.Random _random = new System.Random();
    [SerializeField]private AudioSource audioSource;
    [SerializeField]private AudioClip happySound;
    [SerializeField]private AudioClip angrySound;
    [SerializeField]private AudioClip moneySound;
    public int minStationsToVisit = 1;
    public int maxStationsToVisit = 3;
    public float overflowAngerMultiplier = 2f;    // patience drains faster when over line capacity
    public bool refillPatienceAfterStation = false;
    public PlayerInfo playerInfo;
    public float waypointArriveRadius = 2;
    private Vector3 currentDestination;
    private bool hasDestination;
    private foodStation currentStation;
    private HashSet<foodStation> visited = new HashSet<foodStation>();
    private int stationsToVisit = -1;   // -1 = not rolled yet (rolled on the first PickStation)
    //Change end

    private bool reachedDoor = false;
    private bool beingServed = false;
    

    public GameObject patienceBar;
    public Slider patienceSlider;
    public GameObject rageSmoke;

    //-----------------------------------------------------------------------
    private int desire; // random int that determines how many items customer wants
    private int itemsBought = 0; 
    //private List<Item> itemsWanted = new List<Item>(); 
    //-----------------------------------------------------------------------

    private enum CustomerState
    { 
        entering,
        toCurrentLineEnd,
        inLine,
        atStation,
        rage,
        //New Change
        leaving   // normal exit
    }

    private CustomerState currentState;
    private NavMeshAgent agent;

    private void OnEnable()
    {
        PlayerInfo.DifficultyChanged += OnDifficultyChanged;
    }

    private void Start ()
    {
        if (playerInfo == null) playerInfo = FindFirstObjectByType<PlayerInfo>();
        agent = GetComponent<NavMeshAgent>();

        OnDifficultyChanged(PlayerInfo.CurrentDifficulty);
        rollDesire();
        //-----------------------------------------------------------------------
        //generateItemsWanted();
        //-----------------------------------------------------------------------

        //New Change - deals with stopping in line
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

            //New Changes
            case CustomerState.leaving:
            //Change end

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
                //New Changes
                currentStation = PickStation();
                if (currentStation == null)
                {
                    agent.autoBraking = true;
                    Debug.LogWarning("No checkout found: add a foodStation on an object with no WorkStation.");
                    changeState(CustomerState.leaving);
                    break;
                }
                currentStation.addCustomer(this);
                Vector3 myLinePosition = currentStation.GetPositionForCustomer(this);
                //Change end

                //-----------------------------------------------------------------------
                //No longer needed
                // station1.addCustomer(this);
                // Vector3 myLinePosition = station1.GetPositionForCustomer(this);
                //-----------------------------------------------------------------------

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

                //New Changes
                patienceBar.SetActive(false);
                beingServed = false;
                if (currentStation != null) currentStation.removeCustomer(this);
                currentStation = null;
                //Change end

                //-----------------------------------------------------------------------
                //No longer needed
                // station1.removeCustomer(this);
                //-----------------------------------------------------------------------

                moveTo(rageLeaving.transform);
                //rage position
                break;

            //New Changes
            case CustomerState.leaving:
                agent.autoBraking = false;
                Debug.Log("Leaving");
                patienceBar.SetActive(false);
                rageStep = 1;   // skips rageLeaving: goes exitPoint then despawnPoint
                moveTo(exitPoint.transform);
                break;
            //Change end
        }
    } // changeState handles the logic for each state and what to do when first entering that state 

    private void OnDestroy()
    {
        PlayerInfo.DifficultyChanged -= OnDifficultyChanged;
    }

    private void decreasePatience()
    {
        if (beingServed)
        {
                       return; // patience does not decrease while being served
        }

        patienceBar.SetActive(true);

        //New Changes
        float mult = (currentStation != null && currentStation.IsOverflowing(this)) ? overflowAngerMultiplier : 1f;
        patienceTimer -= Time.deltaTime * mult;
        //Change end

        //-----------------------------------------------------------------------
        //No longer needed
        // patienceTimer -= Time.deltaTime;
        //-----------------------------------------------------------------------

        patienceSlider.value = patienceTimer / maxPatience;

        if (patienceTimer <= 0)
        {
            patienceTimer = 0;
            patienceSlider.value = 0;

            changeState(CustomerState.rage);
        }
    }

    //New Changes

    // used by foodStation to know when the front customer has actually arrived
    public bool IsAtLineSpot => currentState == CustomerState.inLine && hasReachedDestination();

    // called by foodStation while this customer is being helped (pauses patience)
    public void SetBeingServed(bool served)
    {
        beingServed = served;
    }

    // picks the next lane: random open regular lane, then the shortest checkout when done shopping
    private foodStation PickStation()
    {
        var regular = foodStation.AllStations.Where(s => !s.isCheckout).ToList();
        var checkouts = foodStation.AllStations.Where(s => s.isCheckout).ToList();
        foodStation bestCheckout = checkouts.OrderBy(s => s.Count).FirstOrDefault();

        // roll once, the first time we actually pick, when all stations have registered
        if (stationsToVisit < 0)
        {
            int max = Mathf.Min(Mathf.Max(1, maxStationsToVisit), regular.Count);
            int min = Mathf.Min(Mathf.Max(2, minStationsToVisit), max);   // at least 2 stops
            stationsToVisit = Random.Range(min, max + 1);
        }

        Debug.Log($"PickStation: regular={regular.Count}, checkouts={checkouts.Count}, visited={visited.Count}, target={stationsToVisit}", this);

        // never allow checkout before visiting at least one lane, if any regular lane exists
        bool doneShopping = visited.Count >= stationsToVisit && (visited.Count > 0 || regular.Count == 0);

        if (doneShopping) { Debug.Log("-> checkout: visited enough", this); return bestCheckout; }

        var options = regular.Where(s => !visited.Contains(s)).ToList();
        if (options.Count == 0) { Debug.Log($"-> checkout: no unvisited regular lanes (regular={regular.Count})", this); return bestCheckout; }

        var open = options.Where(s => !s.IsFull).ToList();
        if (open.Count > 0) return open[Random.Range(0, open.Count)];

        return options.OrderBy(s => s.Count).First();   // all full: shortest line, angers faster
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
            if (roll == 9 || roll == 8)
            {
                audioSource.PlayOneShot(moneySound);
            }
            return;
        }

        visited.Add(station);
        currentStation = null;
        //if (refillPatienceAfterStation) patienceTimer = maxPatience;

        changeState(CustomerState.toCurrentLineEnd);   // picks the next station or checkout
    }
    //Change end

    private bool hasReachedDestination()
    {
        //New Changes
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
        //Change end

        //-----------------------------------------------------------------------
        //No longer needed
        // return !agent.pathPending &&
        //        agent.remainingDistance <= agent.stoppingDistance;
        //-----------------------------------------------------------------------
    }

    private void setPatience()
    {
        // different difficulties / days will have different patience timers for customers
        if (difficulty == 1)
        {
            patienceTimer = 30.0f;
        }
        else if (difficulty == 2)
        {
            patienceTimer = 25.0f;
        }
        else if (difficulty == 3)
        {
            patienceTimer = 15.0f;
        }

        maxPatience = patienceTimer; // maxpatience works regardless of difficulty bc of here
    }

    private void rollDesire()
    {
        // difficulty affects how many items a customer could ask for
        if (difficulty == 1)
        {
            desire = Random.Range(1, 3);
        }
        else if (difficulty == 2)
        {
            desire = Random.Range(1, 5);
        }
        else if (difficulty == 3)
        {
            desire = Random.Range(1, 6);
        }
        else if (difficulty == 4)
        {             
            desire = Random.Range(1, 7);
        }
        else if (difficulty == 5)
        {
            desire = Random.Range(1, 9);
        }
    }
    //-----------------------------------------------------------------------
    /*private void generateItemsWanted()
    {
        itemsWanted.Clear();

        for (int i = 0; i < desire; i++)
        {
            int randomIndex = Random.Range(0, GameManager.Instance.items.Length);

            Item randomItem = GameManager.Instance.items[randomIndex];

            itemsWanted.Add(randomItem);
        }
    }*/
    //-----------------------------------------------------------------------

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