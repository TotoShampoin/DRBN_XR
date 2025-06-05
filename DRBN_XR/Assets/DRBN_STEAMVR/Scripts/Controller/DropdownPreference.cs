using UnityEngine;
using TMPro;
/// <summary>
/// DropdownPreference class to manage the saving and loading of dropdown preferences using PlayerPrefs.
/// This class is attached to a TMP_Dropdown component and saves the selected index to PlayerPrefs.
/// </summary>
[RequireComponent(typeof(TMP_Dropdown))]
public class DropdownPreference : MonoBehaviour
{
    [Tooltip("The key used to save the dropdown preference in PlayerPrefs.")]
    public string playerPrefKey = "turn";

    [Tooltip("The TMP_Dropdown component to manage.")]
    private TMP_Dropdown dropdown;

    [Tooltip("The default value to use if no preference is saved.")]
    public int defaultValue = 0;

    [Tooltip("The MonoBehaviour to invoke a method on when the dropdown value changes. (Here the LoadControllerPreference Class)")]
    [SerializeField] private MonoBehaviour targetObject;

    [Tooltip("The name of the method to invoke on the target object when the dropdown value changes.")]
    [SerializeField] private string methodName;

    void Awake()
    {
        dropdown = GetComponent<TMP_Dropdown>();

        int savedIndex = PlayerPrefs.GetInt(playerPrefKey, defaultValue);
        dropdown.value = savedIndex;

        dropdown.onValueChanged.AddListener(SavePreference);
    }

    void SavePreference(int index)
    {
        PlayerPrefs.SetInt(playerPrefKey, index);
        PlayerPrefs.Save();

        if (targetObject != null && !string.IsNullOrEmpty(methodName))
        {
            targetObject.Invoke(methodName, 0f);
        }
    }

    void OnDestroy()
    {
        dropdown.onValueChanged.RemoveListener(SavePreference);
    }
}
