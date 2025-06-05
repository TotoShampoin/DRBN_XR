using UnityEngine;
using UnityEngine.EventSystems;

public class CubeClickHandler : MonoBehaviour
{
    // Cette méthode est appelée quand le cube est cliqué
    void OnMouseDown()
    {
        // Affiche un message dans la console de débogage
        Debug.Log("Cube cliqué !");
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("Cube cliqué !");
    }
}
