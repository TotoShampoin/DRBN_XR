using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;

/// <summary>
/// CircularMenu class to manage a circular menu with 3D buttons (and animation !).
/// Each button can show/hide objects and control a menu controller.
/// </summary>

public class CircularMenu : MonoBehaviour
{
    [System.Serializable]
    public class Button3D
    {
        [Tooltip("Button to open/close the menu/object")]
        public GameObject button;

        [Tooltip("Menu controller to toggle (can be null)")]
        public MenuController menuController;

        [Tooltip("Objects to hide when the menu is closing (can be null)")]
        public GameObject[] objectsToHide;

        [Tooltip("Objects to show when the menu is opening (can be null)")]
        public GameObject[] objectsToShow;

        [Tooltip("Color when the menu is closed (normal state)")]
        public Color normalColor = Color.white;

        [Tooltip("Color when the menu is opened")]
        public Color pressedColor = Color.green;

        [Tooltip("Objects that are toggled when hovering, such as the text showing the menu name.")]
        public List<GameObject> hoverObjectsToToggle = new List<GameObject>();
        // For the animations
        [HideInInspector] public bool isPressed = false;
        [HideInInspector] public Color targetColor;
        [HideInInspector] public Vector3 originalLocalPosition;
        [HideInInspector] public Vector3 targetPosition;
        [HideInInspector] public Vector3 originalScale;
        [HideInInspector] public Vector3 targetScale;
        [HideInInspector] public Image buttonImage;
        [HideInInspector] public bool isAnimating = false;

        // For the hover (text that appears on hover)
        [HideInInspector] public bool isHovering = false;
        [HideInInspector] public float hoverTimer = 0f;
        [HideInInspector] public bool hoverObjectsEnabled = false;
        [HideInInspector] public bool isHoverForcedEnabled = false;

        [Tooltip("If it's the help button, toggle on all the hoverObjectsToToggle when pressed")]
        public bool isHelpButton = false;

        [Tooltip("Close button menu controller (facultatif)")]
        public GameObject closeButton;
    }

    [Tooltip("Time in seconds before the hover objects are activated")]
    public float hoverTime = 1f;
    public Button3D[] button3D; // Array of 3D buttons
    

    // Animation parameters
    [Tooltip("Depth of the press effect")]
    public float pressDepth = 20f;
    [Tooltip("Speed of the animation")]
    public float moveSpeed = 10f;
    [Tooltip("Scale of the button when pressed")]
    public float pressedScale = 0.9f;

    void Start()
    {
        Init();
    }

    public void Init()
    {
        // Initialize the buttons (add listeners, set initial positions, etc.)
        for (int i = 0; i < button3D.Length; i++)
        {
            int index = i;

            // Add listener to toggle the menu or show/hide objects
            button3D[i].button.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => toggleButton(index));

            if (button3D[i].closeButton != null)
            {
                // Add listener to close the menu
                button3D[i].closeButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => toggleButton(index));
            }

            // Set the initial position and scale of the button
            button3D[i].originalLocalPosition = button3D[i].button.transform.localPosition;
            button3D[i].targetPosition = button3D[i].originalLocalPosition;

            button3D[i].originalScale = button3D[i].button.transform.localScale;
            button3D[i].targetScale = button3D[i].originalScale;

            button3D[i].buttonImage = button3D[i].button.GetComponent<Image>();
            if (button3D[i].buttonImage != null)
            {
                button3D[i].buttonImage.color = button3D[i].normalColor;
                button3D[i].targetColor = button3D[i].normalColor;
            }

            // For the hover objects
            AddHoverEvents(button3D[i], index);
        }
    }

    private void AddHoverEvents(Button3D btn, int index)
    {
        EventTrigger trigger = btn.button.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = btn.button.AddComponent<EventTrigger>();

        // OnPointerEnter
        EventTrigger.Entry entryEnter = new EventTrigger.Entry();
        entryEnter.eventID = EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener((eventData) => { OnHoverEnter(index); });
        trigger.triggers.Add(entryEnter);

        // OnPointerExit
        EventTrigger.Entry entryExit = new EventTrigger.Entry();
        entryExit.eventID = EventTriggerType.PointerExit;
        entryExit.callback.AddListener((eventData) => { OnHoverExit(index); });
        trigger.triggers.Add(entryExit);
    }


    void Update()
    {
        // Update the hover timer and check if the hover objects should be enabled
        for (int i = 0; i < button3D.Length; i++)
        {
            if (button3D[i].isHovering)
            {
                button3D[i].hoverTimer += Time.unscaledDeltaTime;
                if (button3D[i].hoverTimer >= hoverTime && !button3D[i].hoverObjectsEnabled)
                {
                    EnableHoverObjects(i);
                }
            }

            if (!button3D[i].isAnimating)
                continue;

            button3D[i].button.transform.localPosition = Vector3.Lerp(button3D[i].button.transform.localPosition, button3D[i].targetPosition, Time.unscaledDeltaTime * moveSpeed);
            button3D[i].button.transform.localScale = Vector3.Lerp(button3D[i].button.transform.localScale, button3D[i].targetScale, Time.unscaledDeltaTime * moveSpeed);

            if (button3D[i].buttonImage != null)
            {
                button3D[i].buttonImage.color = Color.Lerp(button3D[i].buttonImage.color, button3D[i].targetColor, Time.unscaledDeltaTime * moveSpeed);
            }

            if (Vector3.Distance(button3D[i].button.transform.localPosition, button3D[i].targetPosition) < 0.01f &&
                Vector3.Distance(button3D[i].button.transform.localScale, button3D[i].targetScale) < 0.01f)
            {
                button3D[i].isAnimating = false;
            }
        }
    }

    private void OnHoverEnter(int index)
    {
        button3D[index].isHovering = true;
        button3D[index].hoverTimer = 0f;
    }

    private void OnHoverExit(int index)
    {
        button3D[index].isHovering = false;
        button3D[index].hoverTimer = 0f;
        DisableHoverObjects(index);
    }

    private void EnableHoverObjects(int index) // For the help button (show all texts). Force the hover objects to be enabled
    {
        foreach (GameObject obj in button3D[index].hoverObjectsToToggle)
        {
            obj.SetActive(true);
        }
        button3D[index].hoverObjectsEnabled = true;
    }

    private void DisableHoverObjects(int index) // For the help button (hide all texts). Force the hover objects to be disabled
    {
        if (button3D[index].isHoverForcedEnabled)
            return;
        foreach (GameObject obj in button3D[index].hoverObjectsToToggle)
        {
            obj.SetActive(false);
        }
        button3D[index].hoverObjectsEnabled = false;
    }

    public void ToggleForcedEnabled() // Help button
    {
        for (int i = 0; i < button3D.Length; i++)
        {
            if (button3D[i].isHoverForcedEnabled == true)
            {
                button3D[i].isHoverForcedEnabled = false;
                DisableHoverObjects(i);
            }
            else{
                button3D[i].isHoverForcedEnabled = true;
                EnableHoverObjects(i);
            }

        }
    }


    public void toggleButton(int index) // The logic to toggle the button and show/hide objects
    {
        if (index < 0 || index >= button3D.Length) // Check if the index is valid
        {
            Debug.LogError("Invalid button index: " + index);
            return;
        }

        //does nothing if a button is animating
        for (int i = 0; i < button3D.Length; i++)
        {
            if (button3D[i].isAnimating == true)
                return;
        }

        // If the button is a help button, toggle the forced enabled state
        if(button3D[index].isHelpButton == true)
        {
            ToggleForcedEnabled();
        }


        // We need to toggle all the other buttons off
        for (int i = 0; i < button3D.Length; i++)
        {
            if (button3D[i].isPressed == true && i != index)
            {
                toggleButton(i);
            }
        }

        // Show/hide objects and toggle menu controller
        if (button3D[index].objectsToShow != null && button3D[index].isPressed == false)
        {
            // Menu is opening, toggle on the objects to show
            foreach (GameObject obj in button3D[index].objectsToShow)
            {
                obj.SetActive(true);
            }
        }
        else if (button3D[index].objectsToHide != null && button3D[index].isPressed == true)
        {
            // Menu is closing, toggle off the objects to hide
            foreach (GameObject obj in button3D[index].objectsToHide)
            {
                obj.SetActive(false);
            }
        }

        // If we have a menu controller, toggle it (as simple as that)
        if (button3D[index].menuController != null)
        {
            button3D[index].menuController.ToggleMenu();
        }

        // Animation logic
        button3D[index].isPressed = !button3D[index].isPressed;
        button3D[index].targetPosition = button3D[index].originalLocalPosition + (button3D[index].isPressed ? new Vector3(0, 0, pressDepth) : Vector3.zero);
        button3D[index].targetColor = button3D[index].isPressed ? button3D[index].pressedColor : button3D[index].normalColor;
        button3D[index].targetScale = button3D[index].isPressed ? button3D[index].originalScale * pressedScale : button3D[index].originalScale;

        button3D[index].isAnimating = true; // Start the animation for the button
    }
}
