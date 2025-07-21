using TMPro;
using UnityEngine;

/// <summary>
/// A TMP UI component designed to specifically display numbers.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class FloatToText : MonoBehaviour
{
    public int digits = 1;
    TextMeshProUGUI tmp;

    void Start()
    {
        tmp = GetComponent<TextMeshProUGUI>();
    }

    //
    public void SetText(float number) => tmp.text = $"{Mathf.Round(number * Mathf.Pow(10, digits)) / Mathf.Pow(10, digits)}";
}
