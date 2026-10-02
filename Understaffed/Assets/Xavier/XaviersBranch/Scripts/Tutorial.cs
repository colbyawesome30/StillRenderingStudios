using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class Tutorial : MonoBehaviour
{
    public bool tutorialComplete;
    private PlayerInfo playerInfo;
    private Timer timer;
    public int TutorialStep;
    [SerializeField]private TMP_Text stepDescriptionText;
    [SerializeField]private TMP_Text stepNameText;
    [SerializeField]private Button stepNextButton;
    [SerializeField]private Button stepPreviousButton;
    [SerializeField]private RawImage videoDisplay;
    [SerializeField]private GameObject videoBorder;

    [System.Serializable]public class StepInformation
    {
        public string stepName;
        public int stepID;
        public string stepDescription;
        public VideoClip video;
        public bool stepComplete;
        public bool pauseTimer;
        public bool freezeGame;
        public bool hasVideo => video != null;
    }

    [SerializeField]public List<StepInformation> tutorialSteps = new List<StepInformation>();

    protected void Start()
    {
        playerInfo = GameObject.FindFirstObjectByType<PlayerInfo>();
        timer = GameObject.FindFirstObjectByType<Timer>();

        if (!playerInfo.TutorialComplete)
        {
            Time.timeScale = 0;
            StartTutorial();
        }
    }

    public void CheckTimerPause()
    {
        if (tutorialSteps[TutorialStep].pauseTimer)
        {
            timer.PauseTimer(true);
        }
        else
        {
            timer.PauseTimer(false);
        }
    }

    public void CheckGameFreeze()
    {
        if (tutorialSteps[TutorialStep].freezeGame)
        {
            Time.timeScale = 0;
        }
        else
        {
            Time.timeScale = 1;
        }
    }

    public void UpdatePreviousButtonState()
    {
        stepPreviousButton.interactable = TutorialStep > 0;
    }

    public void StartTutorial()
    {
        TutorialStep = 0;
        CheckTimerPause();
        CheckGameFreeze();
        SetText();
        CheckStepCompletion();
        UpdatePreviousButtonState();

        for (int i = 0; i < tutorialSteps.Count; i++)
        {
            tutorialSteps[i].stepID = i;
        }
    }

    public void EndTutorial()
    {
        Time.timeScale = 1;
        tutorialComplete = true;
        playerInfo.TutorialComplete = tutorialComplete;
        stepPreviousButton.interactable = false;
        stepNextButton.interactable = false;
        videoDisplay.enabled = false;
    }

    public void NextStep()
    {
        if (!tutorialSteps[TutorialStep].stepComplete)
        {
            Debug.Log("Step " + TutorialStep + " not complete yet, can't advance.");
            return;
        }

        if (TutorialStep < tutorialSteps.Count - 1)
        {
            TutorialStep++;
            SetText();
            CheckTimerPause();
            CheckGameFreeze();
            CheckStepCompletion();
            UpdatePreviousButtonState();
            Debug.Log("Tutorial Step: " + TutorialStep);
        }
        if (TutorialStep == tutorialSteps.Count - 1)
        {
            EndTutorial();
        }
    }
    public void PreviousStep()
    {
        if (TutorialStep > 0)
        {
            TutorialStep--;
            SetText();
            CheckTimerPause();
            CheckGameFreeze();
            CheckStepCompletion();
        }
        UpdatePreviousButtonState();
        Debug.Log("Tutorial Step: " + TutorialStep);
    }

    public void SetText()
    {
        videoDisplay.enabled = tutorialSteps[TutorialStep].hasVideo;
        videoBorder.SetActive(tutorialSteps[TutorialStep].hasVideo);
        stepNameText.text = tutorialSteps[TutorialStep].stepName;
        stepDescriptionText.text = tutorialSteps[TutorialStep].stepDescription;
        if (tutorialSteps[TutorialStep].video != null)
        {
            videoDisplay.GetComponent<VideoPlayer>().clip = tutorialSteps[TutorialStep].video;
            videoDisplay.GetComponent<VideoPlayer>().Play();
        }
    }

    public void CheckStepCompletion()
    {
        if (tutorialSteps[TutorialStep].stepComplete)
        {
            stepNextButton.interactable = true;
        }
        else
        {
            stepNextButton.interactable = false;
        }
    }
}
