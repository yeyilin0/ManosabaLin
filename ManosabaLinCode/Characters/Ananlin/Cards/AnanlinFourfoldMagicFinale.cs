using ManosabaLin.Characters.Ananlin.Capabilities;
using ManosabaLin.Characters.Ananlin.Powers;
using ManosabaLin.Characters.Ananlin.Relics;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using STS2RitsuLib.Models.Capabilities;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinFourfoldMagicFinale()
    : ManosabaCardTemplate(2, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        HoverTipFactory.FromPower<AnanlinMargaretVoiceMagicPower>(),
        CocoComponentHoverTip,
        NayukaComponentHoverTip,
        HoverTipFactory.FromPower<AnanlinLeiaInductionMagicPower>(),
        HoverTipFactory.FromCard<AnanlinCocoMultiverseMagic>()
    ];

    private static IHoverTip CocoComponentHoverTip => new HoverTip(
        new LocString("cards", "MANOSABA_LIN_CARD_ANANLIN_FOURFOLD_MAGIC_FINALE.cocoHovertip.title"),
        new LocString("cards", "MANOSABA_LIN_CARD_ANANLIN_FOURFOLD_MAGIC_FINALE.cocoHovertip.description"));

    private static IHoverTip NayukaComponentHoverTip => new HoverTip(
        new LocString("cards", "MANOSABA_LIN_CARD_ANANLIN_FOURFOLD_MAGIC_FINALE.nayukaHovertip.title"),
        new LocString("cards", "MANOSABA_LIN_CARD_ANANLIN_FOURFOLD_MAGIC_FINALE.nayukaHovertip.description"));

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        await ApplyMargaretMagic(choiceContext);
        await AddCocoMagicCard();
        await AddNayukaHistoryCopy(choiceContext);

        if (cardPlay.Target is { } target)
            await ApplyLeiaInduction(choiceContext, target);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }

    private async Task ApplyMargaretMagic(PlayerChoiceContext choiceContext)
    {
        var power = await PowerCmd.Apply<AnanlinMargaretVoiceMagicPower>(
            choiceContext,
            Owner.Creature,
            1m,
            Owner.Creature,
            this);
        power?.RecordCurrentEnemyIntents(CombatState);
    }

    private async Task AddCocoMagicCard()
    {
        if (CombatState is not { } combatState) return;

        var card = combatState.CreateCard<AnanlinCocoMultiverseMagic>(Owner);
        var capability = card.GetOrCreateCapability<AnanlinCocoMultiverseCapability>();
        capability.RerollFromRecordedPools(this.Sketchbook());
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
    }

    private async Task AddNayukaHistoryCopy(PlayerChoiceContext choiceContext)
    {
        if (CombatState is null) return;

        var candidates = PileType.Exhaust.GetPile(Owner).Cards
            .Where(card => card != this && IsCopyableCard(card))
            .ToArray();
        if (candidates.Length == 0) return;

        var source = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            candidates,
            Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 0, 1))).FirstOrDefault();
        if (source is null) return;

        var copy = CombatState.CreateCard(source.CanonicalInstance, Owner);
        if (this.Sketchbook() is { } sketchbook)
            sketchbook.CopyVisibleAdditions(source, copy);
        else
            AnanlinCardHelpers.CopyUpgradeLevel(source, copy);

        if (WasPlayedThisCombat(source))
            copy.SetFreeIgnoringCardPlayConditions();

        copy.GetOrCreateCapability<AnanlinNayukaHistoryCapability>().Configure(source);
        await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, Owner);
    }

    private async Task ApplyLeiaInduction(PlayerChoiceContext choiceContext, Creature target)
    {
        if (target.Monster is not { NextMove: { } currentMove } monster) return;
        if (FindPrimaryIntentType(currentMove) is not { } intentType) return;

        var replacement = monster.MoveStateMachine?.States.Values
            .OfType<MoveState>()
            .Where(move => IsSameEffectReplacement(move, currentMove, intentType))
            .OrderBy(_ => Owner.RunState.Rng.MonsterAi.NextFloat())
            .FirstOrDefault();
        if (replacement is null) return;

        await PowerCmd.Apply<AnanlinLeiaInductionMagicPower>(
            choiceContext,
            target,
            1m,
            Owner.Creature,
            this);
        monster.SetMoveImmediate(replacement, forceTransition: true);
        AnanlinSilenceIntentManager.RecordIntentRewrites(CombatState, 1);
    }

    private static bool IsCopyableCard(CardModel card)
    {
        return card.Type is CardType.Attack or CardType.Skill or CardType.Power
            && card.Rarity is not CardRarity.Status
            and not CardRarity.Curse
            and not CardRarity.Quest
            and not CardRarity.Event;
    }

    private bool WasPlayedThisCombat(CardModel source)
    {
        return CombatManager.Instance.History.CardPlaysFinished.Any(entry =>
            entry.CardPlay.Card.Owner == Owner
            && entry.CardPlay.Card.Id == source.Id);
    }

    private static IntentType? FindPrimaryIntentType(MoveState move)
    {
        return move.Intents.FirstOrDefault(intent => intent.IntentType != IntentType.Hidden)?.IntentType;
    }

    private static bool IsSameEffectReplacement(
        MoveState candidate,
        MoveState currentMove,
        IntentType intentType)
    {
        return candidate != currentMove
            && candidate.StateId != currentMove.StateId
            && candidate.IsMove
            && candidate.ShouldAppearInLogs
            && FindPrimaryIntentType(candidate) == intentType;
    }
}
