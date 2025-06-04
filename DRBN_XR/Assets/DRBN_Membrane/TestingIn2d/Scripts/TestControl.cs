using System.Collections;
using UnityEngine;
using UnityEngine.XR.Management;

public enum TestControlMode
{
    Generator,
    WeightPaint,
    Voxelizer,
};

public class TestControl : MonoBehaviour
{
    public TestControlMode testControlMode;
    public Material textureRenderer;
    public float thresholdThickness = 0.01f;

    MonoBehaviour current;

    Test_Generator test_Generator;
    Test_WeightPaint test_WeightPaint;
    Test_Voxelizer test_Voxelizer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        test_Generator = GetComponentInChildren<Test_Generator>();
        test_WeightPaint = GetComponentInChildren<Test_WeightPaint>();
        test_Voxelizer = GetComponentInChildren<Test_Voxelizer>();

        if (test_Generator == null) throw new System.Exception("Test_Generator not found");
        if (test_WeightPaint == null) throw new System.Exception("test_WeightPaint not found");
        if (test_Voxelizer == null) throw new System.Exception("test_Voxelizer not found");

        test_Generator.gameObject.SetActive(false);
        test_WeightPaint.gameObject.SetActive(false);
        test_Voxelizer.gameObject.SetActive(false);

        DisableXR();
    }

    // Update is called once per frame
    void Update()
    {
        switch (testControlMode)
        {
            case TestControlMode.Generator:
                SwitchTo(test_Generator);
                break;
            case TestControlMode.WeightPaint:
                SwitchTo(test_WeightPaint);
                break;
            case TestControlMode.Voxelizer:
                SwitchTo(test_Voxelizer);
                break;
            default: break;
        }
        textureRenderer.SetFloat("_ThresholdThickness", thresholdThickness);
    }

    void SwitchTo(MonoBehaviour testThing)
    {
        if (current == null)
        {
            testThing.gameObject.SetActive(true);
            current = testThing;
        }
        if (current != testThing)
        {
            current.gameObject.SetActive(false);
            testThing.gameObject.SetActive(true);
            current = testThing;
        }
    }


    // Call this when entering the non-XR scene
    public void DisableXR()
    {
        StartCoroutine(StopXR());
    }

    // Call this when leaving the non-XR scene (to re-enable XR)
    public void EnableXR()
    {
        StartCoroutine(StartXR());
    }

    IEnumerator StopXR()
    {
        XRGeneralSettings.Instance.Manager.StopSubsystems();
        XRGeneralSettings.Instance.Manager.DeinitializeLoader();
        yield return null;
    }

    IEnumerator StartXR()
    {
        XRGeneralSettings.Instance.Manager.InitializeLoaderSync();
        XRGeneralSettings.Instance.Manager.StartSubsystems();
        yield return null;
    }
}
