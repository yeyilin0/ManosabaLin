using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;

namespace ManosabaLin.Characters.Sherrylin.Powers;

[RegisterPower]
public sealed class ThreeCardOmenPower : ManosabaPowerTemplate
{
    private CardType? _currentType;
    private int _streak;
    private bool _triggered;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (_triggered) return;
        if (cardPlay.Card.Owner.Creature != Owner) return;

        if (_currentType == cardPlay.Card.Type)
            _streak++;
        else
        {
            _currentType = cardPlay.Card.Type;
            _streak = 1;
        }

        if (_streak < 3) return;

        _triggered = true;
        Flash();

        if (_currentType == CardType.Attack)
            await PowerCmd.Apply<MindCostDrawPower>(choiceContext, Owner, 1m, Owner, null, false);
        else if (_currentType == CardType.Skill)
            await PowerCmd.Apply<TheDancingMenPower>(choiceContext, Owner, 1m, Owner, null, false);
        else if (_currentType == CardType.Power)
            await PowerCmd.Apply<FinalFarewellPower>(choiceContext, Owner, 1m, Owner, null, false);
    }

    public override async Task BeforeSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Side) return;
        await PowerCmd.Remove(this);
    }
}
