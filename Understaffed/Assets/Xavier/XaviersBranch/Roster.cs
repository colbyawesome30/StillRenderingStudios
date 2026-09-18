using UnityEngine;
using UnityEngine.EventSystems;

public class Roster : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    //Set camera, layer for floorm and how high they will lift
    [SerializeField] private Camera worldCamera;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float liftHeight = 1.5f;

    //Specific worker
    public Worker worker;
    //Worker being dragged
    private SimpleNavigation draggedInstance;

    private void Awake()
    {
        if (worldCamera == null) worldCamera = Camera.main;
    }

    //Event happens when player clicks and drags
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!RaycastGround(eventData.position, out Vector3 spawnPoint)) return;

        draggedInstance = WorkerSpawnManager.Instance?.BeginManualControl(worker, spawnPoint + Vector3.up * liftHeight);
    }

    //What happens when the player keeps on dragging
    public void OnDrag(PointerEventData eventData)
    {
        if (draggedInstance == null) return;

        if (RaycastGround(eventData.position, out Vector3 point))
        {
            draggedInstance.transform.position = point + Vector3.up * liftHeight;
        }
    }

    //What happens after the player stops dragging
    public void OnEndDrag(PointerEventData eventData)
    {
        if (draggedInstance == null) return;

        Vector3 dropPosition = RaycastGround(eventData.position, out Vector3 point)
            ? point
            : draggedInstance.transform.position;

        DragWorker.Instance.ResolveDrop(worker, draggedInstance, dropPosition);
        draggedInstance = null;
    }

    //Function for checking the drop area via raycast
    private bool RaycastGround(Vector2 screenPosition, out Vector3 hitPoint)
    {
        Ray ray = worldCamera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, groundLayer))
        {
            hitPoint = hit.point;
            return true;
        }

        hitPoint = Vector3.zero;
        return false;
    }
}