using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using System.Collections.Generic;

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
    public foodStation station1;
    public foodStation station2;
    public foodStation station3;
    public GameObject rageLeaving;
    public GameObject exitPoint;
    public GameObject despawnPoint;

    private bool reachedDoor = false;
    private bool beingServed = false;

    public GameObject patienceBar;
    public Slider patienceSlider;
    public GameObject rageSmoke;

    private int desire; // random int that determines how many items customer wants
    private int itemsBought = 0; 
   // private List<Item> itemsWanted = new List<Item>(); 

    private enum CustomerState
    { 
        entering,
        toCurrentLineEnd,
        inLine,
        atStation,
        rage
    }

    private CustomerState currentState;
    private NavMeshAgent agent;

    private void Start ()
    {
        agent = GetComponent<NavMeshAgent>();

        setPatience();
        rollDesire();
        //generateItemsWanted();

        changeState(CustomerState.entering);
    }

    private void Update()
    {
        // update's currentState switch holds checks for each state and what to do when they reach their destination
        switch (currentState)
        {
            case CustomerState.entering:

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
                if (hasReachedDestination())
                {
                    changeState(CustomerState.inLine);
                }
                break;

            case CustomerState.inLine:
                decreasePatience(); 
                break;

            case CustomerState.atStation:
                decreasePatience(); // employee interaction capable, patience stops decreasing 
                break;

            case CustomerState.rage:
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

                reachedDoor = false;
                moveTo(storeFrontPoint.transform);
                break;

            case CustomerState.toCurrentLineEnd:
                Debug.Log("Going to line end");

                station1.addCustomer(this);

                Vector3 myLinePosition = station1.GetPositionForCustomer(this);

                moveToPosition(myLinePosition);

                break;

            case CustomerState.inLine:
                Debug.Log("In line");
                decreasePatience();
                break;

            case CustomerState.atStation:
                Debug.Log("At station");
                decreasePatience();
                // employee can interact with customer and sell item (patience stops going down while being helped)
                break;

            case CustomerState.rage:
                Debug.Log("Rage");
                rageStep = 0; 
                rageSmoke.SetActive(true);

                station1.removeCustomer(this);

                moveTo(rageLeaving.transform);
                // add customer rage
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

        patienceTimer -= Time.deltaTime;

        patienceSlider.value = patienceTimer / maxPatience;

        if (patienceTimer <= 0)
        {
            patienceTimer = 0;
            patienceSlider.value = 0;

            changeState(CustomerState.rage);
        }
    }

    private bool hasReachedDestination()
    {
        return !agent.pathPending &&
               agent.remainingDistance <= agent.stoppingDistance;
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

/*    private void generateItemsWanted()
    {
        itemsWanted.Clear();

        for (int i = 0; i < desire; i++)
        {
            int randomIndex = Random.Range(0, GameManager.Instance.items.Length);

            Item randomItem = GameManager.Instance.items[randomIndex];

            itemsWanted.Add(randomItem);
        }
    }*/

    private void moveTo(Transform destination)
    {
        agent.SetDestination(destination.position);
    } // for movement to transform points
    private void moveToPosition(Vector3 destination)
    {
        agent.SetDestination(destination);
    } // for movement in line

    public void moveToLinePosition(Vector3 position)
    {
        moveToPosition(position);
    } // to move up in line when someone leaves


}
