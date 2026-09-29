using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Worker : MonoBehaviour
{
    //Change worker settings and how they affect stations   
    //Can leave blank if you dont want this
    public string workerName = "Jerald";
    public bool affectsStation = false;
    public float workSpeed = 1;
    public bool affectsLine;
    public int lineCapacity;
}
