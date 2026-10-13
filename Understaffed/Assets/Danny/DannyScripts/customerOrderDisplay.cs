using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class customerOrderDisplay : MonoBehaviour
{
    [System.Serializable]
    public class StationColor
    {
        public foodStation.StationType stationType;
        public Color color = Color.white;
    }

    private averageCustomer customer;
    [SerializeField] private TextMeshProUGUI orderText;

    [Header("Text color by next station")]
    [SerializeField] private List<StationColor> stationColors = new List<StationColor>();
    [SerializeField] private Color defaultColor = Color.black;   // used when a type has no entry
    [SerializeField] private Color checkoutColor = new Color(0.5f, 0.9f, 0.5f);

    private Dictionary<foodStation.StationType, Color> colorLookup;

    void Awake()
    {
        customer = GetComponentInParent<averageCustomer>();
        BuildLookup();
    }

    private void BuildLookup()
    {
        colorLookup = new Dictionary<foodStation.StationType, Color>();
        foreach (StationColor entry in stationColors)
            colorLookup[entry.stationType] = entry.color;
    }

    public void UpdateDisplay()
    {
        if (colorLookup == null) BuildLookup();

        if (customer == null)
            customer = GetComponentInParent<averageCustomer>();

        if (customer == null || customer.order == null)
        {
            gameObject.SetActive(false);
            return;
        }

        //Order finished: show the total in the checkout color
        if (customer.order.Count == 0)
        {
            if (customer.TotalPrice <= 0)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            orderText.text = "$" + (customer.TotalPrice * 10);
            orderText.color = checkoutColor;
            return;
        }

        gameObject.SetActive(true);

        // List every item for the station the customer is heading to next, on one line
        int nextStation = customer.order[0].stationNumber;
        var items = new List<string>();
        foreach (CustomerOrderItem item in customer.order)
        {
            if (item.stationNumber == nextStation)
                items.Add(item.itemName + " x " + item.quantity);
        }

        // Money collected so far, on the same line
        int remaining = 0;
        foreach (var item in customer.order)
            remaining += item.totalPriceOfItems;
        int collectedSoFar = (customer.TotalPrice - remaining) * 10;

        orderText.text = string.Join(", ", items) + " $" + collectedSoFar;

        // Color by the type of the next station
        foodStation station = foodStation.AllStations.Find(
            stationCheck => !stationCheck.isCheckout && stationCheck.stationNumber == nextStation);
        orderText.color = ColorFor(station);
    }

    private Color ColorFor(foodStation station)
    {
        if (station == null) return checkoutColor;

        return colorLookup.TryGetValue(station.stationType, out Color color) ? color : defaultColor;
    }
}