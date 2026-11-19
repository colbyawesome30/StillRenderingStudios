using UnityEngine;

[RequireComponent(typeof(averageCustomer))]
public class TrashDropper : MonoBehaviour
{
    [SerializeField] private GameObject[] trashPrefabs;
    [Range(0f, 1f)][SerializeField] private float dropChance = 0.5f;
    [SerializeField] private float minDelay = 2f;
    [SerializeField] private float maxDelay = 6f;
    [SerializeField] private int maxDrops = 1;
    [SerializeField] private int maxTrashInStore = 10;
    [SerializeField] private float dropRadius = 0.5f;
    [Tooltip("Optional. If set, the trash is placed on the ground under the customer.")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float yOffset = 0.05f;
    [SerializeField] private bool dropDuringTutorial = false;

    private averageCustomer customer;
    private PlayerInfo playerInfo;
    private float dropTimer;
    private int drops;
    private bool finished;

    private void Awake(){customer = GetComponent<averageCustomer>();}

    private void Start()
    {
        playerInfo = FindAnyObjectByType<PlayerInfo>();

        if (trashPrefabs == null || trashPrefabs.Length == 0)
        {
            Debug.LogWarning("TrashDropper on " + name + " has no trash prefabs assigned.", this);
            finished = true;
            return;
        }

        ScheduleNext();
    }

    //Decide if another piece will be dropped and how long until it happens
    private void ScheduleNext()
    {
        if (drops >= maxDrops) { finished = true; return; }
        if (Random.value > dropChance){finished = true; return;}
        dropTimer = Random.Range(minDelay, Mathf.Max(minDelay, maxDelay));
    }
    //Check if the customer is still in the store and if it's time to drop trash
    private void Update()
    {
        if (finished || !customer.IsInStore) return;   //Timer only runs while inside the store

        dropTimer -= Time.deltaTime;
        if (dropTimer > 0f) return;
        if (!TryDrop()){dropTimer = 1f; return;}

        drops++;
        ScheduleNext();
    }
    //Try to drop trash
    private bool TryDrop()
    {
        if (!dropDuringTutorial && playerInfo != null && !playerInfo.TutorialComplete){return false;}
        if (TrashItem.Count >= maxTrashInStore){return false;}

        GameObject prefab = trashPrefabs[Random.Range(0, trashPrefabs.Length)];
        if (prefab == null) { finished = true; return false; }

        Vector2 offset = Random.insideUnitCircle * dropRadius;
        Vector3 position = transform.position + new Vector3(offset.x, 0f, offset.y);

        if (groundLayer.value != 0 &&
            Physics.Raycast(position + Vector3.up * 3f, Vector3.down, out RaycastHit hit, 10f, groundLayer))
            position = hit.point;

        position.y += yOffset;
        Instantiate(prefab, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
        return true;
    }

}