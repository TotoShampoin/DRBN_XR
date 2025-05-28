using UnityEngine;

public enum TestControlMode
{
    Generator,
    WeightPaint,
};

public class TestControl : MonoBehaviour
{
    public TestControlMode testControlMode;

    MonoBehaviour current;

    Test_Generator test_Generator;
    Test_WeightPaint test_WeightPaint;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        test_Generator = GetComponentInChildren<Test_Generator>();
        test_WeightPaint = GetComponentInChildren<Test_WeightPaint>();

        if (test_Generator == null) throw new System.Exception("Test_Generator not found");
        if (test_WeightPaint == null) throw new System.Exception("test_WeightPaint not found");

        test_Generator.enabled = false;
        test_WeightPaint.enabled = false;
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
            default: break;
        }
    }

    void SwitchTo(MonoBehaviour testThing)
    {
        if (current == null)
        {
            testThing.enabled = true;
            current = testThing;
        }
        if (current != testThing)
        {
            current.enabled = false;
            testThing.enabled = true;
            current = testThing;
        }
    }
}
