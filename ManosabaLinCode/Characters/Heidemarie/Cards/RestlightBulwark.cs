using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Component.Core;
using MinionLib.Component.Interfaces;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Heidemarie.Cards;

[RegisterCard(typeof(HeidemarieCardPool))]
public sealed class RestlightBulwark() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public const string LinkedCardsPerBlockKey = "LinkedCardsPerBlock";
    public const string BlockPerLinkedBatchKey = "BlockPerLinkedBatch";

    private int? _linkedCardsAtLinkCleanupStart;

    public override bool GainsBlock => true;

    protected override IEnumerable<ICardComponent> CanonicalComponents =>
    [
        new RestComponent()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(1m, ValueProp.Move),
        new DynamicVar(LinkedCardsPerBlockKey, 1m),
        new BlockVar(BlockPerLinkedBatchKey, 1m, ValueProp.Move)
    ];

    protected override Task BeforeCardPlayed(CardPlay cardPlay, ComponentContext componentContext)
    {
        if (cardPlay.IsAutoPlay
            || !cardPlay.IsFirstInSeries
            || ReferenceEquals(cardPlay.Card, this)
            || cardPlay.Card.Owner != Owner
            || Pile?.Type != PileType.Hand
            || !((CardModel)this).HasComponent<LinkComponent>()
            || !cardPlay.Card.HasComponent<LinkComponent>())
            return Task.CompletedTask;

        _linkedCardsAtLinkCleanupStart = CountCurrentHandLinkedCards();
        return Task.CompletedTask;
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var linkedCardsInHand = CountLinkedCardsForEffect();
        _linkedCardsAtLinkCleanupStart = null;

        var linkedCardsPerBlock = DynamicVars[LinkedCardsPerBlockKey].IntValue;
        if (linkedCardsPerBlock <= 0)
            linkedCardsPerBlock = 1;

        var bonusBatches = linkedCardsInHand / linkedCardsPerBlock;
        var block = DynamicVars.Block.BaseValue
            + bonusBatches * DynamicVars[BlockPerLinkedBatchKey].BaseValue;

        await CreatureCmd.GainBlock(Owner.Creature, block, ValueProp.Move, cardPlay);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }

    private int CountLinkedCardsForEffect()
    {
        if (LinkDiscardContext.IsActiveFor(this) && _linkedCardsAtLinkCleanupStart.HasValue)
            return _linkedCardsAtLinkCleanupStart.Value;

        return CountCurrentHandLinkedCards();
    }

    private int CountCurrentHandLinkedCards()
    {
        return PileType.Hand.GetPile(Owner).Cards.Count(card => card.HasComponent<LinkComponent>());
    }
}
