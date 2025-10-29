using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class NeuralGuidedGP : MonoBehaviour
{
    [Header("Neural Network Parameters")]
    public int hiddenLayerSize = 64;
    public int encodingDimension = 32;
    public float learningRate = 0.01f;
    public int trainingEpochs = 50;
    
    private ExpressionEncoder encoder;
    private FitnessPredictor predictor;
    private List<TrainingExample> trainingData;
    
    [System.Serializable]
    public class TrainingExample
    {
        public float[] encoding;
        public float fitness;
        public float mse;
        public float complexity;
    }
    
    void Start()
    {
        encoder = new ExpressionEncoder(encodingDimension);
        predictor = new FitnessPredictor(encodingDimension, hiddenLayerSize);
        trainingData = new List<TrainingExample>();
    }
    
    public void TrainOnPopulation(List<Individual> population)
    {
        foreach (Individual ind in population)
        {
            float[] encoding = encoder.Encode(ind.root);
            trainingData.Add(new TrainingExample
            {
                encoding = encoding,
                fitness = ind.fitness,
                mse = ind.mse,
                complexity = ind.complexity
            });
        }
        
        if (trainingData.Count > 100)
        {
            predictor.Train(trainingData, trainingEpochs, learningRate);
            Debug.Log($"<color=cyan>[Neural Predictor] Trained on {trainingData.Count} examples</color>");
        }
    }
    
    public float PredictFitness(ExpressionNode node)
    {
        float[] encoding = encoder.Encode(node);
        return predictor.Predict(encoding);
    }
    
    public List<Individual> GeneratePromisingSeeds(int count, GeneticOperations genOps, int maxDepth)
    {
        List<Individual> seeds = new List<Individual>();
        List<Individual> candidates = new List<Individual>();
        
        for (int i = 0; i < count * 10; i++)
        {
            ExpressionNode tree = genOps.GenerateRandomTree(maxDepth);
            candidates.Add(new Individual(tree));
        }
        
        foreach (Individual candidate in candidates)
        {
            float predictedFitness = PredictFitness(candidate.root);
            candidate.fitness = predictedFitness;
        }
        
        candidates.Sort();
        candidates.Reverse();
        
        for (int i = 0; i < count && i < candidates.Count; i++)
        {
            seeds.Add(candidates[i]);
        }
        
        Debug.Log($"<color=yellow>[Neural Guidance] Generated {seeds.Count} promising seeds</color>");
        return seeds;
    }
}

public class ExpressionEncoder
{
    private int dimension;
    private System.Random random;
    
    public ExpressionEncoder(int encodingDimension)
    {
        dimension = encodingDimension;
        random = new System.Random(42);
    }
    
    public float[] Encode(ExpressionNode node)
    {
        float[] encoding = new float[dimension];
        
        List<NodeType> nodeTypes = new List<NodeType>();
        List<float> constants = new List<float>();
        int depth = 0;
        
        CollectFeatures(node, nodeTypes, constants, ref depth, 0);
        
        encoding[0] = nodeTypes.Count / 50f;
        encoding[1] = depth / 10f;
        encoding[2] = constants.Count / 20f;
        
        if (constants.Count > 0)
        {
            encoding[3] = constants.Average() / 10f;
            encoding[4] = constants.Max() / 10f;
            encoding[5] = constants.Min() / 10f;
        }
        
        int[] opCounts = new int[12];
        foreach (NodeType type in nodeTypes)
        {
            opCounts[(int)type]++;
        }
        
        for (int i = 0; i < Mathf.Min(opCounts.Length, dimension - 6); i++)
        {
            encoding[6 + i] = opCounts[i] / 10f;
        }
        
        for (int i = 18; i < dimension; i++)
        {
            encoding[i] = (float)(random.NextDouble() * 0.1);
        }
        
        return encoding;
    }
    
    private void CollectFeatures(ExpressionNode node, List<NodeType> types, 
                                 List<float> constants, ref int maxDepth, int currentDepth)
    {
        if (node == null) return;
        
        types.Add(node.nodeType);
        if (node.nodeType == NodeType.Constant)
            constants.Add(node.constantValue);
        
        maxDepth = Mathf.Max(maxDepth, currentDepth);
        
        CollectFeatures(node.left, types, constants, ref maxDepth, currentDepth + 1);
        CollectFeatures(node.right, types, constants, ref maxDepth, currentDepth + 1);
    }
}

public class FitnessPredictor
{
    private int inputSize;
    private int hiddenSize;
    private float[,] weightsInputHidden;
    private float[] biasHidden;
    private float[] weightsHiddenOutput;
    private float biasOutput;
    private System.Random random;
    
    public FitnessPredictor(int inputDim, int hiddenDim)
    {
        inputSize = inputDim;
        hiddenSize = hiddenDim;
        random = new System.Random(123);
        
        weightsInputHidden = new float[inputSize, hiddenSize];
        biasHidden = new float[hiddenSize];
        weightsHiddenOutput = new float[hiddenSize];
        biasOutput = 0f;
        
        InitializeWeights();
    }
    
    private void InitializeWeights()
    {
        float scale = Mathf.Sqrt(2f / inputSize);
        for (int i = 0; i < inputSize; i++)
        {
            for (int j = 0; j < hiddenSize; j++)
            {
                weightsInputHidden[i, j] = (float)(random.NextDouble() * 2 - 1) * scale;
            }
        }
        
        scale = Mathf.Sqrt(2f / hiddenSize);
        for (int i = 0; i < hiddenSize; i++)
        {
            weightsHiddenOutput[i] = (float)(random.NextDouble() * 2 - 1) * scale;
        }
    }
    
    public float Predict(float[] input)
    {
        float[] hidden = new float[hiddenSize];
        
        for (int j = 0; j < hiddenSize; j++)
        {
            float sum = biasHidden[j];
            for (int i = 0; i < inputSize; i++)
            {
                sum += input[i] * weightsInputHidden[i, j];
            }
            hidden[j] = ReLU(sum);
        }
        
        float output = biasOutput;
        for (int i = 0; i < hiddenSize; i++)
        {
            output += hidden[i] * weightsHiddenOutput[i];
        }
        
        return output;
    }
    
    public void Train(List<NeuralGuidedGP.TrainingExample> data, int epochs, float lr)
    {
        for (int epoch = 0; epoch < epochs; epoch++)
        {
            float totalLoss = 0f;
            
            foreach (var example in data)
            {
                float prediction = Predict(example.encoding);
                float error = example.fitness - prediction;
                totalLoss += error * error;
                
                float[] hidden = new float[hiddenSize];
                for (int j = 0; j < hiddenSize; j++)
                {
                    float sum = biasHidden[j];
                    for (int i = 0; i < inputSize; i++)
                    {
                        sum += example.encoding[i] * weightsInputHidden[i, j];
                    }
                    hidden[j] = ReLU(sum);
                }
                
                for (int i = 0; i < hiddenSize; i++)
                {
                    weightsHiddenOutput[i] += lr * error * hidden[i];
                }
                biasOutput += lr * error;
                
                for (int i = 0; i < inputSize; i++)
                {
                    for (int j = 0; j < hiddenSize; j++)
                    {
                        float gradient = error * weightsHiddenOutput[j] * 
                                       (hidden[j] > 0 ? 1 : 0) * example.encoding[i];
                        weightsInputHidden[i, j] += lr * gradient;
                    }
                }
            }
            
            if (epoch % 10 == 0)
            {
                Debug.Log($"[Neural Training] Epoch {epoch}, Loss: {totalLoss / data.Count:F4}");
            }
        }
    }
    
    private float ReLU(float x)
    {
        return x > 0 ? x : 0;
    }
}
