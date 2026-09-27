using ManosabaLin.Characters.Yalisalin.Relics;

namespace ManosabaLin.Characters.Yalisalin.Cards;

[RegisterCard(typeof(YalisalinCardPool))]
public sealed class Unseenkindling()
    : ManosabaCardTemplate(0, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new BlockVar(4, ValueProp.Move), new CardsVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var target = cardPlay.Target;
        ArgumentNullException.ThrowIfNull(target);

        if (!YalisalinFireColorSystem.TryGetHairpin(Owner, out var hairpin))
            return;

        var hadFireColor = hairpin.TargetHasFireColor(target);
        await hairpin.GiveFireColor(choiceContext, target, hadFireColor ? 1 : 2, this);

        if (hadFireColor)
            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);

        if (IsUpgraded)
            await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

}
