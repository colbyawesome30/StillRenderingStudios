using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonPop : MonoBehaviour
{
    [SerializeField] private float popScale = 1.15f;   // how big it gets
    [SerializeField] private float duration = 0.15f;   // total time of the pop

    private Button button;
    private Vector3 originalScale;
    private Coroutine popRoutine;

    private void Awake()
    {
        button = GetComponent<Button>();
        originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        button.onClick.AddListener(Pop);
    }

    private void OnDisable()
    {
        button.onClick.RemoveListener(Pop);
        popRoutine = null;
        transform.localScale = originalScale;
    }

    public void Pop()
    {
        if (!gameObject.activeInHierarchy) return;

        if (popRoutine != null) StopCoroutine(popRoutine);
        popRoutine = StartCoroutine(PopAnimation());
    }

    private IEnumerator PopAnimation()
    {
        float half = duration / 2f;
        Vector3 big = originalScale * popScale;

        // grow
        for (float time = 0f; time < half; time += Time.unscaledDeltaTime)
        {
            transform.localScale = Vector3.Lerp(originalScale, big, time / half);
            yield return null;
        }

        // shrink back
        for (float time = 0f; time < half; time += Time.unscaledDeltaTime)
        {
            transform.localScale = Vector3.Lerp(big, originalScale, time / half);
            yield return null;
        }

        transform.localScale = originalScale;
        popRoutine = null;
    }
}