using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class debugFastForward : MonoBehaviour
{
    [SerializeField] private float fastForward = 2f;

    private bool spedUp = false;
    public Button toggleButton;
    public Color activeColor = Color.green;
    public Color inactiveColor = Color.red;

    void Start()
    {
        toggleButton.onClick.AddListener(ToggleState);
        UpdateVisuals();
    }

    void Update()
    {
        if (spedUp && Time.timeScale == 1f) Time.timeScale = fastForward; // accounts for timescale changing when pausing and unpausing while game is sped up
    }

    void ToggleState()
    {
        if (!spedUp && Time.timeScale == 1f)
        {
            Time.timeScale = fastForward;
            spedUp = true;
        }
        else if (spedUp)
        {
            if (Time.timeScale == fastForward) Time.timeScale = 1f;
            spedUp = false;
        }

        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        ColorBlock colors = toggleButton.colors;
        colors.normalColor = spedUp ? activeColor : inactiveColor;
        colors.selectedColor = colors.normalColor;
        toggleButton.colors = colors;
    }

    void OnDisable()
    {
        if (spedUp && Time.timeScale == fastForward)
        {
            Time.timeScale = 1f;
        }
        spedUp = false;
        UpdateVisuals();
    }
}