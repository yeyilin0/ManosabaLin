using ManosabaLin.Characters.Hiro.Powers;

namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinButterflyTalismanPower : ManosabaPowerTemplate
{
    private Creature? _markedEnemy;
    private int _silenceOnTrigger;
    private decimal _recordedDamage;
    private bool _triggered;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    internal void Arm(Creature markedEnemy, int silenceOnTrigger)
    {
        _markedEnemy = markedEnemy;
        _silenceOnTrigger = silenceOnTrigger;
        _recordedDamage = 0;
        _triggered = false;
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (_triggered) return;
        if (target != Owner) return;
        if (_markedEnemy is null || dealer != _markedEnemy) return;
        if (result.TotalDamage <= 0) return;

        _triggered = true;
        Flash();

        var amountToRecord = Math.Min(Amount, result.TotalDamage);
        if (Owner.GetPower<CrimsonbutterflyPower>() is { } butterfly)
        {
            butterfly.RecordExtraDamage(amountToRecord);
        }
        else
        {
            _recordedDamage += amountToRecord;
        }

        if (_silenceOnTrigger > 0)
            await PowerCmd.Apply<SilentPower>(choiceContext, Owner, _silenceOnTrigger, Owner, cardSource);

        if (_recordedDamage <= 0)
            await PowerCmd.Remove(this);
    }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (side != Owner.Side) return;

        if (_recordedDamage > 0)
        {
            Flash();
            await CreatureCmd.Heal(Owner, _recordedDamage);
            _recordedDamage = 0;
        }

        await PowerCmd.Remove(this);
    }
}
