using UnityEngine;
using UnityEngine.AI;

//Requires both components to work so put in worker prefab
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(NavMeshAgent))]
public class WorldDrag : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float liftHeight = 1.5f;
    //New
    public static bool AnyDragging { get; private set; }

    private Worker worker;
    private NavMeshAgent agent;
    private SimpleNavigation navigation;
    private bool isDragging;

    //Get the nav mesh and reference to worker
    public void Initialize(Worker workerRef, SimpleNavigation nav)
    {
        worker = workerRef;
        navigation = nav;
        agent = GetComponent<NavMeshAgent>();
    }
    //Get reference to camera
    private void Awake()
    {
        if (_camera == null) _camera = Camera.main;
    }

    //Try to start dragging
    private void Update()
    {
        if (!isDragging)
        {
            TryStartDrag();
        }
        else
        {
            //Keeps dragging
            ContinueDrag();

            //Once the drag has ended
            if (ReleasedThisFrame())
            {
                EndDrag();
            }
        }
    }

    //Checks if clicked to try and drag
    private void TryStartDrag()
    {
        if (!PressedThisFrame(out Vector2 screenPos)) return;

        Ray ray = _camera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f) && hit.collider.GetComponentInParent<WorldDrag>() == this)
        {
            BeginDrag();
        }
    }

    //Start dragging and set the pathing to stop
    private void BeginDrag()
    {
        isDragging = true;
        AnyDragging = true;
        if (navigation != null) navigation.IsSuspended = true;
        if (agent != null) agent.enabled = false;
    }

    //Continue dragging and drag character based on mouse position
    private void ContinueDrag()
    {
        Ray ray = _camera.ScreenPointToRay(CurrentPointerPosition());

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
        {
            transform.position = hit.point + Vector3.up * liftHeight;
        }
    }

    //Dragging has stopped
    private void EndDrag()
    {
        isDragging = false;
        AnyDragging = false;
        Ray ray = _camera.ScreenPointToRay(CurrentPointerPosition());
        Vector3 dropPosition = transform.position;

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
        {
            dropPosition = hit.point;
        }

        //Reenable the worker pathing and if anything needs to happen
        DragWorker.Instance.ResolveDrop(worker, navigation, dropPosition);
    }

    //Checks if interact has been pressed aka left mouse button
    private bool PressedThisFrame(out Vector2 pos)
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            pos = Input.GetTouch(0).position;
            return true;
        }
        if (Input.GetMouseButtonDown(0))
        {
            pos = Input.mousePosition;
            return true;
        }
        pos = default;
        return false;
    }

    //Checks if the poor worker has been released aka the LMB has been let go
    private bool ReleasedThisFrame()
    {
        if (Input.touchCount > 0)
            return Input.GetTouch(0).phase == TouchPhase.Ended || Input.GetTouch(0).phase == TouchPhase.Canceled;
        return Input.GetMouseButtonUp(0);
    }

    //Checks the current mouse cursor position
    private Vector2 CurrentPointerPosition()
    {
        if (Input.touchCount > 0) return Input.GetTouch(0).position;
        return Input.mousePosition;
    }

    private void OnDestroy()
    {
        if (isDragging) AnyDragging = false;
    }
}