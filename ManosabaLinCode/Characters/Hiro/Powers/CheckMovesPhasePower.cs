using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Helpers;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public class CheckMovesPhasePower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;

    private int FailDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 10, 8);
    private int WithAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 40, 30);

    private readonly HashSet<CardType> _playedTypes = new();

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner) return;
        _playedTypes.Add(cardPlay.Card.Type);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Side) return;

        var hasAll = _playedTypes.Contains(CardType.Attack)
                  && _playedTypes.Contains(CardType.Skill)
                  && _playedTypes.Contains(CardType.Power);

        if (!hasAll)
        {
            await CreatureCmd.Damage(
                choiceContext, Owner, FailDamage,
                ValueProp.Unpowered | ValueProp.Move, null, null);

            var boss = Owner.CombatState?.Enemies
                .FirstOrDefault(c => c.GetPower<GuardTwoBossLastStandPower>() != null);

            if (boss != null)
            {
                await PowerCmd.Apply<WithPower>(
                    new ThrowingPlayerChoiceContext(), boss, WithAmount, boss, null, false);
            }
        }

        _playedTypes.Clear();

        // 回合结束时移除自身
        await PowerCmd.Remove<CheckMovesPhasePower>(Owner);
    }
}
