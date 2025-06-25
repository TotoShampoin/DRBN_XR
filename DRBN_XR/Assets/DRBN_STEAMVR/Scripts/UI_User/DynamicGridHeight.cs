using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Class used to adjust the component rect transform of a gridLayoutGroup
/// </summary>
public class DynamicGridHeight : MonoBehaviour
{
    [Tooltip("Container where elements are add")]
    public RectTransform content;

    [Tooltip("GridLayout which need a resize")]
    public GridLayoutGroup gridLayoutGroup;
    private float heightIncrement = 40f; // value add every two elements
    private bool needsAdjustment = true;
    
    void Start()
    {
        // Content and gridLayout must be assigned
        if (content == null)
        {
            content = GetComponent<RectTransform>();
        }

        if (gridLayoutGroup == null)
        {
            gridLayoutGroup = GetComponentInParent<GridLayoutGroup>();
        }
        AdjustContentHeight();
    }

    void LateUpdate()
    {
        if (needsAdjustment)
        {
            AdjustContentHeight();
            needsAdjustment = false;
        }
    }

    public void AdjustContentHeight()
    {
        int childCount = content.childCount;
        float newHeight = Mathf.CeilToInt((float)childCount / 2) * heightIncrement;
        content.sizeDelta = new Vector2(content.sizeDelta.x, newHeight + 10);
    }

    // this method must be call at each new element
    public void OnElementAdded()
    {
        AdjustContentHeight();
    }
}
