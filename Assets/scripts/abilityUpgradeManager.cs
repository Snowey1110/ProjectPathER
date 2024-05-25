using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class abilityUpgradeManager : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public Button abilityButton;
    public Slider progressSlider;
    public Image fillImage;
    public int requiredSkillPoints = 1;
    public TooltipTrigger tooltipTrigger;

    private bool isHolding = false;
    private float elapsed = 0;
    private float duration = 2;

    private void FixedUpdate()
    {
        if (isHolding && !progressSlider.gameObject.activeSelf)
        {
            
            AttemptUpgrade();
        }

        if (progressSlider.gameObject.activeSelf)
        {
            elapsed += Time.deltaTime;
            progressSlider.value = elapsed / duration;

            if (elapsed >= duration)
            {
                DoUpgrade();
            }
            else if (!isHolding)
            {
                progressSlider.gameObject.SetActive(false);
                elapsed = 0;
            }
        }
    }

    private void AttemptUpgrade()
    {

        if (GameObject.FindWithTag("Player").GetComponent<stats>().skillPoints >= requiredSkillPoints)
        {
            progressSlider.gameObject.SetActive(true);
        }
        else
        {
            //fillImage.color = Color.red;
            //progressSlider.gameObject.SetActive(true);
        }
    }

    private void DoUpgrade()
    {
        GameObject.FindWithTag("Player").GetComponent<stats>().skillPoints -= requiredSkillPoints;
        GameObject.FindWithTag("Player").GetComponent<ArcherAbilities>().dashLvl += 1;
        TooltipManager.Hide();
        progressSlider.gameObject.SetActive(false);
        elapsed = 0;
        fillImage.color = Color.white;
    }

    public void OnPointerDown(PointerEventData data)
    {
        isHolding = true;
    }

    public void OnPointerUp(PointerEventData data)
    {
        isHolding = false;
    }
}