using UnityEngine;

public class lookAtCamera : MonoBehaviour
{
    private void LateUpdate()
    {
        transform.LookAt(Camera.main.transform);
        transform.Rotate(0, 180, 0); // flipped for some reason lol
    } // for ui and stuff on customers that move to look at the cam
}
