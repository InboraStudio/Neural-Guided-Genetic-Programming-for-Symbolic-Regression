using System.Collections.Generic;
using UnityEngine;

public class GeneticOperations
{
    private System.Random random;
    
    public GeneticOperations(int seed = -1)
    {
        random = seed >= 0 ? new System.Random(seed) : new System.Random();
    }
    
    public ExpressionNode GenerateRandomTree(int maxDepth, float constantRange = 10f)
    {
        if (maxDepth <= 0 || random.NextDouble() < 0.3)
        {
            if (random.NextDouble() < 0.5)
                return new ExpressionNode(NodeType.Variable);
            else
                return new ExpressionNode(NodeType.Constant, 
                    (float)(random.NextDouble() * constantRange * 2 - constantRange));
        }
        
        NodeType[] binaryOps = { NodeType.Add, NodeType.Subtract, NodeType.Multiply, 
                                 NodeType.Divide, NodeType.Power };
        NodeType[] unaryOps = { NodeType.Sin, NodeType.Cos, NodeType.Log, 
                                NodeType.Exp, NodeType.Sqrt };
        
        bool useBinary = random.NextDouble() < 0.7;
        ExpressionNode node;
        
        if (useBinary)
        {
            node = new ExpressionNode(binaryOps[random.Next(binaryOps.Length)]);
            node.left = GenerateRandomTree(maxDepth - 1, constantRange);
            node.right = GenerateRandomTree(maxDepth - 1, constantRange);
        }
        else
        {
            node = new ExpressionNode(unaryOps[random.Next(unaryOps.Length)]);
            node.left = GenerateRandomTree(maxDepth - 1, constantRange);
        }
        
        return node;
    }
    
    public void MutateConstant(ExpressionNode node, float mutationStrength = 1f)
    {
        List<ExpressionNode> constants = new List<ExpressionNode>();
        CollectConstants(node, constants);
        
        if (constants.Count > 0)
        {
            ExpressionNode target = constants[random.Next(constants.Count)];
            target.constantValue += (float)((random.NextDouble() * 2 - 1) * mutationStrength);
        }
    }
    
    public void MutateSubtree(ExpressionNode node, int maxDepth = 3)
    {
        List<ExpressionNode> allNodes = new List<ExpressionNode>();
        CollectAllNodes(node, allNodes);
        
        if (allNodes.Count > 1)
        {
            ExpressionNode target = allNodes[random.Next(1, allNodes.Count)];
            ExpressionNode newSubtree = GenerateRandomTree(maxDepth);
            
            target.nodeType = newSubtree.nodeType;
            target.constantValue = newSubtree.constantValue;
            target.left = newSubtree.left;
            target.right = newSubtree.right;
        }
    }
    
    public void MutateDelete(ExpressionNode node)
    {
        List<ExpressionNode> nodes = new List<ExpressionNode>();
        CollectAllNodes(node, nodes);
        
        if (nodes.Count > 1)
        {
            ExpressionNode target = nodes[random.Next(1, nodes.Count)];
            
            if (target.left != null && target.right == null)
            {
                ExpressionNode child = target.left;
                target.nodeType = child.nodeType;
                target.constantValue = child.constantValue;
                target.left = child.left;
                target.right = child.right;
            }
            else if (target.left != null && target.right != null)
            {
                ExpressionNode child = random.NextDouble() < 0.5 ? target.left : target.right;
                target.nodeType = child.nodeType;
                target.constantValue = child.constantValue;
                target.left = child.left;
                target.right = child.right;
            }
        }
    }
    
    public void MutateSimplify(ExpressionNode node)
    {
        SimplifyNode(node);
    }
    
    private bool SimplifyNode(ExpressionNode node)
    {
        if (node == null) return false;
        
        bool leftSimplified = node.left != null && SimplifyNode(node.left);
        bool rightSimplified = node.right != null && SimplifyNode(node.right);
        
        if (IsConstantSubtree(node) && node.nodeType != NodeType.Constant)
        {
            float value = node.Evaluate(0);
            node.nodeType = NodeType.Constant;
            node.constantValue = value;
            node.left = null;
            node.right = null;
            return true;
        }
        
        return leftSimplified || rightSimplified;
    }
    
    private bool IsConstantSubtree(ExpressionNode node)
    {
        if (node == null) return true;
        if (node.nodeType == NodeType.Variable) return false;
        if (node.nodeType == NodeType.Constant) return true;
        return IsConstantSubtree(node.left) && IsConstantSubtree(node.right);
    }
    
    public Individual Crossover(Individual parent1, Individual parent2)
    {
        ExpressionNode child = parent1.root.Clone();
        
        List<ExpressionNode> childNodes = new List<ExpressionNode>();
        List<ExpressionNode> parent2Nodes = new List<ExpressionNode>();
        
        CollectAllNodes(child, childNodes);
        CollectAllNodes(parent2.root, parent2Nodes);
        
        if (childNodes.Count > 0 && parent2Nodes.Count > 0)
        {
            ExpressionNode crossoverPoint1 = childNodes[random.Next(childNodes.Count)];
            ExpressionNode crossoverPoint2 = parent2Nodes[random.Next(parent2Nodes.Count)].Clone();
            
            crossoverPoint1.nodeType = crossoverPoint2.nodeType;
            crossoverPoint1.constantValue = crossoverPoint2.constantValue;
            crossoverPoint1.left = crossoverPoint2.left;
            crossoverPoint1.right = crossoverPoint2.right;
        }
        
        return new Individual(child);
    }
    
    private void CollectConstants(ExpressionNode node, List<ExpressionNode> constants)
    {
        if (node == null) return;
        if (node.nodeType == NodeType.Constant) constants.Add(node);
        CollectConstants(node.left, constants);
        CollectConstants(node.right, constants);
    }
    
    private void CollectAllNodes(ExpressionNode node, List<ExpressionNode> nodes)
    {
        if (node == null) return;
        nodes.Add(node);
        CollectAllNodes(node.left, nodes);
        CollectAllNodes(node.right, nodes);
    }
}
