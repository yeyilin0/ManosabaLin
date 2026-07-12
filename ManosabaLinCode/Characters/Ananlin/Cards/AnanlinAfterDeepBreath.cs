using ManosabaLin.Characters.Ananlin.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinAfterDeepBreath()
    : ManosabaCardTemplate(3, CardType.Skill, CardRarity.Rare, TargetType.Self),
        IAnanlinPeaceOfMindSpecialCard
{
    [SavedProperty] public int PendingExtraPlays { get; set; }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<AnanlinPeaceOfMindPower>()
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var lost = await this.LosePeaceOfMind(choiceContext, int.MaxValue);
        if (lost <= 0) return;

        await PlayerCmd.GainEnergy(lost, Owner);
        await CardPileCmd.Draw(choiceContext, lost, Owner);

        var extraPlays = Math.Min(AnanlinPeaceOfMindPower.MaxStacks, lost) - 1;
        if (extraPlays > 0)
            PendingExtraPlays += extraPlays;
    }

    protected override int ModifyCardPlayCountC(CardModel card, Creature? target, int playCount)
    {
        if (PendingExtraPlays <= 0 || card.Owner != Owner || card.Type == CardType.Attack)
            return playCount;

        return playCount + PendingExtraPlays;
    }

    protected override Task AfterModifyingCardPlayCount(CardModel card, ComponentContext componentContext)
    {
        PendingExtraPlays = 0;
        return Task.CompletedTask;
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
