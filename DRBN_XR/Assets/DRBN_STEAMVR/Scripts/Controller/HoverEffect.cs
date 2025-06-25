using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// HoverEffects class to manage hover effects on UI elements.
/// </summary>

public class HoverEffects : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Tooltip("Outline to enable on hover. Can be null.")]
    public Behaviour outlineToEnable;

    [Tooltip("Shadow to enable on hover. Can be null.")]
    public Shadow shadowToEnable;

    [Tooltip("Shadow to disable on hover. Can be null.")]
    public Shadow shadowToDisable;

    [Tooltip("Toggle that will be toggled on click. Can be null. This can be used when to toggle a toggle that is a child of the object this script is attached to.")]
    public Toggle toggleToToggle;

    [Tooltip("Dropdown that will be shown on click. Can be null. This can be used when to toggle a dropdown that is a child of the object this script is attached to.")]
    public TMP_Dropdown dropdownToShow;

    [Tooltip("Button that will be pressed on click. Can be null. This can be used when to toggle a button that is a child of the object this script is attached to.")]
    public Button buttonToPress;

    [Header("Shadow Animation")]
    [Tooltip("Enable shadow animation on hover.")]
    public bool enableShadowAnimation = true;

    [Tooltip("Duration of the shadow animation in seconds.")]
    private float shadowAnimDuration = 0.05f;

    [Tooltip("Start position of the shadow animation.")]
    public Vector2 shadowStart = Vector2.zero;

    [Tooltip("End position of the shadow animation.")]
    public Vector2 shadowEnd = new Vector2(2f, -2f);

    private bool isHovered = false;
    private Coroutine shadowAnimCoroutine;

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;

        if (outlineToEnable != null)
            outlineToEnable.enabled = true;

        if (shadowToEnable != null)
        {
            shadowToEnable.enabled = true;

            if (enableShadowAnimation)
            {
                if (shadowAnimCoroutine != null)
                    StopCoroutine(shadowAnimCoroutine);

                shadowAnimCoroutine = StartCoroutine(AnimateShadow(shadowToEnable, shadowStart, shadowEnd, shadowAnimDuration));
            }
            else
            {
                shadowToEnable.effectDistance = shadowEnd;
            }
        }

        if (shadowToDisable != null)
            shadowToDisable.enabled = false;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;

        if (outlineToEnable != null)
            outlineToEnable.enabled = false;

        if (shadowToEnable != null)
        {
            shadowToEnable.enabled = false;

            if (shadowAnimCoroutine != null)
            {
                StopCoroutine(shadowAnimCoroutine);
                shadowAnimCoroutine = null;
            }
        }

        if (shadowToDisable != null)
            shadowToDisable.enabled = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (toggleToToggle != null)
            toggleToToggle.isOn = !toggleToToggle.isOn;

        if (dropdownToShow != null)
        {
            dropdownToShow.gameObject.SetActive(true);
            dropdownToShow.Show();
        }

        if (buttonToPress != null)
            buttonToPress.onClick.Invoke();
    }

    private IEnumerator AnimateShadow(Shadow shadow, Vector2 start, Vector2 end, float duration)
    {
        float time = 0f;

        while (time < duration)
        {
            if (!isHovered)
                yield break;

            float t = time / duration;
            shadow.effectDistance = Vector2.Lerp(start, end, t);
            time += Time.unscaledDeltaTime;
            yield return null;
        }

        shadow.effectDistance = end;
        shadowAnimCoroutine = null;
    }
}
