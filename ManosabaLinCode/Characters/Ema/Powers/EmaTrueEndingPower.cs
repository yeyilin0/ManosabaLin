using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Cards;
using ManosabaLin.Characters.Emalin.Components;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MinionLib.RightClick;
using MinionLib.RightClick.Easy;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Ema.Powers;

[RegisterPower]
public class EmaTrueEndingPower : ManosabaPowerTemplate, IEasyRightClickablePower
{
    private const int AchieveCountTarget = 13;
    private const int InvokeCountTarget = 2;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override int DisplayAmount => int.Clamp(AchieveCountTarget - _achievedCards.Count, 0, AchieveCountTarget);


    private HashSet<string> _achievedCards = [];
    private Dictionary<string, int> _cardCounter = [];

    public IReadOnlySet<string> AchievedCards => _achievedCards;

    protected override void AfterCloned()
    {
        _achievedCards = [];
        _cardCounter = [];
    }

    public override async Task AfterCardChangedPilesLate(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        if (card.Pile == null && card.Owner.Creature == Owner && card.HasComponent<EmaTrueEndingTagComponent>())
        {
            _achievedCards.Add(card.Id.Entry);
            InvokeDisplayAmountChanged();
            Flash();
        }

        if (_achievedCards.Count >= AchieveCountTarget)
        {
            await OnAchieve();
        }
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var card = cardPlay.Card;
        var id = card.Id.Entry;
        var count = _cardCounter.GetValueOrDefault(id, 0) + 1;
        _cardCounter[id] = count;

        if (count == InvokeCountTarget)
        {
            card.TryAddComponent(new EmaTrueEndingTagComponent());
            card.BaseReplayCount++;
        }
    }


    private async Task OnAchieve()
    {
        if (!Owner.IsPlayer) return;
        var target = Owner.Player!.Deck.Cards.OfType<EmaEnding>().FirstOrDefault();

        target?.AddComponent(new EmaTrueEndingRewardComponent(1));
    }

    public async Task OnRightClick(PlayerChoiceContext choiceContext, RightClickContext clickContext)
    {
        if (!Owner.IsPlayer || _achievedCards.Count == 0) return;

        var cards = _achievedCards
            .Select(cardId => ModelDb.GetById<CardModel>(new ModelId("CARD", cardId)))
            .ToList();

        if (cards.Count == 0) return;

        await CardSelectCmd.FromSimpleGrid(choiceContext, cards, Owner.Player!,
            new CardSelectorPrefs(SelectionScreenPrompt, 0)
            {
                Cancelable = true
            });
    }
}
