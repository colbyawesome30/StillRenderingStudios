using UnityEngine;

public class CustomerCarLink : MonoBehaviour
{
    //Link Between Car and Customer
    public CarMovement car;

    private void OnDestroy()
    {
        // scene.isLoaded stops this firing while a scene is unloading
        if (car != null && gameObject.scene.isLoaded)
            car.CustomerReturned();
    }
}