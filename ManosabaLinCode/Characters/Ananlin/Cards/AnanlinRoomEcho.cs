using ManosabaLin.Characters.Ananlin.Powers;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinRoomEcho()
    : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy),
        IAnanlinPeaceOfMindSpecialCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar("Bonus", 2)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<AnanlinPeaceOfMindPower>()
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        if (this.Sketchbook() is not { } sketchbook) return;
        var bonus = this.PeaceOfMindAmount() * DynamicVars["Bonus"].IntValue;

        if (sketchbook.LastPlayedCardType == CardType.Skill)
        {
            var block = sketchbook.GetRoomEchoValue(CardType.Skill) + bonus;
            if (block > 0)
                await CreatureCmd.GainBlock(Owner.Creature, block, ValueProp.Move, cardPlay);
            return;
        }

        if (sketchbook.LastPlayedCardType != CardType.Attack) return;
        if (cardPlay.Target is not { IsAlive: true } target) return;

        var damage = sketchbook.GetRoomEchoValue(CardType.Attack) + bonus;
        if (damage <= 0) return;

        await DamageCmd.Attack(damage)
            .FromCard(this, cardPlay)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["Bonus"].UpgradeValueBy(1m);
    }
}
