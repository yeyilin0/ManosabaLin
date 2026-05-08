using ManosabaLin.Audio;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.HiroKeywords;
using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class HiroBadEnding : ManosabaCardTemplate
{
    private const int DirectDamage = 999;
    private const int WithPowerReduction = 50;
    private const int EnergyToGive = 3;
    private const int CardsToDraw = 3;

    private int _cardsInHand;

    public HiroBadEnding() : base(-1, CardType.Curse, CardRarity.Ancient, TargetType.None)
    {
    }

    public override int MaxUpgradeLevel => 0;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DynamicVar("Damage", DirectDamage);
            yield return new DynamicVar("WithGain", 20m);
            yield return new DynamicVar("SuspectGain", 3m);
        }
    }

    protected override IEnumerable<string> RegisteredKeywordIds =>
        new[] { HiroKeywordRules.HiroKeywordId };

    public int CardsInHand
    {
        get => _cardsInHand;
        set
        {
            AssertMutable();
            _cardsInHand = value;
        }
    }



    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amountChanged,
        Creature? applier,
        CardModel? cardSource)
    {
        var source = this;

        if (power is not PerjuryPower) return;
        if (amountChanged <= 0) return;
        if (power.Owner != source.Owner.Creature) return;
        if (source.Pile.Type != PileType.Hand) return;

        await PowerCmd.ModifyAmount(choiceContext, power, -1, source.Owner.Creature, source, false);
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        var source = this;
        if (player != source.Owner) return;

        if (source.Pile?.Type != PileType.Hand) await CardPileCmd.Add(source, PileType.Hand);

        var rebirthId = TransmigrationRules.TransmigrationKeywordId;

        var drawCards = PileType.Draw.GetPile(source.Owner).Cards.Where(c => c.HasModKeyword(rebirthId)).ToList();
        var handCards = PileType.Hand.GetPile(source.Owner).Cards.Where(c => c != this && c.HasModKeyword(rebirthId))
            .ToList();
        var discardCards = PileType.Discard.GetPile(source.Owner).Cards.Where(c => c.HasModKeyword(rebirthId)).ToList();

        var allCards = new List<CardModel>();
        allCards.AddRange(drawCards);
        allCards.AddRange(handCards);
        allCards.AddRange(discardCards);

        if (allCards.Count == 0) return;

        var selected = new List<CardModel>();

        if (drawCards.Count > 0)
        {
            var result = await CardSelectCmd.FromSimpleGrid(choiceContext, drawCards, source.Owner,
                new CardSelectorPrefs(source.SelectionScreenPrompt, 1));
            selected.AddRange(result);
        }

        if (handCards.Count > 0)
        {
            var result = await CardSelectCmd.FromSimpleGrid(choiceContext, handCards, source.Owner,
                new CardSelectorPrefs(source.SelectionScreenPrompt, 1));
            selected.AddRange(result);
        }

        if (discardCards.Count > 0)
        {
            var result = await CardSelectCmd.FromSimpleGrid(choiceContext, discardCards, source.Owner,
                new CardSelectorPrefs(source.SelectionScreenPrompt, 1));
            selected.AddRange(result);
        }

        foreach (var card in selected)
        {
            card.RemoveModKeyword(rebirthId);
            RefreshCardVisuals(card);
        }
    }

    private static void RefreshCardVisuals(CardModel card)
    {
        var node = NCard.FindOnTable(card);
        if (node != null) node.UpdateVisuals(card.Pile?.Type ?? PileType.Hand, CardPreviewMode.Normal);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        var source = this;
        if (source.Pile?.Type != PileType.Hand) return;
        if (cardPlay.Card.Owner != source.Owner) return;
        if (cardPlay.Card.Type != CardType.Attack) return;

        await PowerCmd.Apply<WithPower>(
            context,
            source.Owner.Creature,
            source.DynamicVars["WithGain"].BaseValue,
            source.Owner.Creature,
            cardPlay.Card,
            false
        );
    }


    public override Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != CombatSide.Player || Pile?.Type != PileType.Hand)
            return Task.CompletedTask;
        CardsInHand = Pile.Cards.Count;
        return Task.CompletedTask;
    }

    public override bool HasTurnEndInHandEffect => true;

    protected override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext)
    {
        var source = this;

        var justicePower = source.Owner.Creature.GetPower<JusticePower>();
        var justiceAmount = justicePower?.Amount ?? 0;
        if (justiceAmount > 0)
            await CreatureCmd.Damage(choiceContext, source.Owner.Creature, justiceAmount,
                ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, source);

        await PowerCmd.Apply<SuspectPower>(
            choiceContext,
            source.Owner.Creature,
            source.DynamicVars["SuspectGain"].BaseValue,
            source.Owner.Creature,
            source,
            false
        );
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var source = this;
        if (source.CombatState == null) return;

        ManosabaAudio.TryPlayOneShot("hiro_bad_ending_theme.mp3".BgmAudioPath());

        var hiroDeath = source.CombatState.CreateCard<Hirodeath>(source.Owner);
        hiroDeath.SetToFreeThisTurn();
        await CardPileCmd.AddGeneratedCardToCombat(hiroDeath, PileType.Hand, source.Owner);
        await CardCmd.AutoPlay(choiceContext, hiroDeath, null, skipCardPileVisuals: true);

        await CreatureCmd.Damage(choiceContext, source.Owner.Creature, source.DynamicVars["Damage"].BaseValue,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, source);
    }

    public override async Task AfterCardDiscarded(PlayerChoiceContext choiceContext, CardModel card)
    {
        if (!ReferenceEquals(card, this)) return;
        await CardPileCmd.Add(this, PileType.Hand);
    }

    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card,
        bool causedByEthereal)
    {
        if (!ReferenceEquals(card, this)) return;
        await CardPileCmd.Add(this, PileType.Hand);
    }

    protected override void OnUpgrade()
    {
    }
}