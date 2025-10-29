using System;
using UnityEngine;

[Serializable]
public class Individual : IComparable<Individual>
{
    public ExpressionNode root;
    public float fitness;
    public float mse;
    public float complexity;
    public float crowdingDistance;
    public int dominationCount;
    
    public Individual(ExpressionNode expressionRoot)
    {
        root = expressionRoot;
        crowdingDistance = 0f;
        dominationCount = 0;
    }
    
    public void CalculateFitness(float[] inputData, float[] outputData, float complexityWeight)
    {
        mse = 0f;
        int validPoints = 0;
        
        for (int i = 0; i < inputData.Length; i++)
        {
            float predicted = root.Evaluate(inputData[i]);
            
            if (!float.IsNaN(predicted) && !float.IsInfinity(predicted))
            {
                float error = outputData[i] - predicted;
                mse += error * error;
                validPoints++;
            }
        }
        
        if (validPoints > 0)
        {
            mse /= validPoints;
        }
        else
        {
            mse = float.MaxValue;
        }
        
        complexity = root.GetComplexity();
        fitness = -(mse + complexityWeight * complexity);
    }
    
    public int CompareTo(Individual other)
    {
        return fitness.CompareTo(other.fitness);
    }
    
    public Individual Clone()
    {
        return new Individual(root.Clone());
    }
}
