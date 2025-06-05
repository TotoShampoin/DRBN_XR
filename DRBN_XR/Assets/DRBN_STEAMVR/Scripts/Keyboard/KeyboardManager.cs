using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class KeyboardManager : MonoBehaviour
{
    public TMP_InputField inputField; // L'input à remplir
    public Transform target; // XR camera
    public float distance = 0.5f;   // Distance devant le joueur (plus proche)
    public float heightOffset = -0.5f; // Hauteur sous le regard (plus bas)
    
    public float tiltAngle = -20f;

    public void Appear()
    {
        gameObject.SetActive(true);
        if (target != null)
        {
            // Direction horizontale devant le joueur
            Vector3 flatForward = new Vector3(target.forward.x, 0, target.forward.z).normalized;

            // Position
            Vector3 spawnPos = target.position + flatForward * distance + Vector3.up * heightOffset;
            transform.position = spawnPos;

            // Orientation : tourner vers le joueur + inclinaison (tilt)
            float yaw = target.eulerAngles.y;
            transform.rotation = Quaternion.Euler(tiltAngle, yaw, 0);
        }
    }

    // Appelé par chaque touche
    public void AddLetter(string letter)
    {
        inputField.text += letter.ToLower();
    }

    public void Backspace()
    {
        if (inputField.text.Length > 0)
            inputField.text = inputField.text.Substring(0, inputField.text.Length - 1);
    }

    public void HideKeyboard()
    {
        gameObject.SetActive(false);
    }

    
}
