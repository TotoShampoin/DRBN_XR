using UnityEngine;

public class KeyButton : MonoBehaviour
{
    public KeyboardManager keyboardManager;

    // Appelle ça dans un event OnClick ou quand le bouton est "pressé" en VR
    public void PressKey()
    {
        string key = gameObject.name;
        keyboardManager.AddLetter(key);
    }

    public void Backspace()
    {
        keyboardManager.Backspace();
    }

    public void HideKeyboard()
    {
        keyboardManager.HideKeyboard();
    }
}
