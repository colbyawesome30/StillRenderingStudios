using UnityEngine;
using UnityEngine.EventSystems;
public interface IInteractable
{
    void Interact(RaycastHit hit);
}

public class InteractScript : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Camera to raycast from. Defaults to Camera.main if left empty.")]
    [SerializeField] private Camera _camera;

    [Tooltip("Which layers the ray is allowed to hit. Defaults to Everything.")]
    [SerializeField] private LayerMask _hitLayers = ~0;
    [SerializeField] private float dragThreshold = 10f;

    [Header("Debug")]
    [SerializeField] private bool _drawDebugRay = false;

    [SerializeField] private GameObject gridField;
    [SerializeField] private LayerMask _groundLayer;

    private bool pressing;
    private Vector2 pressStart;

    private void Awake()
    {
        if (_camera == null) _camera = Camera.main;
    }

    private void Update()
    {
        if (WorkStation.UIOpen || BuildMode.Active || Roster.UIDragging)
        {
            pressing = false;
            return;
        }

        UpdateGroundHover();
        HandleTap();
    }

    //Only interact on release
    private void HandleTap()
    {
        if (Input.touchCount > 1) { pressing = false; return; }

        if (PressedThisFrame(out Vector2 pressPos))
        {
            int fingerId = Input.touchCount > 0 ? Input.GetTouch(0).fingerId : -1;
            pressing = !IsOverUI(fingerId);
            pressStart = pressPos;
            return;
        }

        if (!pressing) return;

        //Moved too far and this is a drag
        Vector2 pos = CurrentPointerPosition();
        if ((pos - pressStart).magnitude >= dragThreshold)
        {
            pressing = false;
            return;
        }

        if (ReleasedThisFrame())
        {
            pressing = false;
            TryInteract(pressStart);
        }
    }

    //Send ray to try and interact
    private void TryInteract(Vector2 screenPosition)
    {
        Ray ray = _camera.ScreenPointToRay(screenPosition);

        if (_drawDebugRay)
        {
            Debug.DrawRay(ray.origin, ray.direction * 1000, Color.red, 1f);
        }

        if (Physics.Raycast(ray, out RaycastHit hit, 1000, _hitLayers))
        {
            if (hit.collider.TryGetComponent<IInteractable>(out var interactable))
            {
                interactable.Interact(hit);
            }
        }
    }

    private void UpdateGroundHover()
    {
        if (gridField == null || _camera == null) return;

        // Pick the pointer: finger if touching, otherwise mouse
        Vector3 pointer;
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) return;
            pointer = touch.position;
        }
        else
        {
            pointer = Input.mousePosition;
        }

        // Skip invalid positions (mouse outside window, no mouse on device)
        if (float.IsInfinity(pointer.x) || float.IsInfinity(pointer.y) ||
            float.IsNaN(pointer.x) || float.IsNaN(pointer.y)) return;

        //Skip positions outside the camera's viewport
        if (!_camera.pixelRect.Contains(pointer)) return;

        Ray ray = _camera.ScreenPointToRay(pointer);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, _groundLayer))
        {
            // World-space position, slightly above the surface along its normal
            gridField.transform.position = hit.point + hit.normal * 0.02f;
        }
    }

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

    private bool ReleasedThisFrame()
    {
        if (Input.touchCount > 0)
            return Input.GetTouch(0).phase == TouchPhase.Ended;
        return Input.GetMouseButtonUp(0);
    }

    private Vector2 CurrentPointerPosition()
    {
        if (Input.touchCount > 0) return Input.GetTouch(0).position;
        return Input.mousePosition;
    }

    private bool IsOverUI(int fingerId)
    {
        if (EventSystem.current == null) return false;
        return fingerId >= 0
            ? EventSystem.current.IsPointerOverGameObject(fingerId)
            : EventSystem.current.IsPointerOverGameObject();
    }
}