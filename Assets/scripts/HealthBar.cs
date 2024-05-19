using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public Slider slider;
    public Image fill;

    private Camera mainCamera;
    void Start()
    {
        mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward, mainCamera.transform.rotation * Vector3.up);
    }

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
