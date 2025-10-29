using System.Collections.Generic;
using UnityEngine;

public class ConstantOptimizer
{
    private float learningRate = 0.01f;
    private int maxIterations = 100;
    private float tolerance = 1e-6f;
    
    public void OptimizeConstants(Individual individual, float[] inputData, float[] outputData)
    {
        List<ExpressionNode> constants = new List<ExpressionNode>();
        CollectConstants(individual.root, constants);
        
        if (constants.Count == 0) return;
        
        for (int iter = 0; iter < maxIterations; iter++)
        {
            float currentMSE = CalculateMSE(individual.root, inputData, outputData);
            
            foreach (ExpressionNode constant in constants)
            {
                float originalValue = constant.constantValue;
                
                constant.constantValue = originalValue + tolerance;
                float msePlus = CalculateMSE(individual.root, inputData, outputData);
                
                constant.constantValue = originalValue - tolerance;
                float mseMinus = CalculateMSE(individual.root, inputData, outputData);
                
                float gradient = (msePlus - mseMinus) / (2 * tolerance);
                constant.constantValue = originalValue - learningRate * gradient;
            }
            
            float newMSE = CalculateMSE(individual.root, inputData, outputData);
            
            if (Mathf.Abs(newMSE - currentMSE) < tolerance)
                break;
        }
        
        individual.CalculateFitness(inputData, outputData, individual.complexity);
    }
    
    private float CalculateMSE(ExpressionNode root, float[] inputData, float[] outputData)
    {
        float mse = 0f;
        for (int i = 0; i < inputData.Length; i++)
        {
            float predicted = root.Evaluate(inputData[i]);
            float error = outputData[i] - predicted;
            mse += error * error;
        }
        return mse / inputData.Length;
    }
    
    private void CollectConstants(ExpressionNode node, List<ExpressionNode> constants)
    {
        if (node == null) return;
        if (node.nodeType == NodeType.Constant) constants.Add(node);
        CollectConstants(node.left, constants);
        CollectConstants(node.right, constants);
    }
}
