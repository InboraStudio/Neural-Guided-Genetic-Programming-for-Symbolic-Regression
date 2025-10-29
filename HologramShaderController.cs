using UnityEngine;

public class HologramShaderController : MonoBehaviour
{
    [Header("Hologram Settings")]
    public Material hologramMaterial;
    public float scanlineSpeed = 2f;
    public float glowIntensity = 2f;
    public Color hologramColor = new Color(0, 1, 1, 0.5f);
    
    [Header("Animation")]
    public bool animateGlow = true;
    public float glowPulseSpeed = 1f;
    public float minGlow = 1f;
    public float maxGlow = 3f;
    
    private float currentGlow;
    
    void Update()
    {
        if (hologramMaterial == null) return;
        
        hologramMaterial.SetFloat("_ScanlineSpeed", scanlineSpeed);
        hologramMaterial.SetColor("_HologramColor", hologramColor);
        
        if (animateGlow)
        {
            currentGlow = Mathf.Lerp(minGlow, maxGlow, 
                         (Mathf.Sin(Time.time * glowPulseSpeed) + 1f) * 0.5f);
            hologramMaterial.SetFloat("_GlowIntensity", currentGlow);
        }
        else
        {
            hologramMaterial.SetFloat("_GlowIntensity", glowIntensity);
        }
    }
    
    public void TriggerDataPulse()
    {
        if (hologramMaterial != null)
        {
            StartCoroutine(PulseEffect());
        }
    }
    
    System.Collections.IEnumerator PulseEffect()
    {
        float duration = 0.5f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float intensity = Mathf.Lerp(maxGlow * 2f, glowIntensity, elapsed / duration);
            hologramMaterial.SetFloat("_GlowIntensity", intensity);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}
