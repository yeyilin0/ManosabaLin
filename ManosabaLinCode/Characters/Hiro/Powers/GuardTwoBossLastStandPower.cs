using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public sealed class GuardTwoBossLastStandPower : ManosabaPowerTemplate
{
    private const int BlockMultiplier = 3;
    private const decimal InfiniteHp = 999999999m;
    private const decimal HealRatio = 0.5m;

    private sealed class Data
    {
        public bool TriggeredThisTurn;
        public bool PendingHeal;
        public decimal StartingMaxHp;
        public bool StartingMaxHpInitialized;
    }

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    protected override object InitInternalData()
    {
        return new Data();
    }

    private Data State => GetInternalData<Data>();

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        InitializeStartingMaxHp();
        return Task.CompletedTask;
    }

    private void InitializeStartingMaxHp()
    {
        if (State.StartingMaxHpInitialized)
            return;

        State.StartingMaxHp = Owner.MaxHp;
        State.StartingMaxHpInitialized = true;
    }

    public override bool ShouldDie(Creature creature)
    {
        if (creature != Owner)
            return true;
        return false;
    }

    public override async Task BeforeDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        Decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner) return;
        if (!State.TriggeredThisTurn) return;
        if (target.Block > 0) return;

        Flash();
        await CreatureCmd.Kill(Owner, force: true);
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature != Owner)
        {
            return;
        }

        State.TriggeredThisTurn = true;
        State.PendingHeal = true;

        var block = (Owner.GetPower<WithPower>()?.Amount ?? 0) * BlockMultiplier;

        InitializeStartingMaxHp();
        await CreatureCmd.SetMaxAndCurrentHp(creature, InfiniteHp);
        creature.HpDisplay = HpDisplay.InfiniteWithoutNumbers;
        await CreatureCmd.GainBlock(creature, block, ValueProp.Move, null);

        var magicAbsorption = ModelDb.GetById<CardModel>(new ModelId("CARD", "MANOSABA_LIN_CARD_MAGIC_ABSORPTION"));
        if (magicAbsorption != null)
        {
            foreach (var player in Owner.CombatState.Players)
            {
                var card = Owner.CombatState.CreateCard(magicAbsorption, player);
                await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
            }
        }

        if (creature.Monster?.MoveStateMachine?.States.TryGetValue("ATTACK_4_MOVE", out var move) == true &&
            move is MoveState moveState)
        {
            creature.Monster.SetMoveImmediate(moveState);
        }
    }

    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != Owner.Side || !State.PendingHeal) return;

        State.PendingHeal = false;
        State.TriggeredThisTurn = false;

        InitializeStartingMaxHp();
        Owner.HpDisplay = HpDisplay.Normal;
        await CreatureCmd.SetMaxHp(Owner, State.StartingMaxHp);
        await CreatureCmd.SetCurrentHp(Owner, Owner.MaxHp * HealRatio);
    }
}
