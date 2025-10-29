using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GPVisualizationManager : MonoBehaviour
{
    [Header("UI References")]
    public Canvas mainCanvas;
    public TextMeshProUGUI generationText;
    public TextMeshProUGUI bestFitnessText;
    public TextMeshProUGUI bestExpressionText;
    public TextMeshProUGUI populationStatsText;
    
    [Header("Graph Visualization")]
    public RectTransform graphContainer;
    public GameObject dotPrefab;
    public Color graphLineColor = Color.cyan;
    public float graphUpdateInterval = 0.1f;
    
    [Header("Particle Effects")]
    public ParticleSystem evolutionParticles;
    public ParticleSystem bestFoundParticles;
    public ParticleSystem migrationParticles;
    
    [Header("Expression Tree Visualization")]
    public RectTransform treeContainer;
    public GameObject treeNodePrefab;
    public float nodeSpacing = 80f;
    public float levelSpacing = 100f;
    
    private List<float> fitnessHistory;
    private List<float> mseHistory;
    private List<GameObject> graphDots;
    private float lastGraphUpdate;
    
    private List<GameObject> treeNodes;
    
    void Start()
    {
        fitnessHistory = new List<float>();
        mseHistory = new List<float>();
        graphDots = new List<GameObject>();
        treeNodes = new List<GameObject>();
        
        SetupUI();
    }
    
    void SetupUI()
    {
        if (mainCanvas == null)
        {
            GameObject canvasObj = new GameObject("GP Visualization Canvas");
            mainCanvas = canvasObj.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        CreateHolographicBackground();
    }
    
    void CreateHolographicBackground()
    {
        GameObject bgPanel = new GameObject("Holographic Background");
        bgPanel.transform.SetParent(mainCanvas.transform, false);
        
        RectTransform rect = bgPanel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        
        Image img = bgPanel.AddComponent<Image>();
        img.color = new Color(0, 0.1f, 0.2f, 0.3f);
    }
    
    public void UpdateVisualization(int generation, Individual best, List<Island> islands)
    {
        if (generationText != null)
            generationText.text = $"<color=#00ffff>GENERATION:</color> {generation:D5}";
        
        if (bestFitnessText != null)
            bestFitnessText.text = $"<color=#00ff00>FITNESS:</color> {best.fitness:F6}";
        
        if (bestExpressionText != null)
            bestExpressionText.text = $"<color=#ffff00>f(x) = {best.root}</color>";
        
        fitnessHistory.Add(best.fitness);
        mseHistory.Add(best.mse);
        
        // Calculate population statistics
        float avgComplexity = 0f;
        float avgFitness = 0f;
        int totalPop = 0;
        
        foreach (Island island in islands)
        {
            // Use foreach loop instead of Sum() to avoid LINQ issues
            foreach (Individual ind in island.population)
            {
                avgComplexity += ind.complexity;
                avgFitness += ind.fitness;
                totalPop++;
            }
        }
        
        if (totalPop > 0)
        {
            avgComplexity /= totalPop;
            avgFitness /= totalPop;
        }
        
        if (populationStatsText != null)
        {
            populationStatsText.text = $"<color=#ff00ff>POPULATION:</color> {totalPop}\n" +
                                       $"<color=#ff00ff>AVG COMPLEXITY:</color> {avgComplexity:F2}\n" +
                                       $"<color=#ff00ff>AVG FITNESS:</color> {avgFitness:F4}\n" +
                                       $"<color=#ff00ff>MSE:</color> {best.mse:F6}";
        }
        
        if (Time.time - lastGraphUpdate > graphUpdateInterval)
        {
            UpdateFitnessGraph();
            lastGraphUpdate = Time.time;
        }
        
        EmitEvolutionParticles(best.fitness);
    }
    
    void UpdateFitnessGraph()
    {
        if (graphContainer == null || dotPrefab == null) return;
        
        foreach (GameObject dot in graphDots)
        {
            Destroy(dot);
        }
        graphDots.Clear();
        
        if (fitnessHistory.Count < 2) return;
        
        float width = graphContainer.rect.width;
        float height = graphContainer.rect.height;
        
        float minFitness = fitnessHistory.Min();
        float maxFitness = fitnessHistory.Max();
        float range = maxFitness - minFitness;
        
        if (range < 0.001f) range = 1f;
        
        for (int i = 0; i < fitnessHistory.Count; i++)
        {
            float xPos = (i / (float)fitnessHistory.Count) * width;
            float yPos = ((fitnessHistory[i] - minFitness) / range) * height;
            
            GameObject dot = Instantiate(dotPrefab, graphContainer);
            RectTransform dotRect = dot.GetComponent<RectTransform>();
            dotRect.anchoredPosition = new Vector2(xPos, yPos);
            
            Image dotImage = dot.GetComponent<Image>();
            if (dotImage != null)
            {
                dotImage.color = Color.Lerp(Color.red, Color.green, 
                                           (fitnessHistory[i] - minFitness) / range);
            }
            
            graphDots.Add(dot);
            
            if (i > 0)
            {
                DrawLine(graphDots[i - 1].GetComponent<RectTransform>().anchoredPosition,
                        dotRect.anchoredPosition, graphLineColor);
            }
        }
    }
    
    void DrawLine(Vector2 start, Vector2 end, Color color)
    {
        GameObject line = new GameObject("GraphLine");
        line.transform.SetParent(graphContainer, false);
        
        Image lineImg = line.AddComponent<Image>();
        lineImg.color = color;
        
        RectTransform rect = line.GetComponent<RectTransform>();
        Vector2 dir = (end - start).normalized;
        float distance = Vector2.Distance(start, end);
        
        rect.sizeDelta = new Vector2(distance, 2f);
        rect.anchoredPosition = start + dir * distance * 0.5f;
        rect.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        
        graphDots.Add(line);
    }
    
    void EmitEvolutionParticles(float fitness)
    {
        if (evolutionParticles != null)
        {
            var emission = evolutionParticles.emission;
            emission.rateOverTime = Mathf.Lerp(10f, 100f, Mathf.Abs(fitness) / 100f);
            
            var main = evolutionParticles.main;
            main.startColor = Color.Lerp(Color.red, Color.cyan, 
                                        Mathf.InverseLerp(-100f, 0f, fitness));
        }
    }
    
    public void ShowBestFoundEffect(Vector3 position)
    {
        if (bestFoundParticles != null)
        {
            bestFoundParticles.transform.position = position;
            bestFoundParticles.Play();
        }
    }
    
    public void ShowMigrationEffect(int fromIsland, int toIsland)
    {
        if (migrationParticles != null)
        {
            migrationParticles.Play();
        }
    }
    
    public void VisualizeExpressionTree(ExpressionNode root)
    {
        if (treeContainer == null || treeNodePrefab == null) return;
        
        foreach (GameObject node in treeNodes)
        {
            Destroy(node);
        }
        treeNodes.Clear();
        
        if (root == null) return;
        
        Dictionary<ExpressionNode, Vector2> positions = new Dictionary<ExpressionNode, Vector2>();
        CalculateTreePositions(root, positions, 0, 0, 1000f);
        
        foreach (var kvp in positions)
        {
            GameObject nodeObj = Instantiate(treeNodePrefab, treeContainer);
            RectTransform rect = nodeObj.GetComponent<RectTransform>();
            rect.anchoredPosition = kvp.Value;
            
            TextMeshProUGUI nodeText = nodeObj.GetComponentInChildren<TextMeshProUGUI>();
            if (nodeText != null)
            {
                nodeText.text = GetNodeLabel(kvp.Key);
                nodeText.color = GetNodeColor(kvp.Key.nodeType);
            }
            
            treeNodes.Add(nodeObj);
        }
    }
    
    void CalculateTreePositions(ExpressionNode node, Dictionary<ExpressionNode, Vector2> positions,
                               float x, float y, float horizontalSpacing)
    {
        if (node == null) return;
        
        positions[node] = new Vector2(x, y);
        
        float newSpacing = horizontalSpacing / 2f;
        
        if (node.left != null)
        {
            CalculateTreePositions(node.left, positions, x - newSpacing, 
                                  y - levelSpacing, newSpacing);
        }
        
        if (node.right != null)
        {
            CalculateTreePositions(node.right, positions, x + newSpacing, 
                                  y - levelSpacing, newSpacing);
        }
    }
    
    string GetNodeLabel(ExpressionNode node)
    {
        switch (node.nodeType)
        {
            case NodeType.Variable: return "X";
            case NodeType.Constant: return node.constantValue.ToString("F2");
            case NodeType.Add: return "+";
            case NodeType.Subtract: return "-";
            case NodeType.Multiply: return "×";
            case NodeType.Divide: return "÷";
            case NodeType.Sin: return "sin";
            case NodeType.Cos: return "cos";
            case NodeType.Log: return "log";
            case NodeType.Exp: return "exp";
            case NodeType.Power: return "^";
            case NodeType.Sqrt: return "√";
            default: return "?";
        }
    }
    
    Color GetNodeColor(NodeType type)
    {
        switch (type)
        {
            case NodeType.Variable: return Color.cyan;
            case NodeType.Constant: return Color.yellow;
            case NodeType.Add:
            case NodeType.Subtract:
            case NodeType.Multiply:
            case NodeType.Divide:
                return Color.green;
            case NodeType.Sin:
            case NodeType.Cos:
            case NodeType.Log:
            case NodeType.Exp:
            case NodeType.Sqrt:
                return Color.magenta;
            default:
                return Color.white;
        }
    }
}
