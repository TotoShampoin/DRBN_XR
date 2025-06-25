using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Class used to navigate between the panels. 
/// </summary>
public class NavigationMenu : MonoBehaviour
{
    [Header("Panels to switch")]
    public GameObject firstPanel;
    public GameObject secondPanel;

    public void PrintSecondPanel()               // Show the second menu
    {
        if (secondPanel != null)
        {
            secondPanel.SetActive(true);
        }

        if (firstPanel != null)
        {
            firstPanel.SetActive(false);
        }
    }

    public void BackToFirstPanel()               // return to the first panel
    {
        if (secondPanel != null)
        {
            secondPanel.SetActive(false);
        }

        if (firstPanel != null)
        {
            firstPanel.SetActive(true);
        }
    }

    public void SwitchImage()
    {
        if (firstPanel != null && secondPanel != null)
        {
            if (firstPanel.activeSelf)
            {
                firstPanel.SetActive(false);
                secondPanel.SetActive(true);
            }
            else
            {
                firstPanel.SetActive(true);
                secondPanel.SetActive(false);
            }
        }
    }
}
