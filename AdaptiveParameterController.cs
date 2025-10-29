using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AdaptiveParameterController
{
    public float minMutationRate = 0.3f;
    public float maxMutationRate = 0.95f;
    public float minComplexityWeight = 0.5f;
    public float maxComplexityWeight = 5f;
    
    public float MeasureDiversity(List<Individual> population)
    {
        if (population.Count < 2) return 0f;
        
        float totalDistance = 0f;
        int comparisons = 0;
        
        System.Random rng = new System.Random();
        int samples = Mathf.Min(50, population.Count / 2);
        
        for (int i = 0; i < samples; i++)
        {
            int idx1 = rng.Next(population.Count);
            int idx2 = rng.Next(population.Count);
            
            if (idx1 != idx2)
            {
                float distance = TreeDistance(population[idx1].root, population[idx2].root);
                totalDistance += distance;
                comparisons++;
            }
        }
        
        return comparisons > 0 ? totalDistance / comparisons : 0f;
    }
    
    private float TreeDistance(ExpressionNode tree1, ExpressionNode tree2)
    {
        if (tree1 == null && tree2 == null) return 0f;
        if (tree1 == null || tree2 == null) return 1f;
        
        float distance = tree1.nodeType != tree2.nodeType ? 1f : 0f;
        
        if (tree1.nodeType == NodeType.Constant && tree2.nodeType == NodeType.Constant)
        {
            distance += Mathf.Abs(tree1.constantValue - tree2.constantValue) / 10f;
        }
        
        distance += TreeDistance(tree1.left, tree2.left);
        distance += TreeDistance(tree1.right, tree2.right);
        
        return distance;
    }
    
    public float MeasureProgress(List<float> recentBestFitness)
    {
        if (recentBestFitness.Count < 2) return 1f;
        
        float improvement = 0f;
        for (int i = 1; i < recentBestFitness.Count; i++)
        {
            improvement += recentBestFitness[i] - recentBestFitness[i - 1];
        }
        
        return improvement / (recentBestFitness.Count - 1);
    }
    
    public float AdaptMutationRate(float currentRate, float diversity, float progress)
    {
        float targetRate = currentRate;
        
        if (diversity < 0.1f && progress < 0.01f)
        {
            targetRate = Mathf.Min(targetRate * 1.1f, maxMutationRate);
        }
        else if (diversity > 0.5f && progress > 0.05f)
        {
            targetRate = Mathf.Max(targetRate * 0.95f, minMutationRate);
        }
        
        return targetRate;
    }
    
    public float AdaptComplexityWeight(float currentWeight, float avgComplexity, float targetComplexity)
    {
        float targetWeight = currentWeight;
        
        if (avgComplexity > targetComplexity * 1.5f)
        {
            targetWeight = Mathf.Min(targetWeight * 1.05f, maxComplexityWeight);
        }
        else if (avgComplexity < targetComplexity * 0.5f)
        {
            targetWeight = Mathf.Max(targetWeight * 0.95f, minComplexityWeight);
        }
        
        return targetWeight;
    }
}
