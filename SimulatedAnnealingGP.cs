using UnityEngine;

public class SimulatedAnnealingGP
{
    public float initialTemperature = 1.0f;
    public float coolingRate = 0.995f;
    public float currentTemperature;
    
    public void Initialize()
    {
        currentTemperature = initialTemperature;
    }
    
    public void CoolDown()
    {
        currentTemperature *= coolingRate;
    }
    
    public bool AcceptSolution(float oldFitness, float newFitness, System.Random random)
    {
        if (newFitness > oldFitness)
            return true;
        
        float deltaFitness = newFitness - oldFitness;
        float acceptanceProbability = Mathf.Exp(deltaFitness / currentTemperature);
        
        return random.NextDouble() < acceptanceProbability;
    }
}
