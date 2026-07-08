using ManosabaLin.Characters.Ema.Cards;
using ManosabaLin.Characters.Hiro.Cards;
using ManosabaLin.Characters.Sherrylin.Cards;

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
        var previousAmount = currentAmount - amount;

        // 只在增加时扣力量，每2层扣1
        if (amount > 0)
        {
            var oldThreshold = (int)(previousAmount / 2);
            var newThreshold = (int)(currentAmount / 2);
            var strengthLoss = (newThreshold - oldThreshold) * StrengthLossPerTwoStacks;

            if (strengthLoss > 0)
                await PowerCmd.Apply<StrengthPower>(
                    choiceContext,
                    Owner,
                    -strengthLoss,
                    Owner,
                    null,
                    false
                );
        }

        // 达到阈值时触发
        if (currentAmount >= TokenThreshold && !_tokenGiven && !_isRestoring)
        {
            _tokenGiven = true;

            if (Owner.IsPlayer)
                await GiveBadEndingCurse();
            else
                await RemoveBuffsAndPrepareRestore();
        }
    }

    private async Task RemoveBuffsAndPrepareRestore()
    {
        if (Owner?.CombatState == null) return;

        var removed = new List<PowerSnapShot>();

        foreach (var p in Owner.Powers.ToList().Where(p => p.Type == PowerType.Buff))
        {
            removed.Add(new PowerSnapShot(Owner, p.Id, p.Amount));
            await PowerCmd.Remove(p);
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
                    choiceContext,
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

        var characterType = Owner.Player.Character?.GetType();
        ModelId curseModelId;

        if (characterType == typeof(Emalin.Emalin))
            curseModelId = ModelDb.GetId<EmaForgottenOne>();
        else if (characterType == typeof(Sherrylin.Sherrylin))
            curseModelId = ModelDb.GetId<Sherrybadending>();
        else
            curseModelId = ModelDb.GetId<HiroBadEnding>();

        var curseModel = ModelDb.GetById<CardModel>(curseModelId);
        var curseCard = Owner.CombatState.CreateCard(curseModel, Owner.Player);
        await CardPileCmd.AddGeneratedCardToCombat(curseCard, PileType.Hand, Owner.Player);
    }

    private record PowerSnapShot(Creature Owner, ModelId PowerId, int Amount);
}
