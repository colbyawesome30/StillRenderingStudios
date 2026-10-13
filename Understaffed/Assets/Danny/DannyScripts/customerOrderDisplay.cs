using UnityEngine;
using TMPro;

public class customerOrderDisplay : MonoBehaviour
{
    private averageCustomer customer; 
    [SerializeField] private TextMeshProUGUI orderText;

	[Header("Text color by station")]
	[SerializeField] private Color shelfColor = Color.black;
	[SerializeField] private Color deliColor = Color.yellow;
	[SerializeField] private Color bakeryColor = Color.red;
	[SerializeField] private Color checkoutColor = new Color(0.5f, 0.9f, 0.5f);


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
        gameObject.SetActive(true);

		// for if a customer gets all the items in their order and so it doesnt return a null and break stuff
		if (customer.order == null || customer.order.Count == 0)
        {
            orderText.text = "Checkout";
			orderText.color = checkoutColor;
			return;
        }

		// displays all items in the order that are for the station they are going to
		int nextStation = customer.order[0].stationNumber;
		var sb = new System.Text.StringBuilder();
		foreach (CustomerOrderItem item in customer.order)
			if (item.stationNumber == nextStation)
				sb.AppendLine(item.itemName + " x " + item.quantity);
		orderText.text = sb.ToString().TrimEnd();

		// colors ordertext based on next station
		foodStation station = foodStation.AllStations.Find(s => !s.isCheckout && s.stationNumber == nextStation);
		orderText.color = colorFor(station);
	}

	private Color colorFor(foodStation station)
	{
		if (station == null) return checkoutColor;
		switch (station.stationType)
		{
			case foodStation.StationType.Deli:
				return deliColor;
			case foodStation.StationType.Bakery:
				return bakeryColor;
			default:
				return shelfColor;
		}
	}
}
