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

    //Temp stats after leveling
    private int abilityPointSpent;
    private int tempATK;
    private int tempDEF;
    private int tempSPD;
    private int tempHPP;
    private int tempMPP;

    //Reset Button
    public GameObject resetButton;
    public GameObject confirmButton;


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
        if (GameObject.FindWithTag("Player") != null)
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

    public void levelStatsByOne(string statType)
    {
        var playerStats = GameObject.FindWithTag("Player")?.GetComponent<stats>();
        var playerMovements = GameObject.FindWithTag("Player")?.GetComponent<Player>();

        if ((playerStats == null) || (playerMovements ==null))
        {
            Debug.LogError("Cannot find the Player's stats component");
            return;
        }

        if (playerStats.abilityPoints >= 1)
        {
            switch (statType)
            {
                case "ATK":
                    tempATK += 1;
                    playerStats.baseDamage += 1;
                    break;
                case "DEF":
                    tempDEF += 1;
                    playerStats.defense += 1;
                    break;
                case "SPD":
                    tempSPD += 1;
                    playerMovements.speed += 1;
                    break;
                case "HPP":
                    tempHPP += 1;
                    playerStats.HP += 1;
                    playerStats.healthBar.SetMaxHealth(playerStats.HP, playerStats.currentHealth);
                    break;
                case "MPP":
                    tempMPP += 1;
                    playerStats.mana += 1;
                    break;
                default:
                    Debug.LogError("Invalid stat type: " + statType);
                    return;
            }
            playerStats.abilityPoints -= 1;
            abilityPointSpent += 1;
            loadStats();
        }
    }

    public void levelStatsByTen(string statType)
    {
        var playerStats = GameObject.FindWithTag("Player")?.GetComponent<stats>();
        var playerMovements = GameObject.FindWithTag("Player")?.GetComponent<Player>();

        if (playerStats == null)
        {
            Debug.LogError("Cannot find the Player's stats component");
            return;
        }

        if (playerStats.abilityPoints >= 10)
        {
            switch (statType)
            {
                case "ATK":
                    tempATK += 10;
                    playerStats.baseDamage += 10;
                    break;
                case "DEF":
                    tempDEF += 10;
                    playerStats.defense += 10;
                    break;
                case "SPD":
                    tempSPD += 10;
                    playerMovements.speed += 10;
                    break;
                case "HPP":
                    tempHPP += 10;
                    playerStats.HP += 10;
                    playerStats.healthBar.SetMaxHealth(playerStats.HP, playerStats.currentHealth);
                    break;
                case "MPP":
                    tempMPP += 10;
                    playerStats.mana += 10;
                    break;
                default:
                    Debug.LogError("Invalid stat type: " + statType);
                    return;
            }
            abilityPointSpent += 10;
            playerStats.abilityPoints -= 10;
            loadStats();
        }
    }

    //Reset Ability Points after spending
    public void resetAbilityPoints()
    {
        var playerStats = GameObject.FindWithTag("Player")?.GetComponent<stats>();
        var playerMovements = GameObject.FindWithTag("Player")?.GetComponent<Player>();
        playerStats.abilityPoints += abilityPointSpent;
        abilityPointSpent = 0;
        playerStats.baseDamage -= tempATK;
        tempATK = 0;
        playerStats.HP -= tempHPP;
        tempHPP = 0;
        playerStats.healthBar.SetMaxHealth(playerStats.HP, playerStats.currentHealth);
        playerStats.mana -= tempMPP;
        tempMPP = 0;
        playerStats.defense -= tempDEF;
        tempDEF = 0;
        playerMovements.speed -= tempSPD;
        tempSPD = 0;
        loadStats();
    }

    //Confirm ability points spent
    public void confirmAbilityPointSpent()
    {
        tempATK = 0;
        tempHPP = 0;
        tempMPP = 0;
        tempDEF = 0;
        tempSPD = 0;
        abilityPointSpent = 0;
        loadStats();
    }

    public void levelUp()
    {
        var playerStats = GameObject.FindWithTag("Player")?.GetComponent<stats>();
        if (playerStats == null)
        {
            Debug.LogError("Cannot find the Player's stats component");
            return;
        }


        //Randomly adds stats temp to the player
        int randomStat = Random.Range(0, 5);

        switch (randomStat)
        {
            case 0:
                tempATK += 1;
                playerStats.baseDamage += 1;
                break;
            case 1:
                tempDEF += 1;
                playerStats.defense += 1;
                break;
            case 2:
                tempSPD += 1;
                var playerMovements = GameObject.FindWithTag("Player")?.GetComponent<Player>();
                if (playerMovements != null)
                {
                    playerMovements.speed += 1;
                }
                break;
            case 3:
                tempHPP += 1;
                playerStats.HP += 1;
                playerStats.healthBar.SetMaxHealth(playerStats.HP, playerStats.currentHealth);
                break;
            case 4:
                tempMPP += 1;
                playerStats.mana += 1;
                break;
            default:
                Debug.LogError("Unexpected randomStat: " + randomStat);
                return;
        }
        //Regen player to full HP
        playerStats.currentHealth = playerStats.HP; 
        playerStats.healthBar.SetHealth(-1); 

        playerStats.abilityPoints += 5; //Give 5 ability points on leveling
        playerStats.level += 1; //Levels the player up
        loadStats();
    }

}
