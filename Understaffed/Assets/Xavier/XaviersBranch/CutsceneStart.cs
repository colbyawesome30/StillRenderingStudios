using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CutsceneStart : MonoBehaviour
{
    [SerializeField] private Camera introCamera;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private List<GameObject> playerRooms;
    [SerializeField] private List<GameObject> customerPrefabs;
    [SerializeField] private List<GameObject> customers;
    [SerializeField] private float gridSpacing = 5f;
    [SerializeField] private GameObject RoomOriginSpawnPoint;

    [SerializeField] private int rows;
    [SerializeField] private int columns;
    [SerializeField] private int totalCustomers;
    [SerializeField] private List<GameObject> UIGameObjectsToDisable;

    //Made at runtime
    [SerializeField] private List<GameObject> randomListDeleteLater;

    //References
    [SerializeField] private CustomerSpawner customerSpawner;

    [SerializeField] private float cameraDistance = 50f;
    [SerializeField] private float padding = 1f;
    [SerializeField] private float minOrthoSize = 3f;
    //Home pop in settings
    [SerializeField] private float popInDuration = 2f;
    [SerializeField] private float popAnimTime = 0.25f;
    [SerializeField] private float panicPhaseDuration = 5f;
    [SerializeField] private float panicTime = 2f;
    [SerializeField] private float maxStagger = 0.4f;
    [SerializeField] private float endHold = 0.3f;

    //Panic settings
    [SerializeField] private float shakeAmount = 0.12f;
    [SerializeField] private float shakeFrequency = 45f;
    [SerializeField] private float hopHeight = 0.25f;
    [SerializeField] private float hopFrequency = 14f;
    [SerializeField] private float dashAnimSpeed = 1.8f;

    //Walk settings
    [SerializeField] private float floorHeight = 0f;
    [SerializeField] private Vector3 walkStartOffset = new Vector3(-1.5f, 0f, 0f);
    [SerializeField] private Vector3 walkEndOffset = new Vector3(1.5f, 0f, 0f);
    [SerializeField] private bool sideSpriteFacesLeft = true;

    private static readonly int IsMovingSide = Animator.StringToHash("isMovingSide");
    private static readonly int IsMovingFront = Animator.StringToHash("isMovingFront");

    private class CutsceneWalker
    {
        public Transform transform;
        public Transform room;
        public Vector3 roomScale;
        public Vector3 customerScale;
        public Vector3 from, to;
        public Animator animator;
        public bool movingSide;
        public float stagger;
    }
    private readonly List<CutsceneWalker> walkers = new List<CutsceneWalker>();
    private Vector3 shakeAxis = Vector3.right;

    private void Start()
    {
        Initialize();
        SpawnRoomsAndCustomers();
        FrameCamera();
        StartCoroutine(CutsceneRoutine());
    }

    private void Initialize()
    {
        customerSpawner = FindAnyObjectByType<CustomerSpawner>();
        if (mainCamera == null) { mainCamera = FindAnyObjectByType<CameraPan>().GetComponent<Camera>(); }
        if (introCamera == null) { introCamera = this.GetComponent<Camera>(); }

        foreach (var obj in UIGameObjectsToDisable)
        {
            obj.SetActive(false);
        }

        foreach (var waves in customerSpawner.waves)
        {
            foreach (var waveItems in waves.items)
            {
                totalCustomers += waveItems.amountToBeSpawned;
                for (int i = 0; i < waveItems.amountToBeSpawned; i++)
                {
                    customers.Add(waveItems.customer.gameObject.GetComponent<VisualCustomerLink>().cutsceneCustomers);
                }
            }
        }
        Debug.Log("Cutscene: There are " + totalCustomers + " customers coming in today");

        int gridSize = Mathf.CeilToInt(Mathf.Sqrt(totalCustomers));
        rows = gridSize;
        columns = gridSize;

        Debug.Log("Cutscene: rows: " + rows + ", columns: " + columns);
    }

    //Spawn the rooms and customers in a grid, and set up their walk paths
    private void SpawnRoomsAndCustomers()
    {
        shakeAxis = Vector3.ProjectOnPlane(introCamera.transform.right, Vector3.up).normalized;
        Vector3 camRight = shakeAxis;
        Vector3 camForward = Vector3.ProjectOnPlane(introCamera.transform.up, Vector3.up).normalized;

        for (int i = 0; i < customers.Count; i++)
        {
            int row = i / columns;
            int column = i % columns;

            Vector3 offset = new Vector3(column * gridSpacing, 0f, row * gridSpacing);
            Vector3 roomPosition = RoomOriginSpawnPoint.transform.position + offset;

            GameObject room = Instantiate(
                playerRooms[Random.Range(0, playerRooms.Count)],
                roomPosition,
                Quaternion.identity);
            randomListDeleteLater.Add(room);

            // Point A / B use child objects named "CutsceneStart" / "CutsceneEnd" in the room prefab
            Transform anchorA = room.transform.Find("CutsceneStart");
            Transform anchorB = room.transform.Find("CutsceneEnd");

            float floorY = roomPosition.y + floorHeight;
            Vector3 pointA = anchorA != null ? anchorA.position : roomPosition + walkStartOffset;
            Vector3 pointB = anchorB != null ? anchorB.position : roomPosition + walkEndOffset;
            if (anchorA == null) pointA.y = floorY;
            if (anchorB == null) pointB.y = floorY;

            GameObject customer = Instantiate(customers[i], pointA, Quaternion.identity);
            randomListDeleteLater.Add(customer);

            //Lift the customer so the bottom of its sprite sits on the floor
            float lift = 0f;
            SpriteRenderer spriteRenderer = customer.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                lift = pointA.y - spriteRenderer.bounds.min.y;
                customer.transform.position += Vector3.up * lift;
            }

            Vector3 from = pointA + Vector3.up * lift;
            Vector3 to = pointB + Vector3.up * lift;
            Vector3 dir = to - from;
            float sideAmount = Vector3.Dot(dir, camRight);
            float frontAmount = Vector3.Dot(dir, camForward);
            bool movingSide = Mathf.Abs(sideAmount) > Mathf.Abs(frontAmount);

            if (movingSide && spriteRenderer != null && Mathf.Abs(sideAmount) > 0.01f)
                spriteRenderer.flipX = (sideAmount > 0f) == sideSpriteFacesLeft;

            var walker = new CutsceneWalker
            {
                transform = customer.transform,
                room = room.transform,
                roomScale = room.transform.localScale,
                customerScale = customer.transform.localScale,
                from = from,
                to = to,
                animator = customer.GetComponentInChildren<Animator>(),
                movingSide = movingSide,
                stagger = Random.Range(0f, maxStagger)
            };
            walkers.Add(walker);

            room.transform.localScale = Vector3.zero;
            customer.transform.localScale = Vector3.zero;
        }
    }

    //Frame camera for the grid of rooms and customers, with padding
    private void FrameCamera()
    {
        if (customers.Count == 0) return;
        int usedRows = Mathf.CeilToInt(customers.Count / (float)columns);
        int usedColumns = Mathf.Min(columns, customers.Count);

        Vector3 origin = RoomOriginSpawnPoint.transform.position;

        // Center of the grid (cell centers run from origin to origin + (n-1) * spacing)
        Vector3 center = origin + new Vector3(
            (usedColumns - 1) * gridSpacing * 0.5f,
            0f,
            (usedRows - 1) * gridSpacing * 0.5f);

        introCamera.orthographic = true;
        Transform camT = introCamera.transform;

        camT.position = center - camT.forward * cameraDistance;
        float halfX = usedColumns * gridSpacing * 0.5f;
        float halfZ = usedRows * gridSpacing * 0.5f;

        //Project the grid corners onto the camera's right/up axes
        float halfWidth = 0f;
        float halfHeight = 0f;
        for (int scaleX = -1; scaleX <= 1; scaleX += 2)
        {
            for (int scaleZ = -1; scaleZ <= 1; scaleZ += 2)
            {
                Vector3 corner = new Vector3(scaleX * halfX, 0f, scaleZ * halfZ);
                halfWidth = Mathf.Max(halfWidth, Mathf.Abs(Vector3.Dot(corner, camT.right)));
                halfHeight = Mathf.Max(halfHeight, Mathf.Abs(Vector3.Dot(corner, camT.up)));
            }
        }

        float sizeForHeight = halfHeight;
        float sizeForWidth = halfWidth / introCamera.aspect;

        introCamera.orthographicSize = Mathf.Max(minOrthoSize, Mathf.Max(sizeForHeight, sizeForWidth) + padding);
    }

    private IEnumerator CutsceneRoutine()
    {
        float popSpacing = walkers.Count > 1
            ? Mathf.Max(0f, (popInDuration - popAnimTime) / (walkers.Count - 1))
            : 0f;

        float elapsed = 0f;
        while (elapsed < popInDuration)
        {
            for (int i = 0; i < walkers.Count; i++)
            {
                var walker = walkers[i];
                if (walker.transform == null || walker.room == null) continue;

                float time = Mathf.Clamp01((elapsed - i * popSpacing) / popAnimTime);
                float scale = EaseOutBack(time);
                walker.room.localScale = walker.roomScale * scale;
                walker.transform.localScale = walker.customerScale * scale;
            }

            yield return null;
            elapsed += Time.deltaTime;
        }

        foreach (var walker in walkers)
        {
            if (walker.transform == null || walker.room == null) continue;
            walker.room.localScale = walker.roomScale;
            walker.transform.localScale = walker.customerScale;
        }

        float dashTime = Mathf.Max(0.1f, panicPhaseDuration - panicTime - maxStagger - endHold);
        float phaseEnd = panicPhaseDuration - endHold;
        elapsed = 0f;

        while (elapsed < phaseEnd)
        {
            foreach (var walker in walkers)
            {
                if (walker.transform == null) continue;

                float local = elapsed - walker.stagger;

                if (local < panicTime)
                {
                    //Panic shake
                    float shake = Mathf.Sin(Mathf.Max(0f, local) * shakeFrequency) * shakeAmount;
                    float hop = Mathf.Abs(Mathf.Sin(Mathf.Max(0f, local) * hopFrequency)) * hopHeight;
                    walker.transform.position = walker.from + shakeAxis * shake + Vector3.up * hop;
                    SetWalkAnimation(walker, false, 1f);
                }
                else
                {
                    //Customers run out of their homes
                    float time = Mathf.Clamp01((local - panicTime) / dashTime);
                    float eased = time * time;
                    walker.transform.position = Vector3.Lerp(walker.from, walker.to, eased);
                    SetWalkAnimation(walker, time < 1f, dashAnimSpeed);
                }
            }

            yield return null;
            elapsed += Time.deltaTime;
        }

        foreach (var walkers in walkers)
        {
            if (walkers.transform == null) continue;
            walkers.transform.position = walkers.to;
            SetWalkAnimation(walkers, false, 1f);
        }

        yield return new WaitForSeconds(endHold);
        EndCutscene();
    }

    //Room pop feeling
    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float x = t - 1f;
        return 1f + c3 * x * x * x + c1 * x * x;
    }

    private void SetWalkAnimation(CutsceneWalker walker, bool moving, float animSpeed)
    {
        if (walker.animator == null) return;
        walker.animator.speed = animSpeed;
        walker.animator.SetBool(IsMovingSide, moving && walker.movingSide);
        walker.animator.SetBool(IsMovingFront, moving && !walker.movingSide);
    }

    private void EndCutscene()
    {
        introCamera.gameObject.SetActive(false);
        mainCamera.gameObject.SetActive(true);

        foreach (var obj in UIGameObjectsToDisable)
        {
            obj.SetActive(true);
        }

        foreach (var obj in randomListDeleteLater)
        {
            Destroy(obj);
        }
    }
}