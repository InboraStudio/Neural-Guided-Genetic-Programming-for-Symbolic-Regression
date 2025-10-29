using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class Island
{
    public int id;
    public List<Individual> population;
    public GeneticOperations geneticOps;
    public System.Random random;
    public SimulatedAnnealingGP simAnneal;
    public AdaptiveParameterController adaptiveController;
    
    public float mutationRate;
    public float complexityWeight;
    public int initialMaxDepth;
    
    private List<float> recentBestFitness;
    
    public Island(int islandId, int popSize, float mutRate, float compWeight, int maxDepth)
    {
        id = islandId;
        mutationRate = mutRate;
        complexityWeight = compWeight;
        initialMaxDepth = maxDepth;
        random = new System.Random(islandId * 1000);
        geneticOps = new GeneticOperations(islandId * 1000);
        simAnneal = new SimulatedAnnealingGP();
        simAnneal.Initialize();
        adaptiveController = new AdaptiveParameterController();
        recentBestFitness = new List<float>();
        
        population = new List<Individual>();
        for (int i = 0; i < popSize; i++)
        {
            ExpressionNode tree = geneticOps.GenerateRandomTree(initialMaxDepth);
            population.Add(new Individual(tree));
        }
    }
    
    public void EvolveGeneration(float[] inputData, float[] outputData, float deathRate, int eliteCount)
    {
        foreach (Individual ind in population)
        {
            ind.CalculateFitness(inputData, outputData, complexityWeight);
        }
        
        population.Sort();
        population.Reverse();
        
        recentBestFitness.Add(population[0].fitness);
        if (recentBestFitness.Count > 20)
            recentBestFitness.RemoveAt(0);
        
        if (recentBestFitness.Count >= 5 && recentBestFitness.Count % 5 == 0)
        {
            float diversity = adaptiveController.MeasureDiversity(population);
            float progress = adaptiveController.MeasureProgress(recentBestFitness);
            
            mutationRate = adaptiveController.AdaptMutationRate(mutationRate, diversity, progress);
            
            float avgComplexity = population.Average(ind => ind.complexity);
            complexityWeight = adaptiveController.AdaptComplexityWeight(
                complexityWeight, avgComplexity, initialMaxDepth * 2);
        }
        
        int killCount = Mathf.FloorToInt(population.Count * deathRate);
        int surviveCount = population.Count - killCount;
        
        List<Individual> nextGen = new List<Individual>();
        
        for (int i = 0; i < eliteCount && i < surviveCount; i++)
        {
            nextGen.Add(population[i].Clone());
        }
        
        while (nextGen.Count < population.Count)
        {
            double reproductionType = random.NextDouble();
            Individual child;
            
            if (reproductionType < 0.7)
            {
                Individual parent1 = TournamentSelection(5);
                Individual parent2 = TournamentSelection(5);
                child = geneticOps.Crossover(parent1, parent2);
            }
            else
            {
                Individual parent = TournamentSelection(5);
                child = parent.Clone();
                
                if (random.NextDouble() < mutationRate)
                {
                    ApplyRandomMutation(child);
                }
            }
            
            child.CalculateFitness(inputData, outputData, complexityWeight);
            nextGen.Add(child);
        }
        
        simAnneal.CoolDown();
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
