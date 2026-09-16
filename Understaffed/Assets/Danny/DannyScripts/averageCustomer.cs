using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class averageCustomer : MonoBehaviour
{
    // add references to line array, station spawn, difficulty
    public int difficulty;

    public float patienceTimer;
    public int lineCapacity; // xavier had idea to change line cap based on difficulty

    public GameObject storeFrontPoint;
    public GameObject entryPoint;
    public foodStation station1;
    public foodStation station2;
    public foodStation station3;

    private bool reachedDoor = false;
    private bool beingServed = false; 

    private int desire; // random int that determines how many items customer wants
    private int itemsBought = 0; 
   // private List<Item> itemsWanted = new List<Item>(); 

    private enum CustomerState
    { 
        Entering,
        goingToLineEnd,
        inLine,
        atStation,
        Rage
    }

    private CustomerState currentState;
    private NavMeshAgent agent;

    private void Start ()
    {
        agent = GetComponent<NavMeshAgent>();

        setPatience();
        rollDesire();
        //generateItemsWanted();

        changeState(CustomerState.Entering);
    }

    private void Update()
    {
        // update's currentState switch holds checks for each state and what to do when they reach their destination
        switch (currentState)
        {
            case CustomerState.Entering:

                if (!reachedDoor && hasReachedDestination())
                {
                    reachedDoor = true;
                    moveTo(entryPoint.transform);
                }
                else if (reachedDoor && hasReachedDestination())
                {
                    changeState(CustomerState.goingToLineEnd);
                }

                break;

            case CustomerState.goingToLineEnd:
                if (hasReachedDestination())
                {
                    changeState(CustomerState.inLine);
                }
                break;

            case CustomerState.inLine:
                decreasePatience(); 
                break;

            case CustomerState.atStation:
                decreasePatience();
                // employee interaction capable, patience stops decreasing 
                break;

            case CustomerState.Rage:
                // out of patience behavior 
                break;
        }
    }

    private void changeState(CustomerState newState)
    {
        // changeState handles the logic for each state and what to do when first entering that state 
        currentState = newState;

        switch (currentState)
        {
            case CustomerState.Entering:
                Debug.Log("Entering");

                reachedDoor = true;
                moveTo(storeFrontPoint.transform);
                break;

            case CustomerState.goingToLineEnd:
                Debug.Log("Going to line end");

                station1.addCustomer(this);

                Vector3 myLinePosition = station1.GetPositionForCustomer(this);

                moveToPosition(myLinePosition);

                break;

            case CustomerState.inLine:
                Debug.Log("In line");
                break;

            case CustomerState.atStation:
                Debug.Log("At station");
                // employee can interact with customer and sell item (patience stops going down while being helped)
                break;

            case CustomerState.Rage:
                Debug.Log("Mad");
                // add customer rage and have them leave the store
                break;
        }
    }

    private void decreasePatience()
    {
        if (beingServed)
        {
                       return; // patience does not decrease while being served
        }

        patienceTimer -= Time.deltaTime;

        if (patienceTimer <= 0)
        {
            patienceTimer = 0;
            changeState(CustomerState.Rage);
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
    }
    private void moveToPosition(Vector3 destination)
    {
        agent.SetDestination(destination);
    }


}
