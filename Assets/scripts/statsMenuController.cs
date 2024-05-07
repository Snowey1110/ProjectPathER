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

    //Bonus stats after leveling
    private int bonusATK;
    private int bonusDEF;
    private int bonusSPD;
    private int bonusHPP;
    private int bonusMPP;


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
                    playerStats.baseDamage += 1;
                    break;
                case "DEF":
                    playerStats.defense += 1;
                    break;
                case "SPD":
                    playerMovements.speed += 1;
                    break;
                case "HPP":
                    playerStats.HP += 1;
                    playerStats.healthBar.SetMaxHealth(playerStats.HP, playerStats.currentHealth);
                    break;
                case "MPP":
                    playerStats.mana += 1;
                    break;
                default:
                    Debug.LogError("Invalid stat type: " + statType);
                    return;
            }
            playerStats.abilityPoints -= 1;
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
                    playerStats.baseDamage += 10;
                    break;
                case "DEF":
                    playerStats.defense += 10;
                    break;
                case "SPD":
                    playerMovements.speed += 10;
                    break;
                case "HPP":
                    playerStats.HP += 10;
                    playerStats.healthBar.SetMaxHealth(playerStats.HP, playerStats.currentHealth);
                    break;
                case "MPP":
                    playerStats.mana += 10;
                    break;
                default:
                    Debug.LogError("Invalid stat type: " + statType);
                    return;
            }
            playerStats.abilityPoints -= 10;
            loadStats();
        }
    }

    //Default archer stats (Modify here to set default stats)
    public void setDefaultArcherStats()
    {
        var playerStats = GameObject.FindWithTag("Player")?.GetComponent<stats>();
        var playerMovements = GameObject.FindWithTag("Player")?.GetComponent<Player>();
        playerStats.totalAbilityPoints = 11;
        playerStats.baseDamage = 1 + bonusATK;
        playerStats.HP = 10 + bonusHPP;
        playerStats.healthBar.SetMaxHealth(playerStats.HP, playerStats.currentHealth);
        playerStats.mana = 1 + bonusMPP;
        playerStats.defense = 1 + bonusDEF;
        playerMovements.speed = 4 + bonusSPD;
        playerStats.abilityPoints = playerStats.totalAbilityPoints;
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


        //Randomly adds stats bonus to the player
        int randomStat = Random.Range(0, 5);

        switch (randomStat)
        {
            case 0:
                bonusATK += 1;
                playerStats.baseDamage += 1;
                break;
            case 1:
                bonusDEF += 1;
                playerStats.defense += 1;
                break;
            case 2:
                bonusSPD += 1;
                var playerMovements = GameObject.FindWithTag("Player")?.GetComponent<Player>();
                if (playerMovements != null)
                {
                    playerMovements.speed += 1;
                }
                break;
            case 3:
                bonusHPP += 1;
                playerStats.HP += 1;
                playerStats.healthBar.SetMaxHealth(playerStats.HP, playerStats.currentHealth);
                break;
            case 4:
                bonusMPP += 1;
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
