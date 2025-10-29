# Neural-Guided Genetic Programming for Symbolic Regression

A Unity bsed implementation of advanced evolutionary algorithms for discovering mathematical expressions from data, enhanced with neural network guidance and multiobjective optimization
## Mathematical Foundation

### Expression Representation

Expressions are represented as binary trees where:

- **Internal nodes**: Mathematical operators ∈ {+, −, ×, ÷, sin, cos, log, exp, ^, √}
- **Leaf nodes**: Variables (x) or constants (c ∈ ℝ)

Example: f(x) = x² + 1 → Tree(+, Tree(^, x, 2), 1)

### Fitness Function

The fitness φ of an individual is computed as:
```js
φ = -(MSE + λ·C)
```


where:
- MSE = (1/n)Σᵢ₌₁ⁿ (yᵢ - ŷᵢ)² is the mean squared error
- C = number of nodes in expression tree (complexity measure)
- λ ∈ ℝ⁺ is the complexity penalty weight
- n is the number of data points

### Multi-Objective Optimization

The system implements NSGA-II (Non-dominated Sorting Genetic Algorithm II) to optimize two competing objectives:

| Objective | Goal | Formula |
|-----------|------|---------|
| Accuracy | Minimize prediction error | min MSE(f) |
| Simplicity | Minimize expression complexity | min \|nodes(f)\| |

Pareto dominance: f₁ ≻ f₂ iff MSE(f₁) ≤ MSE(f₂) ∧ C(f₁) ≤ C(f₂) ∧ (MSE(f₁) < MSE(f₂) ∨ C(f₁) < C(f₂))

## Algorithm Components

### Evolutionary Operators

| Operator | Type | Probability | Description |
|----------|------|-------------|-------------|
| Constant Mutation | Point | 0.25 | δc ~ 𝒰(-σ, σ), c' = c + δc |
| Subtree Mutation | Structural | 0.30 | Replace random subtree with new random tree |
| Node Deletion | Structural | 0.20 | Remove node, promote child |
| Simplification | Algebraic | 0.25 | Collapse constant-only subtrees |
| Crossover | Recombination | 0.70 | Exchange random subtrees between parents |

### Island Model Architecture

The system employs a parallel island model for population diversity:
- Islands: {I₁, I₂, I₃, I₄}
- Topology: Ring (Iᵢ → I₍ᵢ₊₁₎ mod 4)
- Migration interval: τ = 20 generations
- Migration rate: m = 3 individuals per island
  
Each island maintains different evolutionary parameters:

| Island | Population | Mutation Rate μ | Complexity Weight λ | Max Depth d |
|--------|-----------|----------------|---------------------|-------------|
| I₁ | 250 | 0.70 | 1.0 | 4 |
| I₂ | 250 | 0.75 | 1.5 | 5 |
| I₃ | 250 | 0.80 | 2.0 | 4 |
| I₄ | 250 | 0.85 | 2.5 | 5 |

### Neural Guidance System

A shallow neural network predicts expression fitness before evaluation:

Architecture: → →​​
Activation: ReLU(x) = max(0, x)
Training: Online gradient descent with η

```js
P(accept worse solution) = exp(Δφ / T)
T(t) = T₀ · αᵗ
```


where T₀ = 1.0, α = 0.995 (cooling rate)

## Performance Characteristics

### Computational Complexity

| Operation | Time Complexity | Space Complexity |
|-----------|----------------|------------------|
| Tree evaluation | O(n·m) | O(d) |
| Population evolution | O(p·n·m) | O(p·d) |
| NSGA-II sorting | O(p²·k) | O(p) |
| Neural training | O(e·s·h²) | O(h²) |

where:
- n = number of data points
- m = average expression size
- d = tree depth
- p = population size
- k = number of objectives (2)
- e = training epochs
- s = training set size
- h = hidden layer size

### Benchmark Results

| Function | Expression | Generations | Final MSE | Time (s) |
|----------|------------|-------------|-----------|----------|
| Linear | 2x + 1 | 15 | 1.2×10⁻⁸ | 0.8 |
| Quadratic | x² + 1 | 28 | 3.4×10⁻⁷ | 1.5 |
| Trigonometric | sin(x) | 142 | 2.1×10⁻⁴ | 7.3 |
| Rational | (x+1)/(x+2) | 89 | 1.8×10⁻⁵ | 4.2 |

## Installation

### Prerequisites

- Unity 2021.3 LTS or later
- .NET Framework 4.7.1+
- TextMeshPro package

### Setup

1. Clone the repository:
git clone https://github.com/InboraStudio/Neural-Guided-Genetic-Programming-for-Symbolic-Regression.git
cd Neural-Guided-Genetic-Programming-for-Symbolic-Regression

2. Open project in Unity

3. Install TextMeshPro:
   - Window → Package Manager → Search "TextMesh Pro" → Install

4. Import all scripts into Assets/Scripts/

## Usage

### Basic Configuration

// Create test dataset
float[] inputs = new float[] { 0, 1, 2, 3, 4, 5 };
float[] outputs = new float[] { 1, 2, 5, 10, 17, 26 }; // x² + 1

// Configure GP parameters
numberOfIslands = 4;
populationPerIsland = 250;
maxGenerations = 1000;
deathRate = 0.7f;
complexityWeight = 2.0f;

### Running Evolution

1. Attach `AdvancedSymbolicRegressionGP` to GameObject
2. Configure parameters in Inspector
3. Press Play
4. Monitor Console for evolution progress

### Visualization

Attach `RealTimeGraphSystem` for live plotting:
- Top-left: Evolution metrics (fitness, MSE, complexity)
- Top-right: Function fit comparison
- Bottom: Current best expression and statistics

## Repository Structure


## Algorithm Configuration Parameters

### Recommended Settings

| Parameter | Small Dataset (<50 pts) | Medium (50-500) | Large (>500) |
|-----------|------------------------|-----------------|--------------|
| Population/Island | 100-250 | 250-500 | 500-1000 |
| Max Generations | 100-500 | 500-1000 | 1000-5000 |
| Complexity Weight λ | 1.0-2.0 | 2.0-5.0 | 0.5-1.0 |
| Migration Interval | 10-20 | 20-50 | 50-100 |

### Parameter Sensitivity

High impact parameters (tune first):
- Population size: Linear relationship with solution quality
- Complexity weight: Balances accuracy vs. simplicity
- Death rate: Controls selection pressure

Low impact parameters (use defaults):
- Migration topology: Ring vs. Star (< 5% difference)
- Neural guidance interval: 30-100 generations
- Cooling rate: 0.99-0.999

## Theoretical Background

### References

This implementation is based on the following research:

1. **Genetic Programming**: Koza, J. R. (1992). *Genetic Programming: On the Programming of Computers by Means of Natural Selection*. MIT Press.

2. **Island Models**: Whitley, D., Rana, S., & Heckendorn, R. B. (1998). *The Island Model Genetic Algorithm: On Separability, Population Size and Convergence*. Journal of Computing and Information Technology.

3. **NSGA-II**: Deb, K., et al. (2002). *A Fast and Elitist Multiobjective Genetic Algorithm: NSGA-II*. IEEE Transactions on Evolutionary Computation, 6(2), 182-197.

4. **Symbolic Regression**: Schmidt, M., & Lipson, H. (2009). *Distilling Free-Form Natural Laws from Experimental Data*. Science, 324(5923), 81-85.

5. **Neural-Guided Evolution**: Lample, G., & Charton, F. (2019). *Deep Learning for Symbolic Mathematics*. arXiv:1912.01412.

## Applications

- **Physics**: Discovery of governing equations from experimental data
- **Engineering**: System identification and model fitting
- **Economics**: Empirical relationship modeling
- **Biology**: Gene regulatory network inference
- **Data Science**: Feature engineering and transformation discovery

## Performance Optimization

### Parallel Execution

Island model enables parallel evolution:
```cs
Parallel.ForEach(islands, island => {
island.EvolveGeneration(inputData, outputData, deathRate, eliteCount);
});
```

Expected speedup: S ≈ N_islands / (1 + communication_overhead)

### Memory Usage

Approximate memory footprint:
```cs
Memory ≈ (p × d × 4 bytes) × N_islands + (h² × 4 bytes)
≈ (1000 × 20 × 4) × 4 + (64² × 4)
≈ 336 KB
```

## Known Limitations

1. **Discontinuous Functions**: Struggles with step functions and piecewise definitions
2. **High-Frequency Oscillations**: May underfit rapidly oscillating functions
3. **Dimensional Analysis**: Does not enforce physical unit consistency
4. **Computational Scaling**: O(n·m) evaluation cost limits large datasets

## Future Enhancements

- [ ] Multi-variable support (f: ℝⁿ → ℝ)
- [ ] Dimensional analysis constraints
- [ ] GPU-accelerated tree evaluation
- [ ] Automatic operator selection based on domain
- [ ] Integration with Unity ML-Agents

## Contributing

Contributions are welcome. Please follow these guidelines:

1. Fork the repository
2. Create feature branch: `git checkout -b feature/YourFeature`
3. Maintain code documentation standards
4. Add unit tests for new functionality
5. Submit pull request with detailed description

## License

MIT License - see LICENSE file for details

## Citation

If you use this software in your research, please cite:

@software{inbora_gp_2025,
author = {Dr Chamyoung},
title = {Neural-Guided Genetic Programming for Symbolic Regression},
year = {2025},
publisher = {GitHub},
url = {https://github.com/InboraStudio/Neural-Guided-Genetic-Programming-for-Symbolic-Regression}
}
