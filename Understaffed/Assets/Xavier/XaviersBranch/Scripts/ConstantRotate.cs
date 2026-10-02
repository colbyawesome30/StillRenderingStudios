using UnityEngine;

public class ConstantRotate : MonoBehaviour
{
    public Vector3 rotationAxis = Vector3.up;

    void Update()
    {
        transform.Rotate(rotationAxis * 50 * Time.deltaTime);
    }
}