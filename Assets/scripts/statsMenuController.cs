using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class statsMenuController : MonoBehaviour
{
    public TextMeshProUGUI LEVEL;
    public TextMeshProUGUI ATK;
    public TextMeshProUGUI DEF;
    public TextMeshProUGUI SPD;
    public TextMeshProUGUI HPP;
    public TextMeshProUGUI MPP;
    public TextMeshProUGUI SKILLPOINTS;
    // Start is called before the first frame update
    private void OnEnable()
    {
        if (GameObject.FindWithTag("Player") != null)
        {
            loadStats();
        }
        else
        {
            Debug.LogWarning("StatsMenuController reference not set!");
        }
    }

    public void loadStats()
    {
        LEVEL.text = "Level: " + GameObject.FindWithTag("Player").GetComponent<stats>().level;
        ATK.text = "ATK: " + GameObject.FindWithTag("Player").GetComponent<stats>().baseDamage;
        DEF.text = "DEF: " + GameObject.FindWithTag("Player").GetComponent<stats>().defense;
        SPD.text = "SPD: " + GameObject.FindWithTag("Player").GetComponent<Player>().speed;
        HPP.text = "HP: " + GameObject.FindWithTag("Player").GetComponent<stats>().HP;
        MPP.text = "MP: " + GameObject.FindWithTag("Player").GetComponent<stats>().mana;
        SKILLPOINTS.text = "Ability Points remaining: : " + GameObject.FindWithTag("Player").GetComponent<stats>().abilityPoints;
    }
}
