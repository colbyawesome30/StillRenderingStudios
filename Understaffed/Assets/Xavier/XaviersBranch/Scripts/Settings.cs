using UnityEngine;
using UnityEngine.UI;

public class Settings : MonoBehaviour
{
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider fxSlider;


    ///When the settings menu is opened, set the sliders to the current values
    private void OnEnable()
    {
        masterSlider.SetValueWithoutNotify(GameSettings.Master);
        musicSlider.SetValueWithoutNotify(GameSettings.Music);
        fxSlider.SetValueWithoutNotify(GameSettings.Fx);

        masterSlider.onValueChanged.AddListener(GameSettings.SetMaster);
        musicSlider.onValueChanged.AddListener(GameSettings.SetMusic);
        fxSlider.onValueChanged.AddListener(GameSettings.SetFx);
    }

    //When the settings menu is closed, save the current values to PlayerPrefs
    private void OnDisable()
    {
        masterSlider.onValueChanged.RemoveListener(GameSettings.SetMaster);
        musicSlider.onValueChanged.RemoveListener(GameSettings.SetMusic);
        fxSlider.onValueChanged.RemoveListener(GameSettings.SetFx);
        GameSettings.Save();
    }

    //When paused, save the current values to PlayerPrefs
    private void OnApplicationPause(bool paused)
    {
        if (paused) GameSettings.Save();
    }

    public void Apply()
    {
        GameSettings.Apply();
        GameSettings.Save();
    }
}