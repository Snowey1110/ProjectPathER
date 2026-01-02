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

    // These are the deltas/points the player is previewing.
    // How they apply is defined by the player's StatsDefinition spend rules.
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

    private static StatsDefinition GetDef(stats st) => st != null ? st.Definition : null;

    public void loadStats()
    {
        var playerStats = GetLocalPlayerStats();
        if (playerStats == null) return;

        var def = GetDef(playerStats);

        int abilityLeft = Mathf.Max(0, playerStats.AbilityPoints.Value - abilityPointSpent);

        LEVEL.text = $"Level: {playerStats.Level.Value}";

        int baseAtk = playerStats.Damage.Value + playerStats.BonusDamage.Value;
        int shownAtk = (def != null)
            ? def.spendDamage.PreviewDelta(baseAtk, tempATK)
            : Mathf.Max(1, baseAtk + tempATK);
        ATK.text = $"ATK: {shownAtk}";

        int shownDef = (def != null)
            ? def.spendDefense.PreviewDelta(playerStats.Defense.Value, tempDEF)
            : Mathf.Max(0, playerStats.Defense.Value + tempDEF);
        DEF.text = $"DEF: {shownDef}";

        float shownSpd = (def != null)
            ? def.spendMoveSpeed.PreviewDelta(playerStats.MoveSpeed.Value, tempSPD)
            : Mathf.Max(0.01f, playerStats.MoveSpeed.Value + tempSPD);
        SPD.text = $"SPD: {shownSpd}";

        int shownHp = (def != null)
            ? def.spendMaxHp.PreviewDelta(playerStats.MaxHP.Value, tempHPP)
            : Mathf.Max(1, playerStats.MaxHP.Value + tempHPP);
        HPP.text = $"HP: {shownHp}";

        int shownMana = (def != null)
            ? def.spendMana.PreviewDelta(playerStats.Mana.Value, tempMPP)
            : Mathf.Max(0, playerStats.Mana.Value + tempMPP);
        MPP.text = $"MP: {shownMana}";
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
            case "ATK":
                tempATK += step;
                break;
            case "DEF":
                tempDEF += step;
                break;
            case "SPD":
                tempSPD += step;
                break;
            case "HPP":
                tempHPP += step;
                break;
            case "MPP":
                tempMPP += step;
                break;
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

        // Server interprets these based on the StatsDefinition spend rules.
        int sendDeltaDamage = tempATK;
        int sendDeltaMaxHp = tempHPP;

        playerStats.SpendAbilityPointsServerRpc(
            deltaDamage: sendDeltaDamage,
            deltaDefense: tempDEF,
            deltaMaxHp: sendDeltaMaxHp,
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
