using UnityEngine;

public class RotateToCam : MonoBehaviour
{
    public Transform cameraTransform;

    public void Awake()
    {
        cameraTransform = FindFirstObjectByType<Camera>().transform;
    }

    void Update()
    {
        if (cameraTransform != null)
        {
            // Get the camera's X rotation (pitch)
            float cameraXRotation = cameraTransform.eulerAngles.x;

            //Rotate to camera
            transform.rotation = Quaternion.Euler(cameraXRotation, 0f, 0f);
        }
    }
}
