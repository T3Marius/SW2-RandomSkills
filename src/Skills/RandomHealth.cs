using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Translation;

namespace SW2_RandomSkills;

public class RandomHealthSkill(ILocalizer localizer, ISwiftlyCore core) : BaseSkill(localizer, core)
{
    public override SkillType Type => SkillType.RandomHealth;
    public override string Name => "Random Health";
    private Random _random = new();
    private string _randomHealthValues = null!;
    protected override string GetDefaultDescription()
    {
        return "Gives you a random amount of health!";
    }
    public override void Initialize(SkillConfig config)
    {
        base.Initialize(config);
        _randomHealthValues = GetParameter("RandomHealthValues", "100,125,200,255,300");
    }
    public override void Apply(IPlayer player)
    {
        string[] healths = _randomHealthValues.Split(",", StringSplitOptions.RemoveEmptyEntries);

        string randomHealthSelected = healths[_random.Next(healths.Count())];

        if (!int.TryParse(randomHealthSelected, out int randomHealth))
        {
            SW2_RandomSkills.Core.Logger.LogInformation($"Invalid random health value at Random Health skill. Please enter an integer.");
            return;
        }

        player.SetHealth(randomHealth);

        player.SendChat(Localizer["prefix"] + Localizer["random_health.given", randomHealth]);

    }
}