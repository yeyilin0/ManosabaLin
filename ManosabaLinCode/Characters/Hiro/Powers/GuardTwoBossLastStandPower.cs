using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public sealed class GuardTwoBossLastStandPower : ManosabaPowerTemplate
{
    private const int BlockMultiplier = 3;
    private const decimal ReviveHp = 1m;
    private const decimal HealRatio = 0.5m;

    private sealed class Data
    {
        public bool TriggeredThisTurn;
        public bool PendingHeal;
    }

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    protected override object InitInternalData()
    {
        return new Data();
    }

    private Data State => GetInternalData<Data>();

    public override bool ShouldDie(Creature creature)
    {
        if (creature != Owner)
            return true;
        return State.TriggeredThisTurn;
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

        await CreatureCmd.SetCurrentHp(creature, ReviveHp);
        await CreatureCmd.GainBlock(creature, block, ValueProp.Move, null);

        // 给每个玩家一张 MagicAbsorption
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

        await CreatureCmd.SetCurrentHp(Owner, Owner.MaxHp * HealRatio);
    }
}
