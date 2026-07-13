using ManosabaLin.Characters.Hiro.Powers;

namespace ManosabaLin.Characters.Ananlin.Relics;

[RegisterRelic(typeof(AnanlinRelicPool))]
[RegisterCharacterStarterRelic(typeof(Ananlin))]
public sealed class AnanlinWitchFactor : ManosabaRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<WithPower>("AttackGain", 20m),
        new PowerVar<WithPower>("SkillLoss", 10m),
        new PowerVar<WithPower>("PowerLoss", 10m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<WithPower>()
    ];

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner)
            return;

        var amount = cardPlay.Card.Type switch
        {
            CardType.Attack => DynamicVars["AttackGain"].BaseValue,
            CardType.Skill => -DynamicVars["SkillLoss"].BaseValue,
            CardType.Power => -DynamicVars["PowerLoss"].BaseValue,
            _ => 0m
        };

        if (amount < 0)
        {
            var currentWith = Owner.Creature.GetPower<WithPower>();
            if (currentWith is null)
                return;

            amount = -Math.Min(-amount, currentWith.Amount);
        }

        if (amount == 0) return;

        Flash();
        await PowerCmd.Apply<WithPower>(
            choiceContext,
            Owner.Creature,
            amount,
            Owner.Creature,
            cardPlay.Card,
            false);
    }
}
