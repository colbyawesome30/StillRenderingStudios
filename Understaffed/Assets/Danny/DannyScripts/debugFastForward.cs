using UnityEngine;
using UnityEngine.EventSystems;

public class debugFastForward : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private float fastForward = 2f;

    private bool holding = false; 
    private bool spedUp = false;

    public void OnPointerDown(PointerEventData eventData)
    {
        holding = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        holding = false;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        holding = false;
    }

    private void Update()
    {

        if (holding == true && spedUp == false && Time.timeScale == 1f)
        {
            Time.timeScale = fastForward;
            spedUp = true;
        }
        else if (holding == false && spedUp == true)
        {
            if (Time.timeScale == fastForward) Time.timeScale = 1f;
            spedUp = false;
        }
    }

    void OnDisable()
    {
        if (spedUp == true && Time.timeScale == fastForward) Time.timeScale = 1f;
        spedUp = false;
        holding = false;
    }
}
