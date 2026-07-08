// ThirteenWaterIntelPower.cs
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using ManosabaLin.Characters.Common.Powers;
using ManosabaLin.Characters.Hiro.Monsters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public sealed class ThirteenWaterIntelPower : ManosabaPowerTemplate
{
    private const int IntelMaxPerTurn = 5;
    private const int WithPowerLossOnDeath = 20;
    private static int MaxHpIncrease => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 50, 40);

    private sealed class Data
    {
        public int DeathCount;
        public int LastTaskFailedCount;
        public Dictionary<ulong, int> PlayerIntelThisTurn = new();
        public decimal PreviousMaxHp;
        public bool Initialized;
    }

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    protected override object InitInternalData() => new Data();
    private Data State => GetInternalData<Data>();

    public int LastTaskFailedCount
    {
        get => State.LastTaskFailedCount;
        set => State.LastTaskFailedCount = value;
    }

    public void InitializePreviousMaxHp(decimal maxHp)
    {
        if (State.Initialized) return;

        State.PreviousMaxHp = maxHp;
        State.Initialized = true;
    }

    public override bool ShouldDie(Creature creature)
    {
        if (creature != Owner) return true;
        return false;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature != Owner) return;

        State.DeathCount++;

        if (State.DeathCount == 1 && Owner.GetPower<FusionStandPower>() == null)
        {
            await PowerCmd.Apply<FusionStandPower>(
                new ThrowingPlayerChoiceContext(), Owner, 1, Owner, null);
        }

        // 每次死亡减少20魔女化
        var withPower = Owner.GetPower<WithPower>();
        if (withPower != null)
        {
            if (withPower.Amount <= WithPowerLossOnDeath)
                await PowerCmd.Remove(withPower);
            else
                await PowerCmd.Apply<WithPower>(
                    new ThrowingPlayerChoiceContext(), Owner, -WithPowerLossOnDeath, Owner, null);
        }

        foreach (var player in Owner.CombatState.Players)
        {
            await CreatureCmd.Heal(player.Creature, player.Creature.MaxHp);
            await RemoveDebuffs(player.Creature);
        }

        foreach (var power in Owner.Powers.ToList())
        {
            if (power.Type == PowerType.Debuff && power != this)
                await PowerCmd.Remove(power);
        }

        if (!State.Initialized)
            InitializePreviousMaxHp(Owner.MaxHp);

        var newMaxHp = State.PreviousMaxHp + MaxHpIncrease;
        Owner.MaxHp = (int)Math.Min(newMaxHp, 999999999M);
        Owner.CurrentHp = Math.Min(Owner.CurrentHp, Owner.MaxHp);
        await CreatureCmd.SetCurrentHp(Owner, Owner.MaxHp);
        State.PreviousMaxHp = newMaxHp;

        await CreatureCmd.GainBlock(Owner, 50, ValueProp.Move, null);
        GuardThreeWrongTextVfx.SpawnPersistentWrong(Owner, 3);
    }

    private async Task RemoveDebuffs(Creature creature)
    {
        foreach (var power in creature.Powers.ToList())
        {
            if (power.Type == PowerType.Debuff)
                await PowerCmd.Remove(power);
        }
    }

    public override Decimal ModifyHpLostBeforeOsty(
        Creature target,
        Decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner) return amount;
        if (amount <= 0) return amount;
        if (!props.HasFlag(ValueProp.Move)) return amount;

        float baseChance = 0.2f + State.DeathCount * 0.1f;
        var rng = Owner.CombatState.RunState.Rng.CombatTargets;
        if (rng.NextFloat() >= baseChance) return amount;

        if (dealer?.Player == null) return amount;
        var player = dealer.Player;

        if (!State.PlayerIntelThisTurn.ContainsKey(player.NetId))
            State.PlayerIntelThisTurn[player.NetId] = 0;
        if (State.PlayerIntelThisTurn[player.NetId] >= IntelMaxPerTurn) return amount;

        State.PlayerIntelThisTurn[player.NetId]++;

        _ = PowerCmd.Apply<ThirteenWaterPlayerIntelPower>(
            new ThrowingPlayerChoiceContext(), dealer, 1, Owner, null);

        return amount;
    }

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != Owner.Side) return;
        State.PlayerIntelThisTurn.Clear();
    }
}
