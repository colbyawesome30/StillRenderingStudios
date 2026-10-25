using UnityEngine;

public class RotateToCam : MonoBehaviour
{
    public Transform cameraTransform;

    [SerializeField]private bool changeRot;
    public void Awake()
    {
        cameraTransform = FindAnyObjectByType<Camera>().transform;
    }

    void Update()
    {
        if (cameraTransform != null)
        {
            if (changeRot)
            {
                // Get the camera's X rotation (pitch)
                float cameraXRotation = cameraTransform.eulerAngles.x;

                //Rotate to camera
                transform.rotation = Quaternion.Euler(cameraXRotation * -1, 0f, 0f);
            }
            else
            {
                // Get the camera's X rotation (pitch)
                float cameraXRotation = cameraTransform.eulerAngles.x * -1;

                //Rotate to camera
                transform.rotation = Quaternion.Euler(cameraXRotation, 0f, 0f);
            }
        }
    }
}
