using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System.Linq;

public class IslandModelGP : MonoBehaviour
{
    [Header("Island Model Parameters")]
    public int numberOfIslands = 4;
    public int migrationInterval = 20; // Generations between migrations
    public int migrantsPerIsland = 3;
    public MigrationTopology topology = MigrationTopology.Ring;
    
    public enum MigrationTopology { Ring, FullyConnected, Star, Random }
    
    private List<Island> islands;
    
    [System.Serializable]
    public class Island
    {
        public int id;
        public List<Individual> population;
        public GeneticOperations geneticOps;
        public System.Random random;
        
        // Island-specific parameters (heterogeneous islands)
        public float mutationRate;
        public float complexityWeight;
        public int initialMaxDepth;
        
        public Island(int islandId, int popSize, float mutRate, float compWeight, int maxDepth)
        {
            id = islandId;
            mutationRate = mutRate;
            complexityWeight = compWeight;
            initialMaxDepth = maxDepth;
            random = new System.Random(islandId * 1000);
            geneticOps = new GeneticOperations(islandId * 1000);
            
            // Initialize population
            population = new List<Individual>();
            for (int i = 0; i < popSize; i++)
            {
                ExpressionNode tree = geneticOps.GenerateRandomTree(initialMaxDepth);
                population.Add(new Individual(tree));
            }
        }
        
        public void EvolveGeneration(float[] inputData, float[] outputData, float deathRate, int eliteCount)
        {
            // Evaluate fitness
            foreach (Individual ind in population)
            {
                ind.CalculateFitness(inputData, outputData, complexityWeight);
            }
            
            // Sort by fitness
            population.Sort();
            population.Reverse();
            
            int killCount = Mathf.FloorToInt(population.Count * deathRate);
            int surviveCount = population.Count - killCount;
            
            List<Individual> nextGen = new List<Individual>();
            
            // Elitism
            for (int i = 0; i < eliteCount && i < surviveCount; i++)
            {
                nextGen.Add(population[i].Clone());
            }
            
            // Reproduce
            while (nextGen.Count < population.Count)
            {
                Individual parent = TournamentSelection(5);
                Individual child = parent.Clone();
                
                // Apply mutations
                if (random.NextDouble() < mutationRate)
                {
                    ApplyRandomMutation(child);
                }
                
                nextGen.Add(child);
            }
            
            population = nextGen;
        }
        
        private void ApplyRandomMutation(Individual child)
        {
            double mutType = random.NextDouble();
            if (mutType < 0.25)
                geneticOps.MutateConstant(child.root, 1f);
            else if (mutType < 0.5)
                geneticOps.MutateSubtree(child.root, 3);
            else if (mutType < 0.75)
                geneticOps.MutateDelete(child.root);
            else
                geneticOps.MutateSimplify(child.root);
        }
        
        private Individual TournamentSelection(int tournamentSize)
        {
            Individual best = null;
            for (int i = 0; i < tournamentSize; i++)
            {
                Individual candidate = population[random.Next(population.Count)];
                if (best == null || candidate.fitness > best.fitness)
                    best = candidate;
            }
            return best;
        }
    }
    
    public void InitializeIslands(int popPerIsland, float[] inputData, float[] outputData)
    {
        islands = new List<Island>();
        
        // Create heterogeneous islands with different parameters
        for (int i = 0; i < numberOfIslands; i++)
        {
            float mutRate = 0.7f + (i * 0.05f); // Vary mutation rates
            float compWeight = 1f + (i * 0.5f); // Vary complexity preferences
            int maxDepth = 3 + i; // Vary initial tree depths
            
            islands.Add(new Island(i, popPerIsland, mutRate, compWeight, maxDepth));
        }
    }
    
    public void RunIslandModel(float[] inputData, float[] outputData, int generations, float deathRate, int eliteCount)
    {
        InitializeIslands(250, inputData, outputData); // 4 islands x 250 = 1000 total
        
        for (int gen = 0; gen < generations; gen++)
        {
            // Evolve all islands in parallel
            Parallel.ForEach(islands, island =>
            {
                island.EvolveGeneration(inputData, outputData, deathRate, eliteCount);
            });
            
            // Perform migration
            if (gen > 0 && gen % migrationInterval == 0)
            {
                PerformMigration();
            }
            
            // Report best across all islands
            if (gen % 10 == 0)
            {
                Individual globalBest = GetGlobalBest();
                Debug.Log($"Gen {gen}: Best MSE={globalBest.mse:F6}, " +
                         $"Complexity={globalBest.complexity}, " +
                         $"Expression={globalBest.root}");
            }
        }
    }
    
    private void PerformMigration()
    {
        List<Individual> migrants = new List<Individual>();
        
        // Collect migrants from each island (best individuals)
        foreach (Island island in islands)
        {
            island.population.Sort();
            island.population.Reverse();
            
            for (int i = 0; i < migrantsPerIsland; i++)
            {
                migrants.Add(island.population[i].Clone());
            }
        }
        
        // Distribute migrants based on topology
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
        // Send best individuals to next island in ring
        for (int i = 0; i < islands.Count; i++)
        {
            int targetIsland = (i + 1) % islands.Count;
            int startIdx = i * migrantsPerIsland;
            
            for (int j = 0; j < migrantsPerIsland; j++)
            {
                // Replace worst individuals with migrants
                int worstIdx = islands[targetIsland].population.Count - 1 - j;
                islands[targetIsland].population[worstIdx] = migrants[startIdx + j];
            }
        }
    }
    
    private void MigrateFullyConnected(List<Individual> migrants)
    {
        // Broadcast best from each island to all others
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
        // Island 0 is hub - exchanges with all others
        Island hub = islands[0];
        
        for (int i = 1; i < islands.Count; i++)
        {
            // Send hub's best to island i
            for (int j = 0; j < migrantsPerIsland; j++)
            {
                int worstIdx = islands[i].population.Count - 1 - j;
                islands[i].population[worstIdx] = hub.population[j].Clone();
            }
            
            // Send island i's best to hub
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
