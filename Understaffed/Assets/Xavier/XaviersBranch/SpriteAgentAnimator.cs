using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class SpriteAgentAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("True if the side animation is drawn facing left.")]
    [SerializeField] private bool sideSpriteFacesLeft = true;
    [SerializeField] private float moveThreshold = 0.1f;
    [Tooltip("How much stronger one axis must be before switching between side and front.")]
    [SerializeField] private float axisSwitchBias = 1.3f;
    [Tooltip("Time before switching between side and front.")]
    [SerializeField] private float axisHoldTime = 0.25f;
    [Tooltip("Sideways speed before the sprite flips.")]
    [SerializeField] private float flipDeadzone = 0.3f;
    [Tooltip("Higher = snappier, lower = smoother velocity for animation decisions.")]
    [SerializeField] private float velocitySmoothing = 10f;

    private static readonly int IsMovingSide = Animator.StringToHash("isMovingSide");
    private static readonly int IsMovingFront = Animator.StringToHash("isMovingFront");

    private NavMeshAgent agent;
    private Camera cam;
    private Vector3 smoothedVel;
    private bool wasMovingSide;
    private float nextAxisSwitchTime;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false; // sprites are billboarded, don't let the agent spin the transform

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        if (animator == null) return;
        if (cam == null) cam = Camera.main;

        // Smooth velocity so avoidance jitter / corner spikes don't flip animations
        Vector3 vel = agent.velocity;
        vel.y = 0f;
        float t = 1f - Mathf.Exp(-velocitySmoothing * Time.deltaTime);
        smoothedVel = Vector3.Lerp(smoothedVel, vel, t);

        if (smoothedVel.magnitude < moveThreshold)
        {
            animator.SetBool(IsMovingSide, false);
            animator.SetBool(IsMovingFront, false);
            return;
        }

        Vector3 right = cam != null ? cam.transform.right : Vector3.right;
        Vector3 forward = cam != null ? cam.transform.forward : Vector3.forward;
        right.y = 0f; forward.y = 0f;
        right.Normalize(); forward.Normalize();

        float sideAmount = Vector3.Dot(smoothedVel, right);
        float frontAmount = Vector3.Dot(smoothedVel, forward);

        bool wantSide = wasMovingSide
            ? Mathf.Abs(sideAmount) * axisSwitchBias >= Mathf.Abs(frontAmount)
            : Mathf.Abs(sideAmount) > Mathf.Abs(frontAmount) * axisSwitchBias;

        bool movingSide = wasMovingSide;
        if (wantSide != wasMovingSide && Time.time >= nextAxisSwitchTime)
        {
            movingSide = wantSide;
            nextAxisSwitchTime = Time.time + axisHoldTime;
        }
        wasMovingSide = movingSide;

        animator.SetBool(IsMovingSide, movingSide);
        animator.SetBool(IsMovingFront, !movingSide);

        if (movingSide && spriteRenderer != null && Mathf.Abs(sideAmount) > flipDeadzone)
            spriteRenderer.flipX = (sideAmount > 0f) == sideSpriteFacesLeft;
    }
}