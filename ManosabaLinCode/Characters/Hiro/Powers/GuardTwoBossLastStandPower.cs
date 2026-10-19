using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using ManosabaLin.Characters.Hiro.Monsters;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public sealed class GuardTwoBossLastStandPower : ManosabaPowerTemplate
{
    private const int BlockMultiplier = 3;
    private const decimal InfiniteHp = 999999999m;
    private const decimal HealRatio = 0.5m;

    /// <summary>整场首次拦截时留一条日志，便于把「未拦截」与「拦截被抢」两类问题分开定性。</summary>
    private bool _loggedFirstIntercept;

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
        if (!_loggedFirstIntercept)
        {
            _loggedFirstIntercept = true;
            Log.Info("[GuardTwoBoss] Last-stand intercepting a lethal hit (first time this combat).");
        }
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
        if (!props.HasFlag(ValueProp.Move)) return;

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
        if (creature.Monster is GuardTwoBossMonster boss)
            boss.PlayLastStandMusic();

        // 诈尸护盾不能只按 3×【魔女化】算：BOSS 在打出 ATTACK_3（首次获得【魔女化】）之前
        // 就可能被即死类效果打空血，此时 3×0 = 0；而紧接着的 BeforeDamageReceived 判定是
        // 「TriggeredThisTurn && Block <= 0 && Move ⇒ Kill(force: true)」——
        // 也就是说诈尸刚成立，下一击就会被当成「已破盾」直接真杀。
        // 因此护盾取 max(3×【魔女化】, 该 BOSS 的常规护盾值)，保证诈尸一定带盾、必须破盾才能击杀。
        var withAmount = Owner.GetPower<WithPower>()?.Amount ?? 0;
        var shieldFloor = creature.Monster is GuardTwoBossMonster guard ? guard.ShieldAmount : 0;
        var block = Math.Max(withAmount * BlockMultiplier, shieldFloor);

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
