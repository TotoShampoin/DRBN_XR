using UnityEngine;

/// <summary>
/// Tutorial class to manage the tutorial steps in the game.
/// </summary>

public class Tutorial : MonoBehaviour
{
    [System.Serializable]
    public class Step
    {
        [Tooltip("Objects to toggle on this step.")]
        public GameObject[] objectsToToggle;
    }

    [Tooltip("Preference key to check if the tutorial should be shown at Start.")]
    public string preferenceKey = "startTutorial";

    [Tooltip("Tutorial GameObject")]
    public GameObject tutorialUI;

    [Tooltip("Button to go to the previous step.")]
    public GameObject previousStepButton;


    public Step[] steps;

    private int step = 0; // current step index

    void Start()
    {
        bool shouldInit = PlayerPrefs.GetInt(preferenceKey, 1) == 1;
        // By default, the tutorial is show at the start

        if (shouldInit)
        {
            Init();
        }
    }


    public void Init()
    {
        // Take the first step and desactive all the other steps 
        step = 0;
        deactiveAndActiveCurrentGameObjects(step);
        tutorialUI.SetActive(true);

        //Deactivate the previous step button, since we are at the first step
        previousStepButton.SetActive(false);
    }

    public void NextStep()
    {
        step++;
        // End of the tutorial, we desactive the tutorial UI
        if (step >= steps.Length)
        {
            tutorialUI.SetActive(false);
            return;
        }

        deactiveAndActiveCurrentGameObjects(step);

        //if we are not at the first step,w e activate the previous step button
        if (step > 0)
        {
            previousStepButton.SetActive(true);
        }
    }

    public void PreviousStep()
    {
        step--;
        if (step == 0)
        {
            previousStepButton.SetActive(false);
        }
        deactiveAndActiveCurrentGameObjects(step);
    }
    private void setActiveGameObjects(bool active, GameObject[] gameObjects)
    {
        foreach (GameObject obj in gameObjects)
        {
            obj.SetActive(active);
        }
    }

    private void deactiveAndActiveCurrentGameObjects(int step)
    {
        for (int i = 0; i < steps.Length; i++)
        {
            if (i == step)
            {
                setActiveGameObjects(true, steps[i].objectsToToggle);
            }
            else
            {
                setActiveGameObjects(false, steps[i].objectsToToggle);
            }
        }
    }
}
