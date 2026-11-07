using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class StartupMenu : MonoBehaviour
{
    [SerializeField]private VideoClip startVideoPlayer;
    [SerializeField]private VideoClip normalVideoPlayer;
    [SerializeField]private List<VideoPlayer> videoPlayers;

    public void Start()
    {
        foreach (var vp in videoPlayers)
        {
            vp.isLooping = false;
            vp.clip = startVideoPlayer;
            vp.loopPointReached += OnVideoComplete;
        }
    }

    private void OnVideoComplete(VideoPlayer vp)
    {
        vp.clip = normalVideoPlayer;
        vp.isLooping = true;
    }
}
