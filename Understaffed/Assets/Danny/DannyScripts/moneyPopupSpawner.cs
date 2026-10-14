using UnityEngine;

public class moneyPopupSpawner : MonoBehaviour
{
    private static moneyPopupSpawner instance;

    [SerializeField] private moneyPopup uiPopupPrefab;
    [SerializeField] private moneyPopup worldPopupPrefab;

    [SerializeField] private Transform uiAnchor; // where in UI the popup will appear
    private float uiSpread = 10f; // how far from the anchor the popup can appear in case there are multiple popups at once

    [SerializeField] private float worldPopupHeight = 2.5f; // how high above the world position/register the popup will appear

    private void Awake()
    {
        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public static void SpawnUIPopup(int amount)
    {
        if (!Ready() || amount ==0) return;
        if (instance.uiPopupPrefab == null || instance.uiAnchor == null) return;

        moneyPopup popup = Instantiate(instance.uiPopupPrefab, instance.uiAnchor);

        popup.transform.localPosition = new Vector3(Random.Range(-instance.uiSpread, instance.uiSpread), 0f, 0f);
        popup.Setup(amount);
    }

    public static void SpawnWorldPopup(int amount, Vector3 worldPosition)
    {
        Debug.Log($"World popup: ready={Ready()}, prefab={(instance != null ? instance.worldPopupPrefab : null)}, pos={worldPosition}");
        if (!Ready() || amount == 0) return;

        if (instance.worldPopupPrefab != null)
        {
            moneyPopup popup = Instantiate(instance.worldPopupPrefab, worldPosition + Vector3.up * instance.worldPopupHeight, Quaternion.identity);
            popup.Setup(amount);
        }
    }

    private static bool Ready()
    {
        if (instance != null) return true;
        return false;
    }
}
