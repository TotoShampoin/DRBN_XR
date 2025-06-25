using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;

/// <summary>
/// Class who change the inputField for a search bar
/// </summary>
public class SearchBar : MonoBehaviour
{
    [Tooltip("Input field used as a search bar")]
    public TMP_InputField searchInputField;

    [Tooltip("GridLayout which contains every options")]
    public GridLayoutGroup gridLayoutGroup;

    void Start()
    {
        searchInputField.onValueChanged.AddListener(OnSearchTextChanged);
    }

    void OnSearchTextChanged(string searchText)
    {
        string pattern = Regex.Escape(searchText).Replace("\\*", "\\d");

        foreach (Transform child in gridLayoutGroup.transform)
        {
            // Compare child name with search
            if (Regex.IsMatch(child.name, "^" + pattern, RegexOptions.IgnoreCase))
            {
                child.gameObject.SetActive(true);
            }
            else
            {
                child.gameObject.SetActive(false);
            }
        }
    }
}
