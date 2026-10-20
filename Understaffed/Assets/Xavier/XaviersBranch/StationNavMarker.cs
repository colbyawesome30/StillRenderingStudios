using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StationNavMarker : MonoBehaviour
{
    public RectTransform arrow;
    public TMP_Text label;
    private Image arrowGraphic;
    private Color baseColor = Color.white;
    private bool hasBase;
    private bool alerting;
    public bool IsShown { get; set; }

    public void SetLabel(string text){if (label != null) label.text = text;}

    //Make sure there is a marker
    private void EnsureGraphic()
    {
        if (arrowGraphic == null)
            arrowGraphic = arrow != null ? arrow.GetComponent<Image>() : GetComponent<Image>();
        if (arrowGraphic != null && !hasBase)
        {
            baseColor = arrowGraphic.color;
            hasBase = true;
        }
    }
    //Set marker tint
    public void SetTint(Color color)
    {
        EnsureGraphic();
        baseColor = color;
        hasBase = true;
        // while blinking, the alert owns the color; the new tint is restored afterwards
        if (!alerting && arrowGraphic != null) arrowGraphic.color = color;
        //if (label != null) label.color = color;
    }

    //Set image for nav marker
    public void SetArrow(Sprite image)
    {
        EnsureGraphic();
        if (arrowGraphic != null) 
        {
            arrowGraphic.sprite = image;
        }
    }

    // called every frame by the WorkStation while it needs attention
    public void SetAlertState(bool red, Color alertColor)
    {
        EnsureGraphic();
        if (arrowGraphic == null) return;
        alerting = true;
        Color c = alertColor;
        c.a = baseColor.a;
        arrowGraphic.color = red ? c : baseColor;
    }
    //Stop blinking
    public void ClearAlert()
    {
        if (!alerting) return;
        alerting = false;
        EnsureGraphic();
        if (arrowGraphic != null) arrowGraphic.color = baseColor;
    }
}