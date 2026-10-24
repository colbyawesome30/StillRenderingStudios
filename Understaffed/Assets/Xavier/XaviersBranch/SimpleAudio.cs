using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonAudio : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;
    [Range(0f, 1f)] [SerializeField] private float hoverVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float clickVolume = 1f;

    public AudioSource audioSource;
    private Button button;

    private void Awake()
    {
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        button = GetComponent<Button>();
    }

    //OnHover Play Sound
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!IsInteractable()) return;
        Play(hoverSound, hoverVolume);
    }
    //OnHover Click Sound
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsInteractable()) return;
        Play(clickSound, clickVolume);
    }
    //Can button be interacted with
    private bool IsInteractable() => button == null || button.interactable;

    //Play sound
    private void Play(AudioClip clip, float volume)
    {
        if (clip != null) audioSource.PlayOneShot(clip, volume);
    }
}