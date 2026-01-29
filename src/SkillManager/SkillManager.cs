using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Translation;

namespace SW2_RandomSkills;

public class SkillManager
{
    private readonly Dictionary<SkillType, BaseSkill> _skillPrototypes = new();
    private readonly Random _random = new();

    public void RegisterSkill(BaseSkill skill)
    {
        _skillPrototypes[skill.Type] = skill;
    }

    public BaseSkill? CreateSkillInstance(SkillType type, ILocalizer localizer, ISwiftlyCore core)
    {
        if (_skillPrototypes.TryGetValue(type, out var prototype))
        {
            var newSkill = (BaseSkill)Activator.CreateInstance(prototype.GetType(), localizer, core)!;
            return newSkill;
        }
        return null;
    }

    public SkillType GetRandomSkill(SkillsConfig config)
    {
        var availableSkills = config.Skills
            .Where(kvp => kvp.Value.Weight > 0)
            .ToList();

        if (availableSkills.Count == 0) return SkillType.None;

        int totalWeight = availableSkills.Sum(kvp => kvp.Value.Weight);
        int randomValue = _random.Next(totalWeight);
        int cumulativeWeight = 0;

        foreach (var (skillType, skillConfig) in availableSkills)
        {
            cumulativeWeight += skillConfig.Weight;
            if (randomValue < cumulativeWeight)
            {
                return skillType;
            }
        }

        // Fallback: return the first skill with weight > 0
        return availableSkills.FirstOrDefault().Key;
    }
}