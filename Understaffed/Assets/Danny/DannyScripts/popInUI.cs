using System.Collections;
using UnityEngine;

public class popInUI : MonoBehaviour
{
    public float delay = 0f;        // delay before pop in
    public float duration = 0.4f;

    private Vector3 normalScale;

    private void Awake()
    {
        normalScale = transform.localScale;
    }

    private void OnEnable()
    {
        transform.localScale = Vector3.zero;
        StartCoroutine(Pop());
    }

    private IEnumerator Pop()
    {
        yield return new WaitForSecondsRealtime(delay);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // same bounce as PopText
            const float extraSize = 1.9f;
            const float size = extraSize + 1f;
            float eased = 1f + size * Mathf.Pow(t - 1f, 3f) + extraSize * Mathf.Pow(t - 1f, 2f);

            transform.localScale = normalScale * eased;
            yield return null;
        }

        transform.localScale = normalScale;
    }
}
