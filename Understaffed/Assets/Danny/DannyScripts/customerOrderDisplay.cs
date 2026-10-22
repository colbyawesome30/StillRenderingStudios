using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class customerOrderDisplay : MonoBehaviour
{
    [System.Serializable]
    public struct StationColor
    {
        public foodStation.StationType stationType;
        public Color color;
    }

    private averageCustomer customer;
    [SerializeField] private TextMeshProUGUI orderText;

    [Header("Text color by station")]
    [SerializeField] private List<StationColor> stationColors = new List<StationColor>();
    [SerializeField] private Color defaultColor = Color.black;   // used when a type isn't in the list
    [SerializeField] private Color checkoutColor = new Color(0.5f, 0.9f, 0.5f);

    void Awake()
    {
        customer = GetComponentInParent<averageCustomer>();
    }

    public void UpdateDisplay()
    {
        if (customer == null) customer = GetComponentInParent<averageCustomer>();
        gameObject.SetActive(true);

        // only checkout is left: show everything they are paying for
        if (customer.order == null || customer.order.Count == 0)
        {
            orderText.text = "Checkout $" + (customer.TotalPrice * 10);
            orderText.color = checkoutColor;
            return;
        }

        // items for the station they are heading to, each with its price
        int nextStation = customer.order[0].stationNumber;
        var sb = new System.Text.StringBuilder();
        foreach (CustomerOrderItem item in customer.order)
            if (item.stationNumber == nextStation)
                sb.AppendLine(item.itemName + " x" + item.quantity + " $" + (item.totalPriceOfItems * 10));
        orderText.text = sb.ToString().TrimEnd();

        foodStation station = foodStation.AllStations.Find(s => !s.isCheckout && s.stationNumber == nextStation);
        orderText.color = ColorFor(station);
    }

    private Color ColorFor(foodStation station)
    {
        if (station == null) return checkoutColor;

        foreach (var entry in stationColors)
            if (entry.stationType == station.stationType) return entry.color;

        return defaultColor;
    }
}