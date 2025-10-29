using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;

// Add the enum at the top level (outside the class)
public enum MigrationTopology { Ring, FullyConnected, Star, Random }

public class AdvancedSymbolicRegressionGP : MonoBehaviour
{
    [Header("Dataset")]
    public float[] inputData;
    public float[] outputData;
    
    [Header("Island Model Parameters")]
    public int numberOfIslands = 4;
    public int populationPerIsland = 250;
    public int maxGenerations = 1000;
    public float deathRate = 0.7f;
    public int eliteCount = 5;
    public MigrationTopology topology = MigrationTopology.Ring;
    public int migrationInterval = 20;
    public int migrantsPerIsland = 3;
    
    [Header("Tree Parameters")]
    public int initialMaxDepth = 4;
    
    [Header("Neural Guidance")]
    public bool useNeuralGuidance = true;
    public int neuralGuidanceInterval = 50;
    public int neuralSeedCount = 10;
    
    [Header("Visualization")]
    public GPVisualizationManager visualizationManager;
    public bool enableVisualization = true;
    
    [Header("Advanced Options")]
    public bool useConstantOptimization = true;
    public int optimizeEveryNGenerations = 50;
    
    private List<Island> islands;
    private ConstantOptimizer constantOptimizer;
    private NeuralGuidedGP neuralGuidance;
    
    public Individual bestIndividual;
    private Individual previousBest;
    
    void Start()
    {
        // Example: Fit x^2 + 1
        // In Start() method of AdvancedSymbolicRegressionGP.cs, replace with:
inputData = new float[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
outputData = new float[] { 0, 1, 4, 9, 16, 25, 36, 49, 64, 81, 100 }; // x^2

        
        Run();
    }
    
    public void Run()
    {
        constantOptimizer = new ConstantOptimizer();
        
        if (useNeuralGuidance)
        {
            GameObject neuralObj = new GameObject("Neural Guidance");
            neuralGuidance = neuralObj.AddComponent<NeuralGuidedGP>();
        }
        
        InitializeIslands();
        
        for (int gen = 0; gen < maxGenerations; gen++)
        {
            // Parallel evolution of islands
            Parallel.ForEach(islands, island =>
            {
                island.EvolveGeneration(inputData, outputData, deathRate, eliteCount);
            });
            
            // Migration between islands
            if (gen > 0 && gen % migrationInterval == 0)
            {
                PerformMigration();
            }
            
            bestIndividual = GetGlobalBest();
            
            // Neural guidance injection
            if (useNeuralGuidance && neuralGuidance != null && gen > 0 && gen % neuralGuidanceInterval == 0)
            {
                List<Individual> allPopulation = new List<Individual>();
                foreach (Island island in islands)
                {
                    allPopulation.AddRange(island.population);
                }
                
                neuralGuidance.TrainOnPopulation(allPopulation);
                
                List<Individual> promisingSeeds = neuralGuidance.GeneratePromisingSeeds(
                    neuralSeedCount, islands[0].geneticOps, initialMaxDepth);
                
                // Inject promising seeds into islands
                foreach (Individual seed in promisingSeeds)
                {
                    seed.CalculateFitness(inputData, outputData, islands[0].complexityWeight);
                }
                
                int islandIdx = 0;
                foreach (Individual seed in promisingSeeds)
                {
                    int worstIdx = islands[islandIdx].population.Count - 1;
                    islands[islandIdx].population[worstIdx] = seed;
                    islandIdx = (islandIdx + 1) % islands.Count;
                }
            }
            
            // Constant optimization
            if (useConstantOptimization && gen % optimizeEveryNGenerations == 0 && gen > 0)
            {
                constantOptimizer.OptimizeConstants(bestIndividual, inputData, outputData);
            }
            
            // Visualization updates
            if (enableVisualization && visualizationManager != null)
            {
                visualizationManager.UpdateVisualization(gen, bestIndividual, islands);
                
                if (gen % 50 == 0)
                {
                    visualizationManager.VisualizeExpressionTree(bestIndividual.root);
                }
            }
            
            // Breakthrough detection
            if (previousBest != null && bestIndividual.fitness > previousBest.fitness + 0.1f)
            {
                Debug.Log($"<color=green>█ BREAKTHROUGH at Gen {gen}! " +
                         $"Fitness improved by {(bestIndividual.fitness - previousBest.fitness):F4}</color>");
                
                if (visualizationManager != null)
                {
                    visualizationManager.ShowBestFoundEffect(Vector3.zero);
                }
            }
            
            previousBest = bestIndividual.Clone();
            
            // Progress logging
            if (gen % 10 == 0)
            {
                string bar = new string('!', Mathf.Min((int)((-bestIndividual.mse) * 50), 50));
                Debug.Log($"<color=cyan>[Gen {gen:D4}]</color> " +
                         $"<color=yellow>MSE: {bestIndividual.mse:F6}</color> " +
                         $"<color=magenta>Complexity: {bestIndividual.complexity}</color>\n" +
                         $"<color=green>{bar}</color>");
            }
            
            // Early stopping
            if (bestIndividual.mse < 0.001f)
            {
                //Debug.Log($"<color=green>╔══════════════════════════════════════╗</color>");
                Debug.Log($"<color=green>   PERFECT SOLUTION FOUND! Gen {gen:D4}  </color>");
                //Debug.Log($"<color=green>╚══════════════════════════════════════╝</color>");
                break;
            }
        }
        
        // Final results
      //  Debug.Log($"\n<color=cyan>╔═══════════════ FINAL RESULT ═══════════════╗</color>");
        Debug.Log($"<color=yellow>  f(x) = {bestIndividual.root}</color>");
        Debug.Log($"<color=yellow>  MSE: {bestIndividual.mse:F8}</color>");
        Debug.Log($"<color=yellow>  Complexity: {bestIndividual.complexity}</color>");
        //Debug.Log($"<color=cyan>╚═══════════════════════════════════════════╝</color>\n");
    }
    
    private void InitializeIslands()
    {
        islands = new List<Island>();
        
        // Create heterogeneous islands with different parameters
        for (int i = 0; i < numberOfIslands; i++)
        {
            float mutRate = 0.7f + (i * 0.05f);
            float compWeight = 1f + (i * 0.5f);
            int maxDepth = initialMaxDepth + (i % 2);
            
            islands.Add(new Island(i, populationPerIsland, mutRate, compWeight, maxDepth));
        }
    }
    
    private void PerformMigration()
    {
        List<Individual> migrants = new List<Individual>();
        
        // Collect best individuals from each island
        foreach (Island island in islands)
        {
            island.population.Sort();
            island.population.Reverse();
            
            for (int i = 0; i < migrantsPerIsland; i++)
            {
                migrants.Add(island.population[i].Clone());
            }
        }
        
        if (visualizationManager != null)
        {
            visualizationManager.ShowMigrationEffect(0, 1);
        }
        
        // Perform migration based on topology
        switch (topology)
        {
            case MigrationTopology.Ring:
                MigrateRing(migrants);
                break;
            case MigrationTopology.FullyConnected:
                MigrateFullyConnected(migrants);
                break;
            case MigrationTopology.Star:
                MigrateStar(migrants);
                break;
            case MigrationTopology.Random:
                MigrateRandom(migrants);
                break;
        }
    }
    
    private void MigrateRing(List<Individual> migrants)
    {
        for (int i = 0; i < islands.Count; i++)
        {
            int targetIsland = (i + 1) % islands.Count;
            int startIdx = i * migrantsPerIsland;
            
            for (int j = 0; j < migrantsPerIsland; j++)
            {
                int worstIdx = islands[targetIsland].population.Count - 1 - j;
                islands[targetIsland].population[worstIdx] = migrants[startIdx + j];
            }
        }
    }
    
    private void MigrateFullyConnected(List<Individual> migrants)
    {
        foreach (Island island in islands)
        {
            foreach (Individual migrant in migrants)
            {
                int worstIdx = island.population.Count - 1;
                if (migrant.fitness > island.population[worstIdx].fitness)
                {
                    island.population[worstIdx] = migrant.Clone();
                    island.population.Sort();
                    island.population.Reverse();
                }
            }
        }
    }
    
    private void MigrateStar(List<Individual> migrants)
    {
        Island hub = islands[0];
        
        for (int i = 1; i < islands.Count; i++)
        {
            for (int j = 0; j < migrantsPerIsland; j++)
            {
                int worstIdx = islands[i].population.Count - 1 - j;
                islands[i].population[worstIdx] = hub.population[j].Clone();
            }
            
            for (int j = 0; j < migrantsPerIsland; j++)
            {
                int worstIdx = hub.population.Count - 1 - j;
                hub.population[worstIdx] = islands[i].population[j].Clone();
            }
        }
    }
    
    private void MigrateRandom(List<Individual> migrants)
    {
        System.Random rng = new System.Random();
        foreach (Individual migrant in migrants)
        {
            int targetIsland = rng.Next(islands.Count);
            int worstIdx = islands[targetIsland].population.Count - 1;
            islands[targetIsland].population[worstIdx] = migrant.Clone();
        }
    }
    
    private Individual GetGlobalBest()
    {
        Individual best = null;
        foreach (Island island in islands)
        {
            island.population.Sort();
            island.population.Reverse();
            if (best == null || island.population[0].fitness > best.fitness)
            {
                best = island.population[0];
            }
        }
        return best;
    }
}
