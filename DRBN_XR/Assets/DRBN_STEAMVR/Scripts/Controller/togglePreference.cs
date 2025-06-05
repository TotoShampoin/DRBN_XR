using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// togglePreference class to manage the toggle state and save it in PlayerPrefs.
/// </summary>
/// 
public class togglePreference : MonoBehaviour
{
    [Tooltip("Toggle UI element to manage the preference.")]
    [SerializeField] private Toggle toggle;

    [Tooltip("The key used to save the toggle preference in PlayerPrefs.")]
    public string preferenceKey = "startTutorial";

    [Tooltip("The default value to use if no preference is saved.")]
    public bool defaultValue = true;

    [Tooltip("The MonoBehaviour to invoke a method on when the toogle value changes. (Here the LoadControllerPreference Class)")]
    [SerializeField] private MonoBehaviour targetObject;

    [Tooltip("The name of the method to invoke on the target object when the toggle value changes.")]
    [SerializeField] private string methodName;

    private void Start()
    {
        if (toggle == null)
            toggle = GetComponent<Toggle>();

        bool startTutorial = PlayerPrefs.GetInt(preferenceKey, defaultValue ? 1 : 0) == 1;
        toggle.isOn = startTutorial;

        toggle.onValueChanged.AddListener(OnToggleChanged);
    }

    private void OnToggleChanged(bool isOn)
    {
        PlayerPrefs.SetInt(preferenceKey, isOn ? 1 : 0);
        PlayerPrefs.Save();

        if (targetObject != null && !string.IsNullOrEmpty(methodName))
        {
            targetObject.Invoke(methodName, 0f);
        }
    }

    private void OnDestroy()
    {
        toggle.onValueChanged.RemoveListener(OnToggleChanged);
    }
}
