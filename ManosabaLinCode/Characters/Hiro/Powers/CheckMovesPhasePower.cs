using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Monsters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Helpers;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public class CheckMovesPhasePower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;

    private int _failDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 10, 8);
    private int _withAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 40, 30);

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
                choiceContext, Owner, _failDamage,
                ValueProp.Unpowered | ValueProp.Move, Owner, null);

            var boss = Owner.CombatState?.Enemies
                .OfType<Creature>()
                .FirstOrDefault(c => c.GetPower<GuardTwoBossPhasePower>() != null);

            if (boss != null)
            {
                await PowerCmd.Apply<WithPower>(
                    new ThrowingPlayerChoiceContext(), boss, _withAmount, boss, null, false);
            }
        }

        _playedTypes.Clear();

        // 回合结束时移除自身
        await PowerCmd.Remove<CheckMovesPhasePower>(Owner);
    }
}