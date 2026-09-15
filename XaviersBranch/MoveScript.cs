using UnityEngine;

public class MoveScript : MonoBehaviour
{
    [Header("Movement")]
    public Transform closedTransform;
    public Transform openTransform;

    public float moveTime = 1f;

    public AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector3 currentStart;
    private Vector3 currentTarget;

    private float moveTimer = 0f;
    private bool isMoving = false;

    [Header("Boundary")]
    public Vector3 boundarySize = new Vector3(1f, 1f, 1f);

    public LayerMask entityLayer;

    public int checkEveryNFrames = 3;

    private int frameCounter = 0;

    // Reusable buffer so we're not creating a new array every check.
    private Collider[] overlapBuffer = new Collider[32];

    // How many entities are currently inside.
    private int currentCount = 0;

    private void Start()
    {
        // Start at the closed world position.
        if (closedTransform != null)
        {
            transform.position = closedTransform.position;
        }
    }

    private void Update()
    {
        // Check the boundary.
        frameCounter++;

        if (frameCounter >= checkEveryNFrames)
        {
            frameCounter = 0;
            CheckBoundary();
        }

        // Handle movement.
        UpdateMovement();
    }

    private void StartMove(Vector3 target)
    {
        // Capture the CURRENT world position.
        // This allows the door to smoothly reverse if necessary.
        currentStart = transform.position;

        currentTarget = target;

        moveTimer = 0f;
        isMoving = true;
    }

    private void UpdateMovement()
    {
        if (!isMoving)
            return;

        moveTimer += Time.deltaTime;

        float t = Mathf.Clamp01(moveTimer / moveTime);

        // Apply the animation curve.
        float curvedT = movementCurve.Evaluate(t);

        // Lerp using WORLD position.
        transform.position = Vector3.Lerp(
            currentStart,
            currentTarget,
            curvedT
        );

        // Movement finished.
        if (t >= 1f)
        {
            transform.position = currentTarget;

            moveTimer = 0f;
            isMoving = false;
        }
    }

    private void CheckBoundary()
    {
        int count = Physics.OverlapBoxNonAlloc(
            transform.position,
            boundarySize / 2f,
            overlapBuffer,
            transform.rotation,
            entityLayer
        );

        // Someone entered while nobody was inside.
        if (currentCount == 0 && count > 0)
        {
            Debug.Log("Opening door");

            if (openTransform != null)
            {
                StartMove(openTransform.position);
            }
        }

        // Everyone has left.
        else if (currentCount > 0 && count == 0)
        {
            Debug.Log("Closing door");

            if (closedTransform != null)
            {
                StartMove(closedTransform.position);
            }
        }

        // Update the current number of entities.
        currentCount = count;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.matrix = Matrix4x4.identity;

        Gizmos.DrawWireCube(
            transform.position,
            boundarySize
        );

        // Show closed position.
        if (closedTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(closedTransform.position, 0.15f);
        }

        // Show open position.
        if (openTransform != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(openTransform.position, 0.15f);
        }

        // Show movement path.
        if (closedTransform != null && openTransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(
                closedTransform.position,
                openTransform.position
            );
        }
    }
}