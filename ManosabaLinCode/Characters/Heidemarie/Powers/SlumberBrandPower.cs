using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ManosabaLin.Characters.Heidemarie.Powers;

[RegisterPower]
public sealed class SlumberBrandPower : ManosabaPowerTemplate
{
    private CardModel? _triggeredCard;
    private bool _triggeredLayerReturnsToHand;
    private bool _temporaryRestAddedToTriggeredCard;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    protected override bool IsVisibleInternal => false;
    public override bool ShouldPlayVfx => false;

    [SavedProperty]
    public bool[] ReturnToHandLayers { get; set; } = [];

    public void AddLayer(bool returnsToHand)
    {
        AssertMutable();

        var layers = ReturnToHandLayers;
        Array.Resize(ref layers, layers.Length + 1);
        layers[^1] = returnsToHand;
        ReturnToHandLayers = layers;
    }

    public override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(
        CardModel card,
        bool isAutoPlay,
        ResourceInfo resources,
        PileType pileType,
        CardPilePosition position)
    {
        if (!CanTriggerFor(card) || _triggeredCard != null)
            return (pileType, position);

        _triggeredCard = card;
        _triggeredLayerReturnsToHand = NextLayerReturnsToHand();

        if (_triggeredLayerReturnsToHand && pileType != PileType.None)
            return (PileType.Hand, CardPilePosition.Bottom);

        return (pileType, position);
    }

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (!ReferenceEquals(cardPlay.Card, _triggeredCard) || !cardPlay.IsFirstInSeries)
            return Task.CompletedTask;

        if (!cardPlay.Card.HasComponent<RestComponent>())
            _temporaryRestAddedToTriggeredCard = cardPlay.Card.TryAddComponent(new RestComponent()) != null;

        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!ReferenceEquals(cardPlay.Card, _triggeredCard) || !cardPlay.IsLastInSeries)
            return;

        ClearTemporaryRest();
        RemoveConsumedLayer();
        ClearTriggeredState();

        if (Amount <= 1)
            await PowerCmd.Remove(this);
        else
            await PowerCmd.Decrement(this);
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side == Owner.Side && participants.Contains(Owner))
        {
            ClearTemporaryRest();
            ClearTriggeredState();
            await PowerCmd.Remove(this);
        }
    }

    private bool CanTriggerFor(CardModel card)
    {
        return Amount > 0
            && card.Owner == Owner.Player
            && card.Type == CardType.Attack;
    }

    private bool NextLayerReturnsToHand()
    {
        return ReturnToHandLayers.Length > 0 && ReturnToHandLayers[0];
    }

    private void RemoveConsumedLayer()
    {
        AssertMutable();

        if (ReturnToHandLayers.Length == 0)
            return;

        ReturnToHandLayers = ReturnToHandLayers.Skip(1).ToArray();
    }

    private void ClearTemporaryRest()
    {
        if (!_temporaryRestAddedToTriggeredCard)
            return;

        _triggeredCard.TryRemoveComponent<RestComponent>();
    }

    private void ClearTriggeredState()
    {
        _triggeredCard = null;
        _triggeredLayerReturnsToHand = false;
        _temporaryRestAddedToTriggeredCard = false;
    }
}
