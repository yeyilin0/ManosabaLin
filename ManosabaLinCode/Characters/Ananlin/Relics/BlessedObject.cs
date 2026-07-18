using ManosabaLin.Characters.Ananlin.Cards;
using ManosabaLin.Characters.Ananlin.Powers;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Ananlin.Relics;

[RegisterRelic(typeof(AnanlinRelicPool))]
public sealed class BlessedObject : AnansSketchbook
{
    private const float BlankPageReplacementChance = 0.2f;

    [SavedProperty] public bool GrantedButterflyBeforeAttackIntentSilence { get; set; }

    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            foreach (var tip in base.AdditionalHoverTips)
                yield return tip;

            foreach (var tip in HoverTipFactory.FromRelic<AnansSketchbook>())
                yield return tip;
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

    internal override async Task TriggerSilenceRewrite(PlayerChoiceContext choiceContext)
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

        await base.TriggerSilenceRewrite(choiceContext);
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
        if (card is not BlankPage) return;
        if (card.Pile is null || !card.Pile.Type.IsCombatPile()) return;
        if (Owner.RunState.Rng.CombatCardGeneration.NextFloat() >= BlankPageReplacementChance) return;

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
}
