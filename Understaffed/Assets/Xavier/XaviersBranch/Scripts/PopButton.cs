using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIPop : MonoBehaviour
{
    [Header("When to pop")]
    [SerializeField] private bool popOnEnable = true;
    [SerializeField] private bool popOnClick = false;

    [Header("Enable pop")]
    [SerializeField] private float enableDuration = 0.25f;
    [SerializeField] private float enableStartScale = 0.8f;
    [SerializeField] private float enableOvershoot = 1.1f;

    [Header("Click pop")]
    [SerializeField] private float clickDuration = 0.15f;
    [SerializeField] private float clickScale = 1.15f;

    private Button button;
    private Vector3 originalScale;
    private bool scaleCaptured;
    private Coroutine routine;

    private void Awake()
    {
        CaptureScale();
        button = GetComponent<Button>();
    }

    private void CaptureScale()
    {
        if (scaleCaptured) return;
        originalScale = transform.localScale;
        scaleCaptured = true;
    }
    
    private void OnEnable()
    {
        CaptureScale();
        if (button == null) button = GetComponent<Button>();

        if (popOnClick)
        {
            if (button != null) button.onClick.AddListener(PopClick);
        }

        if (popOnEnable) PopEnable();
    }

    private void OnDisable()
    {
        if (button != null) button.onClick.RemoveListener(PopClick);
        routine = null;
        transform.localScale = originalScale;
    }

    public void PopEnable() => Play(enableStartScale, enableOvershoot, enableDuration);
    public void PopClick() => Play(1f, clickScale, clickDuration);

    private void Play(float startScale, float peakScale, float duration)
    {
        if (!gameObject.activeInHierarchy) return;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(PopRoutine(startScale, peakScale, duration));
    }

    private IEnumerator PopRoutine(float startScale, float peakScale, float duration)
    {
        Vector3 from = originalScale * startScale;
        Vector3 peak = originalScale * peakScale;
        float grow = duration * 0.6f;
        float settle = duration * 0.4f;

        for (float time = 0f; time < grow; time += Time.unscaledDeltaTime)
        {
            transform.localScale = Vector3.Lerp(from, peak, time / grow);
            yield return null;
        }
        for (float time = 0f; time < settle; time += Time.unscaledDeltaTime)
        {
            transform.localScale = Vector3.Lerp(peak, originalScale, time / settle);
            yield return null;
        }

        transform.localScale = originalScale;
        routine = null;
    }
}