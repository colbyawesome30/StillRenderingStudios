using UnityEngine;
using UnityEngine.SceneManagement;

public class InGameSettings : MonoBehaviour
{
    [SerializeField] private GameObject inGameSettings;
    [SerializeField] private GameObject roster;
    [SerializeField] private GameObject builder;
    [SerializeField] private GameObject header;
    [SerializeField] private Timer timer;

    private static bool _settingsIsOpen;
    public static bool SettingsIsOpen => _settingsIsOpen;

    private void Awake()
    {
        if (timer == null) timer = GetComponent<Timer>();
        if (timer == null) timer = FindAnyObjectByType<Timer>();
    }

    private void Start()
    {
        ToggleSettings(false);
    }

    private void OnDestroy()
    {
        _settingsIsOpen = false;   // statics survive scene loads, so reset it
    }

    public void OpenLevel(string levelName)
    {
        SceneManager.LoadScene(levelName);
    }

    public void ToggleSettings(bool Toggle)
    {
        inGameSettings.SetActive(Toggle);

        header.SetActive(!Toggle);
        builder.SetActive(!Toggle);
        roster.SetActive(!Toggle);
        _settingsIsOpen = Toggle;

        if (timer == null) return;

        timer.PauseTimer(Toggle);
        timer.PauseTime(Toggle);
    }
}