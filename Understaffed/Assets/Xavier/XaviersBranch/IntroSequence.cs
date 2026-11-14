using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IntroSequence : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private Image icon;
    [SerializeField] private UIPop iconPop;
    [SerializeField] private float iconFadeInDuration = 1f;
    [SerializeField] private float popDelay = 2f;
    [SerializeField] private float holdAfterPop = 1f;
    [SerializeField] private float fadeToBlackDuration = 1f;
    [SerializeField] private string mainMenu = "MainMenu";

    private void Start()
    {
        SetAlpha(icon, 0f);
        StartCoroutine(Sequence());
    }

    private IEnumerator Sequence()
    {
        //Start sequence and do the pop thing
        yield return FadeAlpha(icon, 0f, 1f, iconFadeInDuration);
        float wait = Mathf.Max(0f, popDelay - iconFadeInDuration);
        yield return new WaitForSecondsRealtime(wait);
        if (iconPop != null) iconPop.PopEnable();
        yield return new WaitForSecondsRealtime(holdAfterPop);
        //Fade both elements
        Color bgStart = background.color;
        Color iconStart = icon.color;

        for (float time = 0f; time < fadeToBlackDuration; time += Time.unscaledDeltaTime)
        {
            float fadeValue = time / fadeToBlackDuration;
            background.color = Color.Lerp(bgStart, Color.black, fadeValue);
            icon.color = Color.Lerp(iconStart, Color.black, fadeValue);
            yield return null;
        }
        background.color = Color.black;
        icon.color = Color.black;

        //Go back to mainmenu
        icon.gameObject.SetActive(false);
        background.gameObject.SetActive(false);
        SceneManager.LoadScene(mainMenu);
    }

    private IEnumerator FadeAlpha(Graphic graphic, float from, float to, float duration)
    {
        for (float time = 0f; time < duration; time += Time.unscaledDeltaTime)
        {
            SetAlpha(graphic, Mathf.Lerp(from, to, time / duration));
            yield return null;
        }
        SetAlpha(graphic, to);
    }

    private static void SetAlpha(Graphic graphic, float alpha){Color color = graphic.color;color.a = alpha;graphic.color = color;}
}