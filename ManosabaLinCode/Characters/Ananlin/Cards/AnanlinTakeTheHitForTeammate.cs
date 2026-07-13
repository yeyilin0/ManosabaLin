using ManosabaLin.Characters.Ananlin.Powers;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinTakeTheHitForTeammate()
    : ManosabaCardTemplate(2, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
{
    private const string BlockThresholdKey = "BlockThreshold";
    private const string IntentDamageKey = "IntentDamage";

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar(BlockThresholdKey, 15m),
        new PowerVar<ThornsPower>(1m),
        new DamageVar(IntentDamageKey, 1m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<ThornsPower>()
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var teammate = PickRandomTeammate();
        if (teammate is not null && teammate.Block > 0)
            await CreatureCmd.GainBlock(Owner.Creature, teammate.Block, ValueProp.Move, cardPlay);

        if (cardPlay.Target is not { Monster: { } monster } target) return;
        if (!monster.IntendsToAttack) return;

        var beforeCount = CombatManager.Instance.History.Entries.Count();
        var move = monster.NextMove;
        await move.PerformMove([Owner.Creature]);
        monster.MoveStateMachine?.OnMovePerformed(move);
        CombatManager.Instance.History.MonsterPerformedMove(CombatState, monster, move, [Owner.Creature]);

        var blockedResults = GetDamageReceivedSince(beforeCount)
            .Where(entry => entry.Receiver == Owner.Creature
                && entry.Dealer == target
                && entry.Result.BlockedDamage > 0
                && entry.Result.Props.IsPoweredAttack())
            .Select(entry => entry.Result)
            .ToArray();

        var totalBlocked = blockedResults.Sum(static result => result.BlockedDamage);
        var thorns = totalBlocked / DynamicVars[BlockThresholdKey].IntValue;
        if (thorns > 0)
            await PowerCmd.Apply<ThornsPower>(choiceContext, Owner.Creature, thorns, Owner.Creature, this);

        var hitCount = blockedResults.Length;
        if (hitCount <= 0) return;

        monster.SetMoveImmediate(CreateOneDamageMove(monster, hitCount), forceTransition: true);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars[BlockThresholdKey].UpgradeValueBy(-5m);
    }

    private Creature? PickRandomTeammate()
    {
        var allies = CombatState.GetTeammatesOf(Owner.Creature)
            .Where(creature => creature is { IsAlive: true } && creature != Owner.Creature)
            .ToArray();

        if (allies.Length == 0)
            return Owner.Creature;

        return Owner.RunState.Rng.CombatTargets.NextItem(allies);
    }

    private IEnumerable<DamageReceivedEntry> GetDamageReceivedSince(int entryCount)
    {
        return CombatManager.Instance.History.Entries
            .Skip(entryCount)
            .OfType<DamageReceivedEntry>();
    }

    private MoveState CreateOneDamageMove(MonsterModel monster, int hitCount)
    {
        return new MoveState(
            $"MANOSABA_LIN_ANANLIN_TAKE_THE_HIT_{hitCount}",
            async _ =>
            {
                await DamageCmd.Attack(DynamicVars[IntentDamageKey].BaseValue)
                    .FromMonster(monster)
                    .WithHitCount(hitCount)
                    .Execute(null);
            },
            new MultiAttackIntent((int)DynamicVars[IntentDamageKey].BaseValue, hitCount));
    }
}
