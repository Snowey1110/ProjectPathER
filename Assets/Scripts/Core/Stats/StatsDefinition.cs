using UnityEngine;

// Data-driven stats definition.
// Assign one of these to a prefab's `stats` component (or set it at spawn time) to define:
// - base stats
// - per-level growth
// - ability-point spending rules
// - special combat traits (Knight hit clamp, Archer %HP bonus, etc.)

[CreateAssetMenu(menuName = "Game/Stats/Stats Definition", fileName = "StatsDefinition")]
public class StatsDefinition : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Stable ID used by the StatsDefinitionLibrary (Resources/StatsDefinitions).")]
    public string id = "Generic";

    [Tooltip("Default starting level when a spawner/prefab does not specify a start level.")]
    public int defaultStartLevel = 1;

    [Header("Growth (per level)")]
    public IntGrowth maxHp = new IntGrowth { baseValue = 10, addPerLevel = 0, mulPerLevel = 1f };
    public IntGrowth damage = new IntGrowth { baseValue = 1, addPerLevel = 0, mulPerLevel = 1f };
    public IntGrowth defense = new IntGrowth { baseValue = 0, addPerLevel = 0, mulPerLevel = 1f };
    public IntGrowth mana = new IntGrowth { baseValue = 0, addPerLevel = 0, mulPerLevel = 1f };
    public FloatGrowth moveSpeed = new FloatGrowth { baseValue = 5f, addPerLevel = 0f, mulPerLevel = 1f };

    [Header("Other Base Stats")]
    [Tooltip("Flat bonus damage (added on top of Damage). Useful for items/buffs; default 0.")]
    public int baseBonusDamage = 0;

    [Header("Progression")]
    public int abilityPointsPerLevel = 5;
    public bool refillHpToFullOnLevelUp = true;

    [Header("Ability Point Spend Rules")]
    public SpendRuleInt spendDamage = new SpendRuleInt { mode = SpendRuleMode.AdditiveDelta, addPerPoint = 1, percentPerPoint = 0.10f };
    public SpendRuleInt spendMaxHp = new SpendRuleInt { mode = SpendRuleMode.AdditiveDelta, addPerPoint = 1, percentPerPoint = 0.10f };
    public SpendRuleInt spendDefense = new SpendRuleInt { mode = SpendRuleMode.AdditiveDelta, addPerPoint = 1, percentPerPoint = 0.10f };
    public SpendRuleInt spendMana = new SpendRuleInt { mode = SpendRuleMode.AdditiveDelta, addPerPoint = 1, percentPerPoint = 0.10f };
    public SpendRuleFloat spendMoveSpeed = new SpendRuleFloat { mode = SpendRuleMode.AdditiveDelta, addPerPoint = 1f, percentPerPoint = 0.10f };

    [Header("Combat Traits")]
    [Tooltip("If enabled, clamps a single hit so it cannot exceed (percent * current HP - minus).")]
    public bool enableSingleHitClamp = false;

    [Range(0.05f, 0.95f)]
    public float clampPercentOfCurrentHp = 0.5f;

    public int clampMinus = 1;

    [Tooltip("If enabled, projectiles can apply bonus damage equal to a percent of target's CURRENT HP.")]
    public bool enablePercentCurrentHpBonusOnHit = false;

    [Range(0f, 1f)]
    public float percentCurrentHpBonus = 0.10f;

    [Range(0f, 1f)]
    public float percentCurrentHpBonusBoss = 0.03f;

    [Tooltip("Tag name treated as 'boss' for percent-current-HP bonus damage.")]
    public string bossTag = "Boss";

    // -----------------
    // Helper structs
    // -----------------

    public enum SpendRuleMode
    {
        // delta is applied directly: value += delta
        AdditiveDelta = 0,

        // delta represents "points"; each point adds addPerPoint.
        PerPointAdd = 1,

        // delta represents "points"; each point applies a compounding percentage.
        PerPointPercentCompounding = 2,
    }

    [System.Serializable]
    public struct IntGrowth
    {
        public int baseValue;
        public int addPerLevel;
        [Tooltip("Multiplier applied each level (1 = no multiplier).")]
        public float mulPerLevel;

        public int EvaluateAtLevel(int level)
        {
            level = Mathf.Max(1, level);

            int v = Mathf.Max(1, baseValue);
            float mul = Mathf.Max(0.0001f, mulPerLevel);

            for (int i = 1; i < level; i++)
                v = Mathf.Max(1, Mathf.CeilToInt(v * mul) + addPerLevel);

            return v;
        }

        public int ApplyNextLevel(int current)
        {
            int v = Mathf.Max(1, current);
            float mul = Mathf.Max(0.0001f, mulPerLevel);
            return Mathf.Max(1, Mathf.CeilToInt(v * mul) + addPerLevel);
        }
    }

    [System.Serializable]
    public struct FloatGrowth
    {
        public float baseValue;
        public float addPerLevel;
        public float mulPerLevel;

        public float EvaluateAtLevel(int level)
        {
            level = Mathf.Max(1, level);
            float v = Mathf.Max(0.01f, baseValue);
            float mul = Mathf.Max(0.0001f, mulPerLevel);

            for (int i = 1; i < level; i++)
                v = Mathf.Max(0.01f, v * mul + addPerLevel);

            return v;
        }

        public float ApplyNextLevel(float current)
        {
            float v = Mathf.Max(0.01f, current);
            float mul = Mathf.Max(0.0001f, mulPerLevel);
            return Mathf.Max(0.01f, v * mul + addPerLevel);
        }
    }

    [System.Serializable]
    public struct SpendRuleInt
    {
        public SpendRuleMode mode;
        public int addPerPoint;
        public float percentPerPoint;

        public int PreviewDelta(int currentValue, int deltaOrPoints)
        {
            int cur = Mathf.Max(1, currentValue);
            int d = deltaOrPoints;

            if (d == 0) return cur;

            switch (mode)
            {
                case SpendRuleMode.AdditiveDelta:
                    return Mathf.Max(1, cur + d);

                case SpendRuleMode.PerPointAdd:
                    return Mathf.Max(1, cur + d * addPerPoint);

                case SpendRuleMode.PerPointPercentCompounding:
                {
                    int v = cur;
                    float mul = 1f + percentPerPoint;
                    for (int i = 0; i < d; i++)
                        v = Mathf.Max(1, Mathf.CeilToInt(v * mul));
                    return v;
                }
                default:
                    return Mathf.Max(1, cur + d);
            }
        }
    }

    [System.Serializable]
    public struct SpendRuleFloat
    {
        public SpendRuleMode mode;
        public float addPerPoint;
        public float percentPerPoint;

        public float PreviewDelta(float currentValue, float deltaOrPoints)
        {
            float cur = Mathf.Max(0.01f, currentValue);
            float d = deltaOrPoints;
            if (Mathf.Abs(d) < 0.00001f) return cur;

            switch (mode)
            {
                case SpendRuleMode.AdditiveDelta:
                    return Mathf.Max(0.01f, cur + d);

                case SpendRuleMode.PerPointAdd:
                    return Mathf.Max(0.01f, cur + d * addPerPoint);

                case SpendRuleMode.PerPointPercentCompounding:
                {
                    float v = cur;
                    float mul = 1f + percentPerPoint;
                    int steps = Mathf.RoundToInt(d);
                    for (int i = 0; i < steps; i++)
                        v = Mathf.Max(0.01f, v * mul);
                    return v;
                }
                default:
                    return Mathf.Max(0.01f, cur + d);
            }
        }
    }
}
