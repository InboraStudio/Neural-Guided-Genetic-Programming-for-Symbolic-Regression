using System;
using UnityEngine;

[Serializable]
public enum NodeType 
{ 
    Add, Subtract, Multiply, Divide, 
    Sin, Cos, Log, Exp, Power, Sqrt,
    Variable, Constant 
}

[Serializable]
public class ExpressionNode
{
    public NodeType nodeType;
    public float constantValue;
    public ExpressionNode left;
    public ExpressionNode right;
    
    public ExpressionNode(NodeType type, float value = 0f)
    {
        nodeType = type;
        constantValue = value;
    }
    
    public float Evaluate(float x)
    {
        switch (nodeType)
        {
            case NodeType.Variable:
                return x;
                
            case NodeType.Constant:
                return constantValue;
                
            case NodeType.Add:
                return left.Evaluate(x) + right.Evaluate(x);
                
            case NodeType.Subtract:
                return left.Evaluate(x) - right.Evaluate(x);
                
            case NodeType.Multiply:
                return left.Evaluate(x) * right.Evaluate(x);
                
            case NodeType.Divide:
                float rightVal = right.Evaluate(x);
                return Mathf.Abs(rightVal) < 0.0001f ? 1f : left.Evaluate(x) / rightVal;
                
            case NodeType.Sin:
                return Mathf.Sin(left.Evaluate(x));
                
            case NodeType.Cos:
                return Mathf.Cos(left.Evaluate(x));
                
            case NodeType.Log:
                float logVal = left.Evaluate(x);
                return logVal > 0 ? Mathf.Log(logVal) : 0f;
                
            case NodeType.Exp:
                return Mathf.Exp(Mathf.Clamp(left.Evaluate(x), -10f, 10f));
                
            case NodeType.Power:
                float baseVal = left.Evaluate(x);
                float expVal = right.Evaluate(x);
                return Mathf.Pow(Mathf.Abs(baseVal), Mathf.Clamp(expVal, -5f, 5f));
                
            case NodeType.Sqrt:
                float sqrtVal = left.Evaluate(x);
                return sqrtVal >= 0 ? Mathf.Sqrt(sqrtVal) : 0f;
                
            default:
                return 0f;
        }
    }
    
    public int GetComplexity()
    {
        int count = 1;
        if (left != null) count += left.GetComplexity();
        if (right != null) count += right.GetComplexity();
        return count;
    }
    
    public ExpressionNode Clone()
    {
        ExpressionNode newNode = new ExpressionNode(nodeType, constantValue);
        if (left != null) newNode.left = left.Clone();
        if (right != null) newNode.right = right.Clone();
        return newNode;
    }
    
    public override string ToString()
    {
        switch (nodeType)
        {
            case NodeType.Variable:
                return "x";
            case NodeType.Constant:
                return constantValue.ToString("F2");
            case NodeType.Add:
                return $"({left} + {right})";
            case NodeType.Subtract:
                return $"({left} - {right})";
            case NodeType.Multiply:
                return $"({left} * {right})";
            case NodeType.Divide:
                return $"({left} / {right})";
            case NodeType.Sin:
                return $"sin({left})";
            case NodeType.Cos:
                return $"cos({left})";
            case NodeType.Log:
                return $"log({left})";
            case NodeType.Exp:
                return $"exp({left})";
            case NodeType.Power:
                return $"({left} ^ {right})";
            case NodeType.Sqrt:
                return $"sqrt({left})";
            default:
                return "?";
        }
    }
}
