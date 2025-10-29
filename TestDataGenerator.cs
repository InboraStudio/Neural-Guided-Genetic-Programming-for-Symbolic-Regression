using UnityEngine;

public class TestDataGenerator : MonoBehaviour
{
    public enum TestFunction
    {
        Quadratic,      // x^2 + 1
        Cubic,          // x^3 - 2x
        Trigonometric,  // sin(x) + cos(x)
        Exponential,    // e^x
        Complex,        // x^2 * sin(x)
        Rational        // (x + 1) / (x + 2)
    }
    
    public TestFunction selectedFunction = TestFunction.Quadratic;
    public int numDataPoints = 20;
    public float minX = -5f;
    public float maxX = 5f;
    
    private AdvancedSymbolicRegressionGP gpController;
    
    void Start()
    {
        gpController = GetComponent<AdvancedSymbolicRegressionGP>();
        GenerateTestData();
    }
    
    public void GenerateTestData()
    {
        float[] inputs = new float[numDataPoints];
        float[] outputs = new float[numDataPoints];
        
        for (int i = 0; i < numDataPoints; i++)
        {
            float x = Mathf.Lerp(minX, maxX, i / (float)(numDataPoints - 1));
            inputs[i] = x;
            outputs[i] = EvaluateFunction(x);
        }
         
        gpController.inputData = inputs;
        gpController.outputData = outputs;
        
        Debug.Log($"<color=cyan>Generated {numDataPoints} data points for: {selectedFunction}</color>");
        Debug.Log($"<color=yellow>Sample: f({inputs[0]:F2}) = {outputs[0]:F2}</color>");
    }
    
    float EvaluateFunction(float x)
    {
        switch (selectedFunction)
        {
            case TestFunction.Quadratic:
                return x * x + 1f;
                
            case TestFunction.Cubic:
                return x * x * x - 2f * x;
                
            case TestFunction.Trigonometric:
                return Mathf.Sin(x) + Mathf.Cos(x);
                
            case TestFunction.Exponential:
                return Mathf.Exp(Mathf.Clamp(x, -2f, 2f)); // Clamped to prevent overflow
                
            case TestFunction.Complex:
                return x * x * Mathf.Sin(x);
                
            case TestFunction.Rational:
                return (x + 1f) / (x + 2f + 0.01f); // Small offset to avoid division by zero
                
            default:
                return 0f;
        }
    }
    
    [ContextMenu("Regenerate Data")]
    public void RegenerateData()
    {
        GenerateTestData();
    }
}
