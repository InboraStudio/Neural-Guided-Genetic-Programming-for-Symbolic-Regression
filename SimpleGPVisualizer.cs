using UnityEngine;
using System.Text;

public class SimpleGPVisualizer : MonoBehaviour
{
    public AdvancedSymbolicRegressionGP gpController;
    public float updateInterval = 1f;
    
    private float lastUpdate;
    private StringBuilder sb;
    
    void Start()
    {
        sb = new StringBuilder();
    }
    
    void Update()
    {
        if (Time.time - lastUpdate > updateInterval && gpController != null && gpController.bestIndividual != null)
        {
            DisplayProgress();
            lastUpdate = Time.time;
        }
    }
    
    void DisplayProgress()
    {
        sb.Clear();
        
        Individual best = gpController.bestIndividual;
        
       // sb.AppendLine("┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓");
        sb.AppendLine("   GENETIC PROGRAMMING PROGRESS      ");
       // sb.AppendLine("┣━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┫");
        sb.AppendLine($" Expression: {best.root.ToString().PadRight(24)} ");
        sb.AppendLine($" MSE:        {best.mse:F8}              ");
        sb.AppendLine($" Fitness:    {best.fitness:F6}                ");
        sb.AppendLine($" Complexity: {best.complexity}                        ");
        
        // Progress bar
        float progress = Mathf.Clamp01(-best.mse / 100f);
        int barLength = 30;
        int filled = Mathf.RoundToInt(progress * barLength);
        string bar = new string('!', filled) + new string('!', barLength - filled);
        sb.AppendLine($" Progress: [{bar}] ");
        
       // sb.AppendLine("┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛");
        
        Debug.Log(sb.ToString());
    }
}
