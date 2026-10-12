using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Roster : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    //Set camera, layer for floor and how high they will lift
    [SerializeField] private Camera worldCamera;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float liftHeight = 1.5f;

    //Roster tint
    [SerializeField] private Image rosterImage;
    [SerializeField] private Color notDeployedColor = new Color(0.6f, 1f, 0.6f);   // green
    [SerializeField] private Color draggingColor    = new Color(1f, 0.92f, 0.4f);  // yellow
    [SerializeField] private Color deployedColor    = new Color(1f, 0.5f, 0.5f);   // red

    //Pop effect
    [Header("Pop")]
    [SerializeField] private float pressedScale = 1.15f;    // size while held or dragged
    [SerializeField] private float growTime = 0.08f;
    [SerializeField] private float returnTime = 0.12f;
    [SerializeField] private float returnUndershoot = 0.95f; // dips under normal size, then settles

    private Vector3 originalScale;
    private Coroutine popRoutine;

    private Tutorial tutorial;
    public static bool UIDragging { get; private set; }

    //Which worker is currently being dragged (from the roster OR in the world)
    public static Worker DraggedWorker { get; set; }

    //Specific worker
    public Worker worker;
    //Worker being dragged
    private SimpleNavigation draggedInstance;

    private void Awake()
    {
        if (worldCamera == null) worldCamera = Camera.main;
        if (rosterImage == null) rosterImage = GetComponent<Image>();

        originalScale = transform.localScale;
        tutorial = FindAnyObjectByType<Tutorial>();
    }

    private void Update()
    {
        RefreshTint();
    }

    //Yellow = being dragged, red = deployed, green = not deployed
    private void RefreshTint()
    {
        if (rosterImage == null || worker == null) return;

        Color target;

        if (DraggedWorker == worker)
        {
            target = draggingColor;
        }
        else if (WorkerIndex.Instance != null && WorkerIndex.Instance.GetCurrentStationFor(worker) != null)
        {
            target = deployedColor;
        }
        else
        {
            target = notDeployedColor;
        }

        if (rosterImage.color != target) rosterImage.color = target;
    }

    //Pop up when pressed
    public void OnPointerDown(PointerEventData eventData)
    {
        if (WorkStation.UIOpen) return;
        StartPop(GrowTo(originalScale * pressedScale));
    }

    //Settle back when released (also fires after a drag ends)
    public void OnPointerUp(PointerEventData eventData)
    {
        StartPop(Release());
    }

    //Event happens when player clicks and drags
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (WorkStation.UIOpen || PayWall.TabOpen) return;
        Debug.Log("Roster: begin drag");
        UIDragging = true;
        bool hit = RaycastGround(eventData.position, out Vector3 spawnPoint);
        Debug.Log("Roster: ground hit = " + hit);
        if (!hit) return;

        draggedInstance = WorkerSpawnManager.Instance?.BeginManualControl(worker, spawnPoint + Vector3.up * liftHeight);

        if (draggedInstance != null) DraggedWorker = worker;
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
        UIDragging = false;
        if (DraggedWorker == worker) DraggedWorker = null;
        if (draggedInstance == null) return;

        Vector3 dropPosition = RaycastGround(eventData.position, out Vector3 point)
            ? point
            : draggedInstance.transform.position;

        DragWorker.Instance.ResolveDrop(worker, draggedInstance, dropPosition);
        draggedInstance = null;

        //Tutorial
        if (tutorial != null && !tutorial.tutorialComplete && tutorial.TutorialStep == 1)
        {
            tutorial.tutorialSteps[tutorial.TutorialStep].stepComplete = true;
            tutorial.NextStep();
            tutorial.CheckStepCompletion();
        }
        //Tutorial
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

    //Pop animation helpers
    private void StartPop(IEnumerator anim)
    {
        if (!gameObject.activeInHierarchy) return;
        if (popRoutine != null) StopCoroutine(popRoutine);
        popRoutine = StartCoroutine(anim);
    }

    private IEnumerator GrowTo(Vector3 target)
    {
        Vector3 from = transform.localScale;
        for (float time = 0f; time < growTime; time += Time.unscaledDeltaTime)
        {
            transform.localScale = Vector3.Lerp(from, target, time / growTime);
            yield return null;
        }
        transform.localScale = target;
        popRoutine = null;
    }

    private IEnumerator Release()
    {
        Vector3 from = transform.localScale;
        Vector3 under = originalScale * returnUndershoot;
        float half = returnTime / 2f;

        for (float time = 0f; time < half; time += Time.unscaledDeltaTime)
        {
            transform.localScale = Vector3.Lerp(from, under, time / half);
            yield return null;
        }
        for (float time = 0f; time < half; time += Time.unscaledDeltaTime)
        {
            transform.localScale = Vector3.Lerp(under, originalScale, time / half);
            yield return null;
        }

        transform.localScale = originalScale;
        popRoutine = null;
    }

    //Upon disable cant drag
    private void OnDisable()
    {
        UIDragging = false;
        if (DraggedWorker == worker) DraggedWorker = null;

        popRoutine = null;
        transform.localScale = originalScale;   // never leave it stuck big
    }
}