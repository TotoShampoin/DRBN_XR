using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Management;
using WeightGeneration;

public class TestControl2 : MonoBehaviour
{
    public RenderTexture A, B;
    public Material matA, matB, matC;
    public Noise genA, genB;
    public TextMeshProUGUI distanceText;
    public DistanceOfVolumes distanceOfVolumes;
    public float thresholdThickness = 0.01f;
    [Range(0, 2)] public float shiftB = 0.00f;
    RenderTexture res;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DisableXR();
        res = new(A) { enableRandomWrite = true };
        matC.SetTexture("_MainTex", res);
    }

    // Update is called once per frame
    void Update()
    {
        genB.noiseOffset.y = shiftB;
        genA.Generate(A);
        genB.Generate(B);
        matA.SetFloat("_ThresholdThickness", thresholdThickness);
        matB.SetFloat("_ThresholdThickness", thresholdThickness);

        float distance = distanceOfVolumes.Distance(A, B, res);
        distanceText.text = $"Distance: {distance:F5}";
    }


    public void DisableXR()
    {
        StartCoroutine(StopXR());
    }
    IEnumerator StopXR()
    {
        XRGeneralSettings.Instance.Manager.StopSubsystems();
        XRGeneralSettings.Instance.Manager.DeinitializeLoader();
        yield return null;
    }

}
