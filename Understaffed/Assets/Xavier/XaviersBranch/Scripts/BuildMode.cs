using System.Collections.Generic;
using UnityEngine;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;

public class BuildMode : MonoBehaviour
{
    //Build move mode is always active
    public static bool Active { get; private set; }
    public static bool MoveMode => Active;
    public static int PlaceRotationSteps { get; private set; }
    public static event System.Action<int> Rotated;

    //Build UI panels and related fields
    [SerializeField] private List<GameObject> buildPanels;
    [SerializeField] private List<GameObject> hideBuildPanels;
    [SerializeField] private Timer timer;
    private float previousTimeScale = 1f;

    private bool timerWasPaused;


    //Find timer
    private void Awake()
    {
        if (timer == null) timer = FindAnyObjectByType<Timer>();
    }

    //UI buttons
    public void Toggle() => SetActive(!Active);
    public void ToggleMove() { }
    public void RotateLeft() => Rotate(-1);
    public void RotateRight() => Rotate(1);

    //Rotate object
    private void Rotate(int dir)
    {
        if (!Active) return;
        PlaceRotationSteps = ((PlaceRotationSteps + dir) % 4 + 4) % 4;
        Rotated?.Invoke(dir);
    }

    //Set build mode active
    public void SetActive(bool on)
    {
        if (on == Active) return;
        if (on && (Roster.UIDragging || WorldDrag.AnyDragging)) return;

        Active = on;

        if (on)
        {
            WorkStation.CloseSelected();
            PayWall.CloseOpen();
        }
        else
        {
            PlaceRotationSteps = 0;
        }

        //enable build panels
        foreach (var p in buildPanels)
            if (p != null) p.SetActive(on);

        //hide build panels
        foreach (var p in hideBuildPanels)
            if (p != null) p.SetActive(!on);

        //Set timer is active or not
        if (on)
        {
            previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = previousTimeScale;
        }

        if (timer != null)
        {
            if (on) { timerWasPaused = timer.pauseTimer; timer.PauseTimer(true); }
            else timer.PauseTimer(timerWasPaused);
        }
    }

    //Disable build mode
    private void OnDisable()
    {
        if (Active) Time.timeScale = previousTimeScale;
        Active = false;
        PlaceRotationSteps = 0;
    }
}