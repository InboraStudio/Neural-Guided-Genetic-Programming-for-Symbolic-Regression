using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq; // THIS IS CRITICAL!

public class RealTimeGraphSystem : MonoBehaviour
{
    [Header("Graph Settings")]
    public int maxDataPoints = 100;
    public float graphWidth = 800f;
    public float graphHeight = 400f;
    public Color backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.9f);
    
    [Header("Line Colors")]
    public Color fitnessLineColor = Color.cyan;
    public Color mseLineColor = Color.red;
    public Color complexityLineColor = Color.yellow;
    public Color predictedLineColor = Color.green;
    public Color actualLineColor = Color.white;
    
    [Header("References")]
    public AdvancedSymbolicRegressionGP gpController;
    
    private Canvas canvas;
    private GameObject fitnessGraphPanel;
    private GameObject functionGraphPanel;
    private GameObject statsPanel;
    
    private List<float> fitnessHistory;
    private List<float> mseHistory;
    private List<float> complexityHistory;
    private List<GameObject> fitnessGraphObjects;
    private List<GameObject> functionGraphObjects;
    
    private TextMeshProUGUI generationText;
    private TextMeshProUGUI fitnessText;
    private TextMeshProUGUI mseText;
    private TextMeshProUGUI expressionText;
    
    void Start()
    {
        fitnessHistory = new List<float>();
        mseHistory = new List<float>();
        complexityHistory = new List<float>();
        fitnessGraphObjects = new List<GameObject>();
        functionGraphObjects = new List<GameObject>();
        
        CreateGraphUI();
        InvokeRepeating("UpdateGraphs", 0.1f, 0.1f);
    }
    
    void CreateGraphUI()
    {
        // Create main canvas
        GameObject canvasObj = new GameObject("Graph Canvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Create fitness evolution graph (top left)
        CreateFitnessGraph();
        
        // Create function comparison graph (top right)
        CreateFunctionGraph();
        
        // Create stats panel (bottom)
        CreateStatsPanel();
    }
    
    void CreateFitnessGraph()
    {
        fitnessGraphPanel = new GameObject("Fitness Graph Panel");
        fitnessGraphPanel.transform.SetParent(canvas.transform, false);
        
        RectTransform rect = fitnessGraphPanel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.02f, 0.5f);
        rect.anchorMax = new Vector2(0.48f, 0.98f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        Image bg = fitnessGraphPanel.AddComponent<Image>();
        bg.color = backgroundColor;
        
        // Add title
        GameObject title = new GameObject("Title");
        title.transform.SetParent(fitnessGraphPanel.transform, false);
        
        RectTransform titleRect = title.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.sizeDelta = new Vector2(0, 40);
        titleRect.anchoredPosition = new Vector2(0, 0);
        
        TextMeshProUGUI titleText = title.AddComponent<TextMeshProUGUI>();
        titleText.text = "EVOLUTION PROGRESS";
        titleText.fontSize = 24;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.cyan;
        titleText.fontStyle = FontStyles.Bold;
        
        // Create legend
        CreateLegend(fitnessGraphPanel);
        
        // Create grid
        CreateGrid(fitnessGraphPanel, 10, 5);
    }
    
    void CreateFunctionGraph()
    {
        functionGraphPanel = new GameObject("Function Graph Panel");
        functionGraphPanel.transform.SetParent(canvas.transform, false);
        
        RectTransform rect = functionGraphPanel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.52f, 0.5f);
        rect.anchorMax = new Vector2(0.98f, 0.98f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        Image bg = functionGraphPanel.AddComponent<Image>();
        bg.color = backgroundColor;
        
        // Add title
        GameObject title = new GameObject("Title");
        title.transform.SetParent(functionGraphPanel.transform, false);
        
        RectTransform titleRect = title.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.sizeDelta = new Vector2(0, 40);
        titleRect.anchoredPosition = new Vector2(0, 0);
        
        TextMeshProUGUI titleText = title.AddComponent<TextMeshProUGUI>();
        titleText.text = "FUNCTION FIT";
        titleText.fontSize = 24;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.green;
        titleText.fontStyle = FontStyles.Bold;
        
        CreateGrid(functionGraphPanel, 10, 10);
    }
    
    void CreateStatsPanel()
    {
        statsPanel = new GameObject("Stats Panel");
        statsPanel.transform.SetParent(canvas.transform, false);
        
        RectTransform rect = statsPanel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.02f, 0.02f);
        rect.anchorMax = new Vector2(0.98f, 0.48f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        Image bg = statsPanel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.1f, 0.95f);
        
        // Create stats text displays
        CreateStatDisplay("Generation", ref generationText, 0);
        CreateStatDisplay("Fitness", ref fitnessText, 1);
        CreateStatDisplay("MSE", ref mseText, 2);
        CreateStatDisplay("Expression", ref expressionText, 3);
    }
    
    void CreateStatDisplay(string label, ref TextMeshProUGUI textComponent, int index)
    {
        GameObject statObj = new GameObject(label + " Display");
        statObj.transform.SetParent(statsPanel.transform, false);
        
        RectTransform rect = statObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.05f, 0.8f - (index * 0.2f));
        rect.anchorMax = new Vector2(0.95f, 0.95f - (index * 0.2f));
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        textComponent = statObj.AddComponent<TextMeshProUGUI>();
        textComponent.text = $"<color=cyan>{label}:</color> ---";
        textComponent.fontSize = index == 3 ? 20 : 28;
        textComponent.alignment = TextAlignmentOptions.Left;
        textComponent.color = Color.white;
        textComponent.fontStyle = FontStyles.Bold;
    }
    
    void CreateLegend(GameObject parent)
    {
        GameObject legend = new GameObject("Legend");
        legend.transform.SetParent(parent.transform, false);
        
        RectTransform rect = legend.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.05f, 0.85f);
        rect.anchorMax = new Vector2(0.95f, 0.95f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        string[] labels = { "FITNESS", "MSE", "COMPLEXITY" };
        Color[] colors = { fitnessLineColor, mseLineColor, complexityLineColor };
        
        for (int i = 0; i < labels.Length; i++)
        {
            CreateLegendItem(legend, labels[i], colors[i], i);
        }
    }
    
    void CreateLegendItem(GameObject parent, string label, Color color, int index)
    {
        GameObject item = new GameObject("Legend_" + label);
        item.transform.SetParent(parent.transform, false);
        
        RectTransform rect = item.AddComponent<RectTransform>();
        float xPos = 0.1f + (index * 0.3f);
        rect.anchorMin = new Vector2(xPos, 0);
        rect.anchorMax = new Vector2(xPos + 0.25f, 1);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        // Color box
        GameObject colorBox = new GameObject("ColorBox");
        colorBox.transform.SetParent(item.transform, false);
        
        RectTransform boxRect = colorBox.AddComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0, 0.3f);
        boxRect.anchorMax = new Vector2(0.2f, 0.7f);
        boxRect.offsetMin = Vector2.zero;
        boxRect.offsetMax = Vector2.zero;
        
        Image boxImg = colorBox.AddComponent<Image>();
        boxImg.color = color;
        
        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(item.transform, false);
        
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.25f, 0);
        labelRect.anchorMax = new Vector2(1, 1);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        
        TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 14;
        labelText.alignment = TextAlignmentOptions.Left;
        labelText.color = color;
    }
    
    void CreateGrid(GameObject parent, int horizontalLines, int verticalLines)
    {
        GameObject grid = new GameObject("Grid");
        grid.transform.SetParent(parent.transform, false);
        
        RectTransform gridRect = grid.AddComponent<RectTransform>();
        gridRect.anchorMin = new Vector2(0.05f, 0.1f);
        gridRect.anchorMax = new Vector2(0.95f, 0.8f);
        gridRect.offsetMin = Vector2.zero;
        gridRect.offsetMax = Vector2.zero;
        
        Color gridColor = new Color(0.3f, 0.3f, 0.4f, 0.3f);
        
        // Horizontal lines
        for (int i = 0; i <= horizontalLines; i++)
        {
            float y = i / (float)horizontalLines;
            CreateGridLine(grid, new Vector2(0, y), new Vector2(1, y), gridColor);
        }
        
        // Vertical lines
        for (int i = 0; i <= verticalLines; i++)
        {
            float x = i / (float)verticalLines;
            CreateGridLine(grid, new Vector2(x, 0), new Vector2(x, 1), gridColor);
        }
    }
    
    void CreateGridLine(GameObject parent, Vector2 start, Vector2 end, Color color)
    {
        GameObject line = new GameObject("GridLine");
        line.transform.SetParent(parent.transform, false);
        
        RectTransform rect = line.AddComponent<RectTransform>();
        rect.anchorMin = start;
        rect.anchorMax = end;
        rect.sizeDelta = new Vector2(1, 1);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        Image img = line.AddComponent<Image>();
        img.color = color;
    }
    
    void UpdateGraphs()
    {
        if (gpController == null || gpController.bestIndividual == null) return;
        
        // Collect data
        Individual best = gpController.bestIndividual;
        fitnessHistory.Add(best.fitness);
        mseHistory.Add(best.mse);
        complexityHistory.Add(best.complexity);
        
        // Limit history size
        if (fitnessHistory.Count > maxDataPoints)
        {
            fitnessHistory.RemoveAt(0);
            mseHistory.RemoveAt(0);
            complexityHistory.RemoveAt(0);
        }
        
        // Update graphs
        DrawEvolutionGraph();
        DrawFunctionFit();
        UpdateStats();
    }
    
    void DrawEvolutionGraph()
    {
        // Clear old objects
        foreach (GameObject obj in fitnessGraphObjects)
        {
            Destroy(obj);
        }
        fitnessGraphObjects.Clear();
        
        if (fitnessHistory.Count < 2) return;
        
        // Get graph area
        RectTransform graphArea = fitnessGraphPanel.GetComponent<RectTransform>();
        float width = graphArea.rect.width * 0.9f;
        float height = graphArea.rect.height * 0.7f;
        
        // Draw fitness line
        DrawLine(fitnessHistory, fitnessLineColor, width, height, -100f, 0f, fitnessGraphPanel);
        
        // Draw MSE line - USE LINQ NOW
        float mseMin = mseHistory.Count > 0 ? mseHistory.Min() : 0f;
        float mseMax = mseHistory.Count > 0 ? mseHistory.Max() : 1f;
        DrawLine(mseHistory, mseLineColor, width, height, mseMin, mseMax, fitnessGraphPanel);
        
        // Draw complexity line (normalized) - USE LINQ NOW
        List<float> normalizedComplexity = new List<float>();
        float maxComp = complexityHistory.Count > 0 ? complexityHistory.Max() : 1f;
        foreach (float c in complexityHistory)
        {
            normalizedComplexity.Add(c / maxComp * 10f);
        }
        DrawLine(normalizedComplexity, complexityLineColor, width, height, 0f, 10f, fitnessGraphPanel);
    }
    
    void DrawLine(List<float> data, Color color, float width, float height, float minValue, float maxValue, GameObject parent)
    {
        if (data.Count < 2) return;
        
        float range = maxValue - minValue;
        if (range < 0.0001f) range = 1f;
        
        for (int i = 0; i < data.Count - 1; i++)
        {
            float x1 = (i / (float)(data.Count - 1)) * width;
            float y1 = ((data[i] - minValue) / range) * height;
            
            float x2 = ((i + 1) / (float)(data.Count - 1)) * width;
            float y2 = ((data[i + 1] - minValue) / range) * height;
            
            GameObject lineObj = CreateLineSegment(
                new Vector2(x1, y1), 
                new Vector2(x2, y2), 
                color, 
                3f, 
                parent
            );
            
            fitnessGraphObjects.Add(lineObj);
        }
        
        // Draw dots
        for (int i = 0; i < data.Count; i++)
        {
            float x = (i / (float)(data.Count - 1)) * width;
            float y = ((data[i] - minValue) / range) * height;
            
            GameObject dot = CreateDot(new Vector2(x, y), color, 6f, parent);
            fitnessGraphObjects.Add(dot);
        }
    }
    
    void DrawFunctionFit()
    {
        // Clear old objects
        foreach (GameObject obj in functionGraphObjects)
        {
            Destroy(obj);
        }
        functionGraphObjects.Clear();
        
        if (gpController == null || gpController.inputData == null || gpController.inputData.Length == 0)
            return;
        
        RectTransform graphArea = functionGraphPanel.GetComponent<RectTransform>();
        float width = graphArea.rect.width * 0.9f;
        float height = graphArea.rect.height * 0.7f;
        
        float[] inputs = gpController.inputData;
        float[] outputs = gpController.outputData;
        
        // USE LINQ for arrays
        float minX = inputs.Min();
        float maxX = inputs.Max();
        float minY = outputs.Min();
        float maxY = outputs.Max();
        
        float rangeX = maxX - minX;
        float rangeY = maxY - minY;
        
        if (rangeX < 0.0001f) rangeX = 1f;
        if (rangeY < 0.0001f) rangeY = 1f;
        
        // Draw actual data points
        for (int i = 0; i < inputs.Length; i++)
        {
            float x = ((inputs[i] - minX) / rangeX) * width;
            float y = ((outputs[i] - minY) / rangeY) * height;
            
            GameObject dot = CreateDot(new Vector2(x, y), actualLineColor, 10f, functionGraphPanel);
            functionGraphObjects.Add(dot);
        }
        
        // Draw predicted function
        if (gpController.bestIndividual != null)
        {
            int samples = 100;
            for (int i = 0; i < samples - 1; i++)
            {
                float t1 = i / (float)(samples - 1);
                float t2 = (i + 1) / (float)(samples - 1);
                
                float x1Input = Mathf.Lerp(minX, maxX, t1);
                float x2Input = Mathf.Lerp(minX, maxX, t2);
                
                float y1Predicted = gpController.bestIndividual.root.Evaluate(x1Input);
                float y2Predicted = gpController.bestIndividual.root.Evaluate(x2Input);
                
                float x1 = t1 * width;
                float y1 = ((y1Predicted - minY) / rangeY) * height;
                
                float x2 = t2 * width;
                float y2 = ((y2Predicted - minY) / rangeY) * height;
                
                GameObject lineObj = CreateLineSegment(
                    new Vector2(x1, y1), 
                    new Vector2(x2, y2), 
                    predictedLineColor, 
                    2f, 
                    functionGraphPanel
                );
                
                functionGraphObjects.Add(lineObj);
            }
        }
    }
    
    GameObject CreateLineSegment(Vector2 start, Vector2 end, Color color, float thickness, GameObject parent)
    {
        GameObject line = new GameObject("Line");
        line.transform.SetParent(parent.transform, false);
        
        RectTransform rect = line.AddComponent<RectTransform>();
        
        Vector2 dir = (end - start).normalized;
        float distance = Vector2.Distance(start, end);
        
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(0, 0);
        rect.pivot = new Vector2(0, 0.5f);
        rect.sizeDelta = new Vector2(distance, thickness);
        
        float graphWidth = parent.GetComponent<RectTransform>().rect.width;
        float graphHeight = parent.GetComponent<RectTransform>().rect.height;
        
        rect.anchoredPosition = new Vector2(
            start.x - graphWidth * 0.45f, 
            start.y - graphHeight * 0.45f
        );
        
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rect.localRotation = Quaternion.Euler(0, 0, angle);
        
        Image img = line.AddComponent<Image>();
        img.color = color;
        
        return line;
    }
    
    GameObject CreateDot(Vector2 position, Color color, float size, GameObject parent)
    {
        GameObject dot = new GameObject("Dot");
        dot.transform.SetParent(parent.transform, false);
        
        RectTransform rect = dot.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(0, 0);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(size, size);
        
        float graphWidth = parent.GetComponent<RectTransform>().rect.width;
        float graphHeight = parent.GetComponent<RectTransform>().rect.height;
        
        rect.anchoredPosition = new Vector2(
            position.x - graphWidth * 0.45f, 
            position.y - graphHeight * 0.45f
        );
        
        Image img = dot.AddComponent<Image>();
        img.color = color;
        
        // Make it circular
        img.sprite = CreateCircleSprite();
        
        return dot;
    }
    
    Sprite CreateCircleSprite()
    {
        Texture2D tex = new Texture2D(32, 32);
        Color[] pixels = new Color[32 * 32];
        
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float dx = x - 16f;
                float dy = y - 16f;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                
                pixels[y * 32 + x] = distance < 15f ? Color.white : Color.clear;
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        
        return Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
    }
    
    void UpdateStats()
    {
        if (gpController == null || gpController.bestIndividual == null) return;
        
        Individual best = gpController.bestIndividual;
        
        int currentGen = fitnessHistory.Count;
        
        generationText.text = $"<color=cyan>GENERATION:</color> <color=white>{currentGen:D5}</color>";
        fitnessText.text = $"<color=cyan>FITNESS:</color> <color=lime>{best.fitness:F6}</color>";
        mseText.text = $"<color=cyan>MSE:</color> <color=red>{best.mse:F8}</color>";
        expressionText.text = $"<color=cyan>EXPRESSION:</color> <color=yellow>{best.root}</color>";
        
        // Add glow effect when fitness improves
        if (fitnessHistory.Count > 1 && best.fitness > fitnessHistory[fitnessHistory.Count - 2])
        {
            fitnessText.color = Color.Lerp(Color.white, Color.green, Mathf.PingPong(Time.time * 5f, 1f));
        }
    }
}
