using ManosabaLin.Characters.Common.Components;

namespace ManosabaLin.Characters.Heidemarie.Powers;

[RegisterPower]
public sealed class EmberTetherDrawPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    protected override bool IsVisibleInternal => false;
    public override bool ShouldPlayVfx => false;

    public static void GrantLinkToRestCard(CardModel card)
    {
        if (!card.HasComponent<RestComponent>() || card.HasComponent<LinkComponent>())
            return;

        card.TryAddComponent(new LinkComponent());
    }

    public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        if (card.Owner != Owner.Player || card.Pile?.Type != PileType.Hand)
            return Task.CompletedTask;

        GrantLinkToRestCard(card);
        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Side || !participants.Contains(Owner))
            return;

        await PowerCmd.Remove(this);
    }
}
