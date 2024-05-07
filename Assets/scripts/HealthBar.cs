using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Slider slider;
    public Image fill;

    public void SetMaxHealth(int health, int currentHealth)
    {
        slider.maxValue = health;
        slider.value = currentHealth;

    }

    public void SetHealth(int health)
    {
        slider.value = health;
        if (health == -1)
        {
            slider.value = slider.maxValue;
        }
    }

    

}
