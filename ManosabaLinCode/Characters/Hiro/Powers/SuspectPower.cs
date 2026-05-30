using ManosabaLin.Characters.Hiro.Cards;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public sealed class SuspectPower : ManosabaPowerTemplate
{
    private const int TokenThreshold = 12;
    private const int StrengthLossPerTwoStacks = 1;

    private IReadOnlyList<PowerSnapShot> RemovedPowers
    {
        get;
        set
        {
            AssertMutable();
            field = value;
        }
    } = [];

    private bool _isRestoring;
    private bool _tokenGiven;

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => true;

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power != this) return;

        var currentAmount = power.Amount;

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

            if (Owner.Player != null) await GiveBadEndingCurse();
        }
    }

    private async Task RemovePowersFromOwnerAndPrepareRestore()
    {
        if (Owner?.CombatState == null) return;

        var removed = new List<PowerSnapShot>();

        var creature = Owner;

        foreach (var power in creature.Powers.ToList().Where(p => p.Type == PowerType.Buff))
        {
            removed.Add(new PowerSnapShot(creature, power.Id, power.Amount));
            await PowerCmd.Remove(power);
        }

        RemovedPowers = removed;

        _isRestoring = true;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (!_isRestoring) return;
        if (side != CombatSide.Player) return;

        foreach (var (creature, powerId, amount) in RemovedPowers)
            if (!creature.IsDead)
            {
                var powerModel = ModelDb.GetById<PowerModel>(powerId);
                await PowerCmd.Apply(
                    new ThrowingPlayerChoiceContext(),
                    powerModel.ToMutable(0),
                    creature,
                    amount,
                    Owner,
                    null
                );
            }

        RemovedPowers = [];
        _isRestoring = false;

        await PowerCmd.Remove(this);
    }

    private async Task GiveBadEndingCurse()
    {
        if (Owner?.Player == null) return;
        if (Owner.CombatState == null) return;

        var curseModel = ModelDb.GetById<CardModel>(ModelDb.GetId<HiroBadEnding>());

        var curseCard = Owner.CombatState.CreateCard(curseModel, Owner.Player);
        await CardPileCmd.AddGeneratedCardToCombat(curseCard, PileType.Hand, Owner.Player);
    }

    private record PowerSnapShot(Creature Owner, ModelId PowerId, int Amount);
}
