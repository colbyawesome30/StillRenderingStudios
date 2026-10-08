using UnityEngine;

public class CarMovementVar : MonoBehaviour
{
    [Header("Driving")]
    public float speed = 6f;
    public float acceleration = 8f;
    public float turnSpeed = 180f;
    [Header("Path")]
    //Corner radius for the main route.
    public float cornerRadius = 4f;
    //Corner radius for the turn into the slot.
    public float parkCornerRadius = 7f;
    //Heading is measured over this much path length.
    public float headingWindow = 2.5f;
    //Lower = lazier rotation
    public float rotationSmoothing = 8f;
    [Header("Leaving")]
    //How far the car backs out of the slot. 
    public float backOutDistance = 4f;
    //How far along the aisle the car merges back onto the route.
    public float mergeAhead = 6f;
    //Any other car this close counts as 'in the way' for a car backing out.
    public float mergeClearRadius = 5f;
    //Cars within this distance heading toward a backing-out car make it retreat.
    public float backoutRadius = 10f;
    //Speed a car drives back into its slot at.
    public float backoutSpeed = 5f;
    //Minimum wait in the slot after retreating (a little random time is added).
    public float backoutMinWait = 1f;
    //Max wait in the slot before backing out anyway.
    public float mergeMaxWait = 10f;
    [Header("Car awareness")]
    //Width of the area checked in front of / behind the car.
    public float castRadius = 1f;
    //Max distance a car backs up to make room.
    public float yieldDistance = 4f;
    //Speed a car backs up at when making room.
    public float yieldSpeed = 5f;
    //Keep waiting until the car it made room for is this far away. 
    public float yieldClearDistance = 6f;
    //Never wait longer than this for that car to clear.
    public float yieldMaxHold = 3f;
    public float stopDistance = 3f;
}