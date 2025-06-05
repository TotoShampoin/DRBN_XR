using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    [Tooltip("Transform of the start position of the menu")]
    public Transform startTransform;

    [Tooltip("Transform of the end position of the menu")]
    public Transform endTransform;

    [Tooltip("Duration of the animation in seconds")]
    public float animationDuration = 0.2f;

    [System.Serializable]
    public class Tabs
    {
        [Tooltip("Panel of the tab")]
        public GameObject tabPanel;
        
        [Tooltip("Button to open the tab")]
        public GameObject tabButton;

        [Tooltip("Image of the tab")]
        public Image tabImage;

        [Tooltip("Selector of the tab (visual image that shows the tab is selected)")]
        public GameObject tabSelector;
        
        [Tooltip("Title of the tab")]
        public GameObject tabTitle;

        [Tooltip("Objects to hide when the tab when closing the tab")]
        public GameObject[] objectsToHide;
        
        [Tooltip("Objects to show when the tab is opened")]
        public GameObject[] objectsToShow;

        [Tooltip("Scripts to deactivate when the tab is closed")]
        public MonoBehaviour[] scriptsToDeactivate;
        
        [Tooltip("Scripts to activate when the tab is opened")]
        public MonoBehaviour[] scriptsToActivate;
    }
    [Tooltip("Tabs to manage")]
    public Tabs[] tabs;

    private bool isAnimating = false;
    private bool isMenuOpen = false;

    public int CurrentTabIndex { get; private set; } = 0;

    void Start()
    {
        Init();
    }

    public void Init()
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            int index = i;

            // Add listener to the button to change the tab to the one we clicked on
            var button = tabs[i].tabButton.GetComponent<UnityEngine.UI.Button>();
            button.onClick.AddListener(() => ChangeTab(index));

            // Initialize the tab image alpha to 60f (not selected)
            SetImageAlpha(tabs[i].tabImage, 60f);

            // Event for hovering over the tab button
            UnityEngine.EventSystems.EventTrigger trigger = tabs[i].tabButton.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (trigger == null)
                trigger = tabs[i].tabButton.AddComponent<UnityEngine.EventSystems.EventTrigger>();

            var entryEnter = new UnityEngine.EventSystems.EventTrigger.Entry
            {
                eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter
            };
            entryEnter.callback.AddListener((data) =>
            {
                if (index != CurrentTabIndex)
                    SetImageAlpha(tabs[index].tabImage, 255f);
            });
            trigger.triggers.Add(entryEnter);

            var entryExit = new UnityEngine.EventSystems.EventTrigger.Entry
            {
                eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit
            };
            entryExit.callback.AddListener((data) =>
            {
                if (index != CurrentTabIndex)
                    SetImageAlpha(tabs[index].tabImage, 60f);
            });
            trigger.triggers.Add(entryExit);
        }

        // Set the first tab as active at the start
        ChangeTab(0);
    }

    public void ChangeTab(int index, bool activateScripts = true)
    {
        if (index < 0 || index >= tabs.Length) return;

        // Deactivate all tabs, selectors, titles, objects to show, objects to hide, and scripts
        for (int i = 0; i < tabs.Length; i++)
        {
            tabs[i].tabPanel.SetActive(false);
            tabs[i].tabSelector.SetActive(false);
            tabs[i].tabTitle.SetActive(false);

            SetImageAlpha(tabs[i].tabImage, 60f);

            if (tabs[i].objectsToHide != null)
            {
                foreach (GameObject obj in tabs[i].objectsToHide)
                {
                    if (obj != null)
                        obj.SetActive(false);
                }
            }

            if (tabs[i].objectsToShow != null)
            {
                foreach (GameObject obj in tabs[i].objectsToShow)
                {
                    if (obj != null)
                        obj.SetActive(false);
                }
            }

            if (tabs[i].scriptsToDeactivate != null)
            {
                foreach (var script in tabs[i].scriptsToDeactivate)
                {
                    if (script != null)
                        script.enabled = false;
                }
            }
        }

        //Activate the new tab, its associated elements and its scripts
        tabs[index].tabPanel.SetActive(true);
        tabs[index].tabSelector.SetActive(true);
        tabs[index].tabTitle.SetActive(true);

        SetImageAlpha(tabs[index].tabImage, 255f);

        if (tabs[index].objectsToShow != null)
        {
            foreach (GameObject obj in tabs[index].objectsToShow)
            {
                if (obj != null)
                    obj.SetActive(true);
            }
        }

        if (activateScripts == true && tabs[index].scriptsToActivate != null)
        {
            foreach (var script in tabs[index].scriptsToActivate)
            {
                if (script != null)
                    script.enabled = true;
            }
        }
        CurrentTabIndex = index;
    }


    //Open the menu (not the tab but the menu itself)
    public void ShowMenu()
    {
        if (isAnimating || isMenuOpen || startTransform == null || endTransform == null || gameObject == null)
            return;

        gameObject.SetActive(true);
        gameObject.transform.position = startTransform.position;
        gameObject.transform.rotation = startTransform.rotation;
        gameObject.transform.localScale = Vector3.one * 0.1f;

        // Activate the first tab and its associated elements
        if (tabs.Length > 0 && tabs[0].scriptsToActivate != null)
        {
            foreach (var script in tabs[0].scriptsToActivate)
            {
                if (script != null)
                {
                    script.enabled = true;
                }
            }
        }

        if (tabs.Length > 0 && tabs[0].objectsToShow != null)
        {
            foreach (GameObject obj in tabs[0].objectsToShow)
            {
                if (obj != null)
                    obj.SetActive(true);
            }
        }
        // Start la coroutine (animation) pour ouvrir le menu
        StartCoroutine(AnimateMenu(endTransform, true));
    }

    public void CloseMenu()
    {
        if (isAnimating || !isMenuOpen || startTransform == null || gameObject == null)
            return;
        // Start la coroutine (animation) pour fermer le menu
        StartCoroutine(AnimateMenu(startTransform, false));
    }

    public void ToggleMenu()
    {
        if (isAnimating || gameObject == null)
            return;

        if (isMenuOpen)
        {
            CloseMenu();
        }
        else
        {
            ShowMenu();
        }
    }

    private System.Collections.IEnumerator AnimateMenu(Transform target, bool opening)
    {
        //animation logic
        
        isAnimating = true;
        float elapsed = 0f;

        Vector3 startPos = gameObject.transform.position;
        Quaternion startRot = gameObject.transform.rotation;
        Vector3 startScale = gameObject.transform.localScale;

        Vector3 targetPos = target.position;
        Quaternion targetRot = target.rotation;
        Vector3 targetScale = opening ? target.localScale : Vector3.one * 0.1f;

        while (elapsed < animationDuration)
        {
            float t = elapsed / animationDuration;

            gameObject.transform.position = Vector3.Lerp(startPos, targetPos, t);
            gameObject.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            gameObject.transform.localScale = Vector3.Lerp(startScale, targetScale, t);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        gameObject.transform.position = targetPos;
        gameObject.transform.rotation = targetRot;
        gameObject.transform.localScale = targetScale;

        isAnimating = false;
        isMenuOpen = opening;

        if (!opening)
        {
            gameObject.SetActive(false);
            ChangeTab(0, activateScripts: false);

            // Désactiver tous les objectsToShow (au cas où)
            for (int i = 0; i < tabs.Length; i++)
            {
                if (tabs[i].objectsToShow != null)
                {
                    foreach (GameObject obj in tabs[i].objectsToShow)
                    {
                        if (obj != null)
                            obj.SetActive(false);
                    }
                }
            }
        }
    }

    void Togglescripts(MonoBehaviour[] scripts, bool enable)
    {
        if (scripts != null)
        {
            foreach (var script in scripts)
            {
                if (script != null)
                {
                    script.enabled = enable;
                }
            }
        }
    }

    private void SetImageAlpha(Image image, float alpha)
{
    if (image != null)
    {
        Color color = image.color;
        color.a = alpha / 255f;
        image.color = color;
    }
}

}
