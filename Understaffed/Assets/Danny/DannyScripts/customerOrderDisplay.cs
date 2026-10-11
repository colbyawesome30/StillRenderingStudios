using UnityEngine;
using TMPro;

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
            customer = GetComponentInParent<averageCustomer>();

        if (customer == null || customer.order == null)
        {
            gameObject.SetActive(false);
            return;
        }

        // Order finished: keep showing how much they're spending
        if (customer.order.Count == 0)
        {
            if (customer.TotalPrice <= 0)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            orderText.text = "$" + (customer.TotalPrice * 10);
            return;
        }

        gameObject.SetActive(true);

        int remaining = 0;
        foreach (var item in customer.order)
            remaining += item.totalPriceOfItems;

        int collectedSoFar = (customer.TotalPrice - remaining) * 10;

        var current = customer.order[0];
        orderText.text = current.itemName + " x " + current.quantity + " $" + collectedSoFar;
    }
}