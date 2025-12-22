using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Slider slider;
    public Image fill;

    private stats boundStats;

    // Called by stats.OnNetworkSpawn
    public void Bind(stats s)
    {
        boundStats = s;
        if (boundStats == null) return;

        // Initial paint (safe even if net vars not ready yet)
        SetMaxHealth(boundStats.MaxHP.Value, boundStats.CurrentHP.Value);
    }

    public void SetMaxHealth(int health, int currentHealth)
    {
        if (slider == null) return;
        slider.maxValue = Mathf.Max(1, health);
        slider.value = Mathf.Clamp(currentHealth, 0, slider.maxValue);
    }

    public void SetHealth(int health)
    {
        if (slider == null) return;

        if (health == -1)
            slider.value = slider.maxValue;
        else
            slider.value = Mathf.Clamp(health, 0, slider.maxValue);
    }
}
