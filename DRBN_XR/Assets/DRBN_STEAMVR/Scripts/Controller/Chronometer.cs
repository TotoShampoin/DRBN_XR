using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.EventSystems;

/// <summary>
/// Chronometer class to manage a chronometer and a clock.
/// The chronometer can be started, paused, and reset. It also displays the current time.
/// </summary>

public class Chronometer : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // Chronometer
    [Header("Chronometer")]
    [Tooltip("Text to display the elapsed time")]
    public TextMeshProUGUI chronoTimeText;
    [Tooltip("Button to start the chronometer")]
    public Button playButton;
    [Tooltip("Button to pause the chronometer")]
    public Button pauseButton;
    [Tooltip("Button to reset the chronometer")]
    public Button resetButton;

    private float elapsedTime = 0f;
    private bool isRunning = false;

    // Resize on hover
    [Header("Resize on Hover (chronometer)")]
    [Tooltip("Target to resize")]
    public RectTransform target;
    [Tooltip("Size when not hovered")]
    public Vector2 normalSize = new Vector2(225f, 100f);
    [Tooltip("Size when hovered")]
    public Vector2 hoverSize = new Vector2(270f, 100f);
    [Tooltip("Speed of the resize in seconds")]
    public float resizeSpeed = 5f;
    private Vector2 targetSize;

    // Clock
    [Header("Clock")]
    [Tooltip("Text to display the current time")]

    public TextMeshProUGUI clockTimeText;
    private string clockLastTime = "";
    
    void Start()
    {
        // For the chronometer
        UpdateTimeDisplay();
        UpdateButtons();

        playButton.onClick.AddListener(PlayChrono);
        pauseButton.onClick.AddListener(PauseChrono);
        resetButton.onClick.AddListener(ResetChrono);

        // Hover
        if (target == null)
            target = GetComponent<RectTransform>();

        targetSize = normalSize;
        target.sizeDelta = normalSize;
    }

    void Update()
    {
        // Update the chronometer
        if (isRunning)
        {
            elapsedTime += Time.unscaledDeltaTime;
            UpdateTimeDisplay();
        }
    
        // Update Hover effect
        target.sizeDelta = Vector2.Lerp(target.sizeDelta, targetSize, Time.unscaledDeltaTime * resizeSpeed);

        // Update the clock
        string currentTime = DateTime.Now.ToString("HH:mm");

        if (currentTime != clockLastTime)
        {
            clockLastTime = currentTime;
            clockTimeText.text = currentTime;
        }
    }

    public void PlayChrono()
    {
        isRunning = true;
        UpdateButtons();
    }

    public void PauseChrono()
    {
        isRunning = false;
        UpdateButtons();
    }

    public void ResetChrono()
    {
        elapsedTime = 0f;
        isRunning = false;
        UpdateTimeDisplay();
        UpdateButtons();
    }

    void UpdateTimeDisplay() // Pour le chrono
    {
        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);
        int hundredths = Mathf.FloorToInt((elapsedTime * 100f) % 100);

        chronoTimeText.text = $"{minutes:00}:{seconds:00},{hundredths:00}";
    }

    void UpdateButtons() // Pour le chrono
    {
        playButton.gameObject.SetActive(!isRunning);
        pauseButton.gameObject.SetActive(isRunning);
    }

    public void OnPointerEnter(PointerEventData eventData) // On Pointer Enter -> Hover animation
    {
        targetSize = hoverSize;
    }

    public void OnPointerExit(PointerEventData eventData) // On Pointer Exit -> Hover animation Exit
    {
        targetSize = normalSize;
    }
}
