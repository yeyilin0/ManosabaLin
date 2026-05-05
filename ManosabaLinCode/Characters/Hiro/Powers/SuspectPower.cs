using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public sealed class SuspectPower : ManosabaPowerTemplate
{
    private const int TokenThreshold = 12;
    private const int StrengthLossPerTwoStacks = 1;

    private readonly List<(Creature owner, ModelId powerId, int amount)> _removedPowers = new();

    private int _accumulatedStacks;
    private bool _isRestoring;
    private bool _tokenGiven;

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => true;

    [SavedProperty]
    public int AccumulatedStacks
    {
        get => _accumulatedStacks;
        set
        {
            AssertMutable();
            _accumulatedStacks = value;
        }
    }

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power != this) return;

        var currentAmount = (int)power.Amount;

        _accumulatedStacks = currentAmount;

        var strengthLoss = currentAmount / 2 * StrengthLossPerTwoStacks;

        if (strengthLoss > 0)
            await PowerCmd.Apply<StrengthPower>(
                new ThrowingPlayerChoiceContext(),
                Owner,
                -strengthLoss,
                Owner,
                null,
                false
            );

        if (currentAmount >= TokenThreshold && !_tokenGiven && !_isRestoring)
        {
            _tokenGiven = true;
            await RemovePowersFromOwnerAndPrepareRestore();

            if (Owner?.Player != null) await GiveBadEndingCurse();
        }
    }

    private async Task RemovePowersFromOwnerAndPrepareRestore()
    {
        if (Owner?.CombatState == null) return;

        _removedPowers.Clear();

        var creature = Owner;

        foreach (var power in creature.Powers.ToList())
        {
            if (power is SuspectPower) continue;

            _removedPowers.Add((creature, power.Id, power.Amount));
            await PowerCmd.Remove(power);
        }

        _isRestoring = true;
    }

    // ★ 我方回合结束时归还能力
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (!_isRestoring) return;
        if (side != CombatSide.Player) return;

        foreach (var (creature, powerId, amount) in _removedPowers)
            if (!creature.IsDead)
            {
                var powerModel = ModelDb.GetById<PowerModel>(powerId);
                if (powerModel != null)
                    await PowerCmd.Apply(
                        new ThrowingPlayerChoiceContext(),
                        powerModel.ToMutable(0),
                        creature,
                        amount,
                        Owner,
                        null,
                        false
                    );
            }

        _removedPowers.Clear();
        _isRestoring = false;

        await PowerCmd.Remove((PowerModel)this);
    }

    private async Task GiveBadEndingCurse()
    {
        if (Owner?.Player == null) return;
        if (Owner.CombatState == null) return;

        var curseModel = ModelDb.GetById<CardModel>(ModelDb.GetId<HiroBadEnding>());
        if (curseModel == null) return;

        var curseCard = Owner.CombatState.CreateCard(curseModel, Owner.Player);
        await CardPileCmd.AddGeneratedCardToCombat(curseCard, PileType.Hand, Owner.Player);
    }
}