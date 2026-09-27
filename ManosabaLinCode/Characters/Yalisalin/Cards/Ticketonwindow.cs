using ManosabaLin.Characters.Yalisalin.Relics;

namespace ManosabaLin.Characters.Yalisalin.Cards;

/// <summary>贴在窗上的罚单：给予火色的效果额外多给 1 格；一回合内给够 5 格时获得能量（由发夹结算，多张叠加）。</summary>
[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Ticketonwindow()
    : ManosabaCardTemplate(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        if (YalisalinFireColorSystem.TryGetHairpin(Owner, out var hairpin))
            hairpin.AddTicketStack();
        return Task.CompletedTask;
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
