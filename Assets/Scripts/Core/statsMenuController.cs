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

    // For most classes these are direct stat deltas.
    // For Archer/Mage: tempATK/tempHPP represent POINTS spent on that stat (server applies class rules).
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

    private static bool IsArcherOrMage(stats st)
    {
        if (st == null) return false;
        var ident = st.GetComponent<ClassIdentity>();
        return ident != null && (ident.classType == ClassType.Archer || ident.classType == ClassType.Mage);
    }

    private static int PreviewPercentHp(int baseHp, int hpPoints, float pctPerPoint)
    {
        int hp = Mathf.Max(1, baseHp);
        for (int i = 0; i < hpPoints; i++)
            hp = Mathf.Max(1, Mathf.CeilToInt(hp * (1f + pctPerPoint)));
        return hp;
    }

    public void loadStats()
    {
        var playerStats = GetLocalPlayerStats();
        if (playerStats == null) return;

        bool usesPercentHpRules = IsArcherOrMage(playerStats);

        int abilityLeft = Mathf.Max(0, playerStats.AbilityPoints.Value - abilityPointSpent);

        LEVEL.text = $"Level: {playerStats.Level.Value}";

        int baseAtk = playerStats.Damage.Value + playerStats.BonusDamage.Value;
        int shownAtk = usesPercentHpRules ? baseAtk + (tempATK * 10) : baseAtk + tempATK;
        ATK.text = $"ATK: {shownAtk}";

        DEF.text = $"DEF: {playerStats.Defense.Value + tempDEF}";
        SPD.text = $"SPD: {playerStats.MoveSpeed.Value + tempSPD}";

        int shownHp = usesPercentHpRules
            ? PreviewPercentHp(playerStats.MaxHP.Value, tempHPP, 0.10f)
            : playerStats.MaxHP.Value + tempHPP;
        HPP.text = $"HP: {shownHp}";

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
            case "ATK":
                // Archer/Mage: step is POINTS spent (server applies +10 damage per point).
                // Others: step is raw stat delta.
                tempATK += step;
                break;
            case "DEF":
                tempDEF += step;
                break;
            case "SPD":
                tempSPD += step;
                break;
            case "HPP":
                // Archer/Mage: step is POINTS spent (server applies +10% HP per point).
                // Others: step is raw stat delta.
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

        bool usesPercentHpRules = IsArcherOrMage(playerStats);

        // Archer/Mage: send POINTS (tempATK/tempHPP) instead of raw deltas.
        // Other classes: unchanged behavior (raw deltas).
        int sendDeltaDamage = usesPercentHpRules ? Mathf.Max(0, tempATK) : tempATK;
        int sendDeltaMaxHp = usesPercentHpRules ? Mathf.Max(0, tempHPP) : tempHPP;

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
