using ManosabaLin.Characters.Ananlin.Cards;
using ManosabaLin.Characters.Ananlin.Powers;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Ananlin.Relics;

[RegisterRelic(typeof(AnanlinRelicPool))]
public sealed class BlessedObject : AnansSketchbook
{
    private const float BlankPageReplacementChance = 0.2f;
    private const string BlankPageReplacementRngSalt = "blessed_object_blank_page_replacement";

    [SavedProperty] public bool GrantedButterflyBeforeAttackIntentSilence { get; set; }

    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            foreach (var tip in base.AdditionalHoverTips)
                yield return tip;

            yield return ModelDb.Relic<AnansSketchbook>().HoverTip;
        }
    }

    public override async Task BeforeCombatStart()
    {
        await base.BeforeCombatStart();
        GrantedButterflyBeforeAttackIntentSilence = false;
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();

        if (AnansSketchbookRefinementMemory.TryRestore(this))
            InvokeDisplayAmountChanged();
    }

    internal override async Task<IReadOnlyList<Creature>> TriggerSilenceRewriteAndGetTargets(PlayerChoiceContext choiceContext)
    {
        if (!GrantedButterflyBeforeAttackIntentSilence && HasAnyEnemyAttackIntent())
        {
            GrantedButterflyBeforeAttackIntentSilence = true;
            Flash();
            await PowerCmd.Apply<CrimsonbutterflyPower>(
                choiceContext,
                Owner.Creature,
                2,
                Owner.Creature,
                null,
                false);
        }

        return await base.TriggerSilenceRewriteAndGetTargets(choiceContext);
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        await base.AfterSideTurnEnd(choiceContext, side, participants);

        if (!participants.Contains(Owner.Creature)) return;

        Flash();
        await PowerCmd.Apply<AnanlinPeaceOfMindPower>(
            choiceContext,
            Owner.Creature,
            1,
            Owner.Creature,
            null,
            false);
    }

    public override async Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
    {
        await base.AfterCardGeneratedForCombat(card, creator);

        if (card.Owner != Owner) return;
        if (card is not BlankPage blankPage) return;
        if (blankPage.BlessedObjectReplacementResolved) return;
        if (card.Pile is null || !card.Pile.Type.IsCombatPile()) return;
        if (!ShouldReplaceBlankPage(card, creator)) return;

        var pileType = card.Pile.Type;
        var isUpgraded = card.IsUpgraded;
        var combatState = Owner.Creature.CombatState;
        if (combatState is null) return;

        var marginPage = combatState.CreateCard<MarginPage>(Owner);
        if (isUpgraded)
            CardCmd.Upgrade(marginPage);

        Flash();
        await CardPileCmd.RemoveFromCombat(card, skipVisuals: true);

        var position = pileType == PileType.Draw ? CardPilePosition.Random : CardPilePosition.Bottom;
        await CardPileCmd.AddGeneratedCardToCombat(marginPage, pileType, creator ?? Owner, position);
    }

    private bool ShouldReplaceBlankPage(CardModel card, Player? creator)
    {
        var generatedCardOrdinal = CombatManager.Instance.History.Entries.OfType<CardGeneratedEntry>().Count();
        return ShouldReplaceBlankPage(creator, generatedCardOrdinal);
    }

    internal CardModel CreateBlankPageOrReplacement(ICombatState combatState, bool upgraded, Player? creator)
    {
        var page = ShouldReplaceBlankPage(creator, GetNextGeneratedCardOrdinal())
            ? (CardModel)combatState.CreateCard<MarginPage>(Owner)
            : combatState.CreateCard<BlankPage>(Owner);

        if (page is BlankPage blankPage)
            blankPage.BlessedObjectReplacementResolved = true;

        if (upgraded)
            CardCmd.Upgrade(page);

        return page;
    }

    private static int GetNextGeneratedCardOrdinal()
    {
        return CombatManager.Instance.History.Entries.OfType<CardGeneratedEntry>().Count() + 1;
    }

    private bool ShouldReplaceBlankPage(Player? creator, int generatedCardOrdinal)
    {
        // Keep this passive replacement from advancing shared combat RNG while multiplayer queues continue independently.
        var creatorSlot = creator is null ? -1 : Owner.RunState.GetPlayerSlotIndex(creator);
        var mixin = (uint)StringHelper.GetDeterministicHashCode(
            $"{BlankPageReplacementRngSalt}:{Owner.RunState.TotalFloor}:{generatedCardOrdinal}:{ModelDb.GetId<BlankPage>().Entry}:{creatorSlot}");
        var rng = new Rng(Owner, Id, mixin);

        return rng.NextFloat() < BlankPageReplacementChance;
    }
}
