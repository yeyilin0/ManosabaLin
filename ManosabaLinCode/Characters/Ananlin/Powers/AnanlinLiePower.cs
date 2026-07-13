using ManosabaLin.Characters.Ananlin.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinLiePower : ManosabaPowerTemplate
{
    private const string VigorPerLieKey = "VigorPerLie";

    private static readonly HashSet<string> StableMultiHitAttackNames =
    [
        "AstralPulse",
        "CelestialMight",
        "Conflagration",
        "DaggerSpray",
        "Dismantle",
        "Exterminate",
        "FightMe",
        "GunkUp",
        "Maul",
        "Peck",
        "Refract",
        "Ricochet",
        "RipAndTear",
        "SevenStars",
        "SwordBoomerang",
        "Thrash",
        "TwinStrike",
        "Uproar"
    ];

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override LocString Description
    {
        get
        {
            var description = base.Description;
            description.Add(new PowerVar<VigorPower>(VigorPerLieKey, 2m));
            return description;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<VigorPower>(VigorPerLieKey, 2m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<SilentPower>(),
        HoverTipFactory.FromPower<VigorPower>()
    ];

    internal async Task ResolveAfterSilenceRightClick(PlayerChoiceContext choiceContext)
    {
        if (Owner.Player is not { } player || Amount <= 0) return;

        Flash();
        var lieAmount = Math.Max(0, (int)Amount);

        if (Owner.GetPower<SilentPower>() is { } silence && silence.Amount > 0)
            await PowerCmd.ModifyAmount(choiceContext, silence, -silence.Amount, Owner, null);

        await PowerCmd.Apply<VigorPower>(
            choiceContext,
            Owner,
            lieAmount * DynamicVars[VigorPerLieKey].BaseValue,
            Owner,
            null);

        await AddRandomMultiHitAttackToHand(player);
        await PowerCmd.Remove(this);
    }

    private async Task AddRandomMultiHitAttackToHand(Player player)
    {
        if (CombatState is not { } combatState) return;

        var candidates = BuildPlayableMultiHitAttacks(player, combatState).ToArray();
        if (candidates.Length == 0) return;

        var card = player.RunState.Rng.CombatCardGeneration.NextItem(candidates);
        if (card is null) return;

        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
    }

    private IEnumerable<CardModel> BuildPlayableMultiHitAttacks(Player player, ICombatState combatState)
    {
        var seenIds = new HashSet<ModelId>();

        foreach (var pool in player.UnlockState.CharacterCardPools)
        {
            foreach (var template in pool.GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint))
            {
                if (!seenIds.Add(template.Id)) continue;
                if (!IsStableMultiHitAttack(template)) continue;
                if (!AnansSketchbook.CanSketchbookGenerate(template)) continue;

                var card = combatState.CreateCard(template, player);
                card.SetToFreeThisTurn();

                if (IsCurrentlyPlayable(card, combatState))
                    yield return card;
            }
        }
    }

    private static bool IsStableMultiHitAttack(CardModel template)
    {
        if (template.Type != CardType.Attack) return false;
        if (template.EnergyCost.CostsX) return false;
        if (template.DynamicVars.ContainsKey("CalculatedHits")) return false;
        if (template.DynamicVars.ContainsKey("CardCount")) return false;

        if (template.DynamicVars.TryGetValue("HitCount", out var hitCountVar)
            && hitCountVar.IntValue > 1)
            return true;

        return StableMultiHitAttackNames.Contains(template.GetType().Name);
    }

    private static bool IsCurrentlyPlayable(CardModel card, ICombatState combatState)
    {
        if (!HasValidTarget(card, combatState)) return false;

        return card.CanPlay();
    }

    private static bool HasValidTarget(CardModel card, ICombatState combatState)
    {
        return card.TargetType switch
        {
            TargetType.AnyEnemy => combatState.Enemies.Any(card.IsValidTarget),
            TargetType.AnyAlly => combatState.PlayerCreatures.Any(card.IsValidTarget),
            _ => card.IsValidTarget(null)
        };
    }
}
