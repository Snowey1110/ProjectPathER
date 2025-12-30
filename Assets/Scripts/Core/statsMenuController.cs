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

    private int tempATK;
    private int tempDEF;
    private int tempHPP;
    private int tempMPP;
    private float tempSPD;

    private int abilityPointSpent;

    private stats GetLocalPlayerStats()
    {
        var go = GameObject.FindWithTag("Player");
        return go != null ? go.GetComponent<stats>() : null;
    }

    public void loadStats()
    {
        var playerStats = GetLocalPlayerStats();
        if (playerStats == null) return;

        int abilityLeft = Mathf.Max(0, playerStats.AbilityPoints.Value - abilityPointSpent);

        LEVEL.text = $"Level: {playerStats.Level.Value}";
        ATK.text = $"ATK: {playerStats.Damage.Value + tempATK}";
        DEF.text = $"DEF: {playerStats.Defense.Value + tempDEF}";
        SPD.text = $"SPD: {playerStats.MoveSpeed.Value + tempSPD}";
        HPP.text = $"HP: {playerStats.MaxHP.Value + tempHPP}";
        MPP.text = $"MP: {playerStats.Mana.Value + tempMPP}";
        SKILLPOINTS.text = $"Ability Points remaining: {abilityLeft}";
    }

    public void levelStatsByOne(string statType) => ApplyPreviewUpgrade(statType, step: 1, cost: 1);
    public void levelStatsByTen(string statType) => ApplyPreviewUpgrade(statType, step: 10, cost: 10);

    private void ApplyPreviewUpgrade(string statType, int step, int cost)
    {
        var playerStats = GetLocalPlayerStats();
        if (playerStats == null) return;

        int available = playerStats.AbilityPoints.Value - abilityPointSpent;
        if (available < cost) return;

        switch (statType)
        {
            case "ATK": tempATK += step; break;
            case "DEF": tempDEF += step; break;
            case "SPD": tempSPD += step; break;
            case "HPP": tempHPP += step; break;
            case "MPP": tempMPP += step; break;
            default:
                Debug.LogError("Unexpected statType: " + statType);
                return;
        }

        abilityPointSpent += cost;
        loadStats();
    }

    public void cancelAbilityPointSpent()
    {
        tempATK = 0;
        tempDEF = 0;
        tempHPP = 0;
        tempMPP = 0;
        tempSPD = 0f;
        abilityPointSpent = 0;
        loadStats();
    }

    public void confirmAbilityPointSpent()
    {
        var playerStats = GetLocalPlayerStats();
        if (playerStats == null) return;
        if (abilityPointSpent <= 0) return;

        playerStats.SpendAbilityPointsServerRpc(
            deltaDamage: tempATK,
            deltaDefense: tempDEF,
            deltaMaxHp: tempHPP,
            deltaMana: tempMPP,
            deltaMoveSpeed: tempSPD,
            cost: abilityPointSpent);

        tempATK = 0;
        tempDEF = 0;
        tempHPP = 0;
        tempMPP = 0;
        tempSPD = 0f;
        abilityPointSpent = 0;

        loadStats();
    }

    public void levelUp()
    {
        var playerStats = GetLocalPlayerStats();
        if (playerStats == null) return;

        playerStats.RequestLevelUpServerRpc();
        loadStats();
    }
}
