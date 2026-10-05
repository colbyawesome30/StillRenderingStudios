using UnityEngine;
using TMPro;
using UnityEditor;

public class customerOrderDisplay : MonoBehaviour
{
    private averageCustomer customer; 
    [SerializeField] private TextMeshProUGUI orderText;

    void Awake()
    {
        customer = GetComponentInParent<averageCustomer>();
    }

    public void UpdateDisplay()
    {
        if (customer == null)
        {
            customer = GetComponentInParent<averageCustomer>();
        }

        // for if a customer gets all the items in their order and so it doesnt return a null and break stuff
        if (customer.order == null || customer.order.Count == 0)
        {
            gameObject.SetActive(false);
            return;
        } 

        gameObject.SetActive(true);
        orderText.text = customer.order[0].itemName + " x " + customer.order[0].quantity;
    }
}
