using ManosabaLin.Characters.Yalisalin.Relics;

namespace ManosabaLin.Characters.Yalisalin.Cards;

/// <summary>不准降下来：每回合开始时随机给予 1 名敌人火色（由发夹在回合开始结算，多张叠加）。</summary>
[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Dontcooldown()
    : ManosabaCardTemplate(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Fire", 4)];

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        if (YalisalinFireColorSystem.TryGetHairpin(Owner, out var hairpin))
            hairpin.AddTurnStartFireGift(DynamicVars["Fire"].IntValue);

        return Task.CompletedTask;
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["Fire"].UpgradeValueBy(1);
    }
}
