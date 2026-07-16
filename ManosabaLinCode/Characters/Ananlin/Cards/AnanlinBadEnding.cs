using ManosabaLin.Characters.Ananlin.Components;
using ManosabaLin.Characters.Ananlin.Powers;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(LinCardPool))]
public sealed class AnanlinBadEnding() : ManosabaCardTemplate(-1, CardType.Curse, CardRarity.Ancient, TargetType.None)
{
    protected override IEnumerable<ICardComponent> CanonicalComponents => [new AnanDeath()];

    private const int SuspectGain = 3;
    private const int BacklashGain = 1;
    private const int CurseChancePercent = 50;
    private const int DirectDamage = 999;

    public override int MaxUpgradeLevel => 0;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<SuspectPower>("SuspectGain", SuspectGain),
        new PowerVar<AnanlinBrainwashBacklashPower>("BacklashGain", BacklashGain),
        new IntVar("CurseChance", CurseChancePercent),
        new DynamicVar("Damage", DirectDamage)
    ];

   

    internal static async Task TryAddRandomCurseFromPage(PlayerChoiceContext choiceContext, Player owner, CardModel source)
    {
        if (!IsActiveInHand(owner)) return;
        if (owner.Creature.CombatState is not { } combatState) return;

        var rng = owner.RunState.Rng.CombatCardGeneration;
        if (rng.NextFloat() >= CurseChancePercent / 100f) return;

        var curses = ModelDb.CardPool<CurseCardPool>()
            .GetUnlockedCards(owner.UnlockState, owner.RunState.CardMultiplayerConstraint)
            .Where(static card => card.CanBeGeneratedByModifiers)
            .ToArray();
        if (curses.Length == 0) return;

        var canonical = rng.NextItem(curses);
        if (canonical is null) return;

        var curse = combatState.CreateCard(canonical, owner);
        await CardPileCmd.AddGeneratedCardToCombat(curse, PileType.Hand, owner);
    }

    protected override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        Player player,
        ComponentContext componentContext)
    {
        if (player != Owner) return;

        if (Pile?.Type != PileType.Hand)
            await CardPileCmd.Add(this, PileType.Hand);

        await PowerCmd.Apply<SuspectPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["SuspectGain"].BaseValue,
            Owner.Creature,
            this,
            false);
    }

    protected override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amountChanged,
        Creature? applier,
        CardModel? cardSource,
        ComponentContext componentContext)
    {
        if (Pile?.Type != PileType.Hand) return;
        if (power is not SilentPower || power.Owner != Owner.Creature || amountChanged >= 0) return;

        await PowerCmd.Apply<AnanlinBrainwashBacklashPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["BacklashGain"].BaseValue,
            Owner.Creature,
            this,
            false);
    }

    protected override bool HasTurnEndInHandEffectC => true;

    protected override async Task OnTurnEndInHand(PlayerChoiceContext choiceContext, ComponentContext componentContext)
    {
        if (Owner.Creature.GetPower<AnanlinPeaceOfMindPower>() is not { Amount: > 0 }) return;

        await PowerCmd.Apply<AnanlinIsolatedPower>(
            choiceContext,
            Owner.Creature,
            1,
            Owner.Creature,
            this,
            false);
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        if (CombatState is null) return;

        var owner = Owner;
        var creature = owner.Creature;
        var ananDeath = CombatState.CreateCard<Anandeath>(owner);
        ananDeath.SetToFreeThisTurn();
        await CardPileCmd.AddGeneratedCardToCombat(ananDeath, PileType.Hand, owner);
        await CardCmd.AutoPlay(choiceContext, ananDeath, null, skipCardPileVisuals: true);

        var buffsToRemove = creature.Powers
            .Where(static power => power.Type == PowerType.Buff)
            .ToList();
        foreach (var buff in buffsToRemove)
            await PowerCmd.Remove(buff);

        await CreatureCmd.Damage(
            choiceContext,
            creature,
            DynamicVars["Damage"].BaseValue,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
            this,
            cardPlay);
    }

    protected override async Task AfterCardDiscarded(
        PlayerChoiceContext choiceContext,
        CardModel card,
        ComponentContext componentContext)
    {
        if (!ReferenceEquals(card, this)) return;
        await CardPileCmd.Add(this, PileType.Hand);
    }

    protected override async Task AfterCardExhausted(
        PlayerChoiceContext choiceContext,
        CardModel card,
        bool causedByEthereal,
        ComponentContext componentContext)
    {
        if (!ReferenceEquals(card, this)) return;
        await CardPileCmd.Add(this, PileType.Hand);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
    }

    private static bool IsActiveInHand(Player owner)
    {
        return PileType.Hand.GetPile(owner).Cards.OfType<AnanlinBadEnding>().Any();
    }
}
