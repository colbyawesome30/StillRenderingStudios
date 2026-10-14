using UnityEngine;
using TMPro;

public class moneyPopup : MonoBehaviour
{
    [SerializeField] private TMP_Text label;

    // timing
    [SerializeField] private float lifetime = 1.2f;
    [SerializeField] private float fadeInTime = 0.15f;
    [SerializeField] private float fadeOutTime = 0.5f;

    // units/pixels per second: UI popup around 60, world popup about 1
    [SerializeField] private float floatSpeed = 1f;
    [SerializeField] private bool faceCamera = false;

    // colors
    [SerializeField] private Color gainColor = new Color(0.2f, 0.8f, 0.3f);
    [SerializeField] private Color spendColor = new Color(0.9f, 0.2f, 0.2f);
    private Color baseColor = Color.white;

    private float age;
    private float direction = 1f;
    private Camera cam;

    public void Setup(int amount)
    {
        if (label == null) label = GetComponentInChildren<TMP_Text>();

        bool gain = amount >= 0;
        if (gain == true)
        {
            label.text = "+$" + amount.ToString();
            baseColor = gainColor;
            direction = 1f;
        }
        else if (gain == false)
        {
            label.text = "-$" + Mathf.Abs(amount).ToString();
            baseColor = spendColor;
            direction = -1f;
        }

        SetAlpha(0f); // start invisible so it fades in
        cam = Camera.main;
    }

    private void Update()
    {
        if (label == null) return;

        float dt = Time.unscaledDeltaTime; // unscaled time lets popups fade away properly if/when the game is paused
        age += dt;

        transform.localPosition += Vector3.up * (direction * floatSpeed * dt);

        // keep the world popup facing the camera
        if (faceCamera == true && cam != null)
        {
            transform.rotation = cam.transform.rotation;
        }

        // fade in at the start, fade out at the end
        float alpha = 1f;
        if (age < fadeInTime)
        {
            alpha = age / fadeInTime;
        }
        else if (age > lifetime - fadeOutTime)
        {
            alpha = (lifetime - age) / fadeOutTime;
        }

        SetAlpha(Mathf.Clamp01(alpha));

        if (age >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    private void SetAlpha(float alpha)
    {
        Color c = baseColor;
        c.a = alpha;
        label.color = c;
    } // sets transparency for the fade in/out

}
