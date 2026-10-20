using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StationNavMarker : MonoBehaviour
{
    public RectTransform arrow;
    public TMP_Text label;
    private Graphic arrowGraphic;
    public bool IsShown { get; set; }

    public void SetLabel(string text){if (label != null) label.text = text;}
    public void SetTint(Color color)
    {
        if (arrowGraphic == null)
            arrowGraphic = arrow != null ? arrow.GetComponent<Graphic>() : GetComponent<Graphic>();
        if (arrowGraphic != null) arrowGraphic.color = color;
        if (label != null) label.color = color;
    }
}