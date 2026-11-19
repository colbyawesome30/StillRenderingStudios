using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TrashItem : MonoBehaviour, IInteractable
{
    public static readonly List<TrashItem> All = new List<TrashItem>();
    public static int Count => All.Count;
    public float cleanTime = 3f;
    public float penaltyWeight = 1f;
    [SerializeField] private GameObject barRoot;
    [SerializeField] private Slider barSlider;
    [SerializeField] private Image barFill;
    [SerializeField] private bool barFacesCamera = true;
    public Color blinkColor = new Color(1f, 0.25f, 0.25f, 1f);
    public float blinkInterval = 0.4f;
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private readonly List<Renderer> blinkRenderers = new List<Renderer>();
    private MaterialPropertyBlock blinkBlock;
    private bool blinking;
    private bool lastRed;
    private static PlayerInfo playerInfo;
    private Camera cam;
    private bool cleaning;
    private float timeLeft;

    private void OnEnable(){All.Add(this); NotifyPlayerInfo();}

    private void OnDisable(){All.Remove(this); if (gameObject.scene.isLoaded) NotifyPlayerInfo();}

    private void Start()
    {
        cam = Camera.main;

        if (barSlider == null && barRoot != null) barSlider = barRoot.GetComponentInChildren<Slider>(true);
        if (barSlider != null)
        {
            barSlider.minValue = 0f;
            barSlider.maxValue = 1f;
            barSlider.wholeNumbers = false;
            barSlider.interactable = false;
            barSlider.value = 1f;
        }

        if (barRoot != null) barRoot.SetActive(false);

        StartBlink();
    }

    //Tap the trash to start cleaning it
    public void Interact(RaycastHit hit)
    {
        if (cleaning) return;

        cleaning = true;
        StopBlink();
        timeLeft = cleanTime;
        if (barRoot != null) barRoot.SetActive(true);
        UpdateBar();
    }
    //Blink when not cleaned
    private void StartBlink()
    {
        blinkBlock = new MaterialPropertyBlock();
        blinkRenderers.Clear();
        foreach (var render in GetComponentsInChildren<Renderer>(true))
        {
            if (!(render is MeshRenderer || render is SkinnedMeshRenderer || render is SpriteRenderer)) continue;
            if (barRoot != null && render.transform.IsChildOf(barRoot.transform)) continue;
            blinkRenderers.Add(render);
        }
        blinking = true;
        lastRed = false;
        ApplyBlink(true);
    }

    private void ApplyBlink(bool red)
    {
        if (red)
        {
            blinkBlock.Clear();
            blinkBlock.SetColor(ColorId, blinkColor);
            blinkBlock.SetColor(BaseColorId, blinkColor);
        }
        foreach (var render in blinkRenderers)
            if (render != null) render.SetPropertyBlock(red ? blinkBlock : null);
    }

    private void StopBlink()
    {
        if (!blinking) return;
        blinking = false;
        foreach (var render in blinkRenderers)
            if (render != null) render.SetPropertyBlock(null);
        blinkRenderers.Clear();
    }

    private void Update()
    {
        if (blinking)
        {
            bool red = Mathf.FloorToInt(Time.unscaledTime / Mathf.Max(0.05f, blinkInterval)) % 2 == 0;
            if (red != lastRed) { lastRed = red; ApplyBlink(red); }
        }

        if (!cleaning) return;

        timeLeft -= Time.deltaTime;
        UpdateBar();

        if (timeLeft <= 0f) Destroy(gameObject);   //OnDisable remove affect on stars
    }

    private void LateUpdate()
    {
        if (!barFacesCamera || barRoot == null || !barRoot.activeSelf || cam == null) return;
        barRoot.transform.rotation = cam.transform.rotation;
    }

    private void UpdateBar()
    {
        float fraction = Mathf.Clamp01(timeLeft / Mathf.Max(0.01f, cleanTime));
        if (barSlider != null) barSlider.value = fraction;
        if (barFill != null) barFill.fillAmount = fraction;
    }

    //Tell the star rating how much trash is on the floor and affect rating
    private static void NotifyPlayerInfo()
    {
        if (playerInfo == null) playerInfo = FindAnyObjectByType<PlayerInfo>();
        if (playerInfo == null) return;

        float load = 0f;
        foreach (var trash in All)
            if (trash != null) load += trash.penaltyWeight;
        playerInfo.SetTrashLoad(load);
    }
}