using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MultiObjectiveGP
{
    public bool Dominates(Individual a, Individual b)
    {
        bool betterOrEqual = a.mse <= b.mse && a.complexity <= b.complexity;
        bool strictlyBetter = a.mse < b.mse || a.complexity < b.complexity;
        return betterOrEqual && strictlyBetter;
    }
    
    public List<List<Individual>> FastNonDominatedSort(List<Individual> population)
    {
        List<List<Individual>> fronts = new List<List<Individual>>();
        int[] dominationCount = new int[population.Count];
        List<int>[] dominatedSolutions = new List<int>[population.Count];
        
        for (int i = 0; i < population.Count; i++)
        {
            dominatedSolutions[i] = new List<int>();
        }
        
        List<int> firstFront = new List<int>();
        for (int p = 0; p < population.Count; p++)
        {
            for (int q = 0; q < population.Count; q++)
            {
                if (p == q) continue;
                
                if (Dominates(population[p], population[q]))
                {
                    dominatedSolutions[p].Add(q);
                }
                else if (Dominates(population[q], population[p]))
                {
                    dominationCount[p]++;
                }
            }
            
            if (dominationCount[p] == 0)
            {
                firstFront.Add(p);
            }
        }
        
        fronts.Add(firstFront.Select(idx => population[idx]).ToList());
        
        int currentFront = 0;
        while (fronts[currentFront].Count > 0)
        {
            List<int> nextFront = new List<int>();
            
            foreach (int pIdx in firstFront)
            {
                foreach (int qIdx in dominatedSolutions[pIdx])
                {
                    dominationCount[qIdx]--;
                    if (dominationCount[qIdx] == 0)
                    {
                        nextFront.Add(qIdx);
                    }
                }
            }
            
            if (nextFront.Count > 0)
            {
                fronts.Add(nextFront.Select(idx => population[idx]).ToList());
                firstFront = nextFront;
                currentFront++;
            }
            else
            {
                break;
            }
        }
        
        return fronts;
    }
    
    public void CalculateCrowdingDistance(List<Individual> front)
    {
        if (front.Count == 0) return;
        
        foreach (Individual ind in front)
        {
            ind.crowdingDistance = 0f;
        }
        
        var sortedByMSE = front.OrderBy(ind => ind.mse).ToList();
        sortedByMSE[0].crowdingDistance = float.MaxValue;
        sortedByMSE[sortedByMSE.Count - 1].crowdingDistance = float.MaxValue;
        
        float mseRange = sortedByMSE[sortedByMSE.Count - 1].mse - sortedByMSE[0].mse;
        if (mseRange > 0)
        {
            for (int i = 1; i < sortedByMSE.Count - 1; i++)
            {
                sortedByMSE[i].crowdingDistance += 
                    (sortedByMSE[i + 1].mse - sortedByMSE[i - 1].mse) / mseRange;
            }
        }
        
        var sortedByComplexity = front.OrderBy(ind => ind.complexity).ToList();
        sortedByComplexity[0].crowdingDistance = float.MaxValue;
        sortedByComplexity[sortedByComplexity.Count - 1].crowdingDistance = float.MaxValue;
        
        float complexityRange = sortedByComplexity[sortedByComplexity.Count - 1].complexity - 
                               sortedByComplexity[0].complexity;
        if (complexityRange > 0)
        {
            for (int i = 1; i < sortedByComplexity.Count - 1; i++)
            {
                sortedByComplexity[i].crowdingDistance += 
                    (sortedByComplexity[i + 1].complexity - sortedByComplexity[i - 1].complexity) / complexityRange;
            }
        }
    }
    
    public List<Individual> NSGAIISelection(List<Individual> population, int targetSize)
    {
        List<List<Individual>> fronts = FastNonDominatedSort(population);
        List<Individual> selected = new List<Individual>();
        
        int frontIndex = 0;
        while (frontIndex < fronts.Count && selected.Count + fronts[frontIndex].Count <= targetSize)
        {
            foreach (Individual ind in fronts[frontIndex])
            {
                selected.Add(ind);
            }
            frontIndex++;
        }
        
        if (selected.Count < targetSize && frontIndex < fronts.Count)
        {
            List<Individual> lastFront = fronts[frontIndex];
            CalculateCrowdingDistance(lastFront);
            
            var sortedByDistance = lastFront.OrderByDescending(ind => ind.crowdingDistance).ToList();
            int remaining = targetSize - selected.Count;
            
            for (int i = 0; i < remaining && i < sortedByDistance.Count; i++)
            {
                selected.Add(sortedByDistance[i]);
            }
        }
        
        return selected;
    }
}
