using UnityEngine;

/// <summary>
/// Class used to exit the simulation from the simulation
/// </summary>
public class QuitApp : MonoBehaviour
{
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}