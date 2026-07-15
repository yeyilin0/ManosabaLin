using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinMargaretVoiceMagicPower : ManosabaPowerTemplate
{
    private const char Separator = '|';

    [SavedProperty] public string RecordedIntentKeys { get; set; } = string.Empty;
    [SavedProperty] public string SeenIntentKeys { get; set; } = string.Empty;

    private bool _shouldRemoveAfterCancellation;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public void RecordCurrentEnemyIntents(ICombatState? combatState)
    {
        if (combatState is null)
        {
            RecordedIntentKeys = string.Empty;
            SeenIntentKeys = string.Empty;
            return;
        }

        RecordedIntentKeys = string.Join(
            Separator,
            combatState.Enemies
                .Where(static enemy => enemy.IsAlive)
                .Select(TryGetIntentKey)
                .OfType<string>()
                .Distinct(StringComparer.Ordinal));
        SeenIntentKeys = string.Empty;
    }

    public override decimal ModifyHpLostAfterOsty(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner || amount <= 0 || dealer is null)
            return amount;

        var key = TryGetIntentKey(dealer);
        if (key is null || !ContainsKey(RecordedIntentKeys, key))
            return amount;

        if (!ContainsKey(SeenIntentKeys, key))
        {
            SeenIntentKeys = AppendKey(SeenIntentKeys, key);
            return amount;
        }

        _shouldRemoveAfterCancellation = true;
        Flash();
        return 0m;
    }

    public override async Task AfterModifyingHpLostAfterOsty()
    {
        if (!_shouldRemoveAfterCancellation) return;

        _shouldRemoveAfterCancellation = false;
        await PowerCmd.Remove(this);
    }

    private static string? TryGetIntentKey(Creature creature)
    {
        if (creature.Monster is not { NextMove: { } move }) return null;

        return $"{creature.Monster.Id.Entry}:{move.StateId}:{BuildIntentShape(move)}";
    }

    private static string BuildIntentShape(MoveState move)
    {
        return string.Join(",", move.Intents.Select(static intent => intent.IntentType.ToString()));
    }

    private static bool ContainsKey(string keys, string key)
    {
        return keys.Split(Separator, StringSplitOptions.RemoveEmptyEntries).Contains(key, StringComparer.Ordinal);
    }

    private static string AppendKey(string keys, string key)
    {
        return string.IsNullOrWhiteSpace(keys)
            ? key
            : $"{keys}{Separator}{key}";
    }
}
