using ManosabaLin.Characters.Ananlin.Cards;

namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinSoftAgreementPower : ManosabaPowerTemplate
{
    private CardModel? _card;
    private int _heal;
    private bool _pending = true;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;
    protected override bool IsVisibleInternal => false;

    internal void Track(CardModel card, int heal)
    {
        _card = card;
        _heal = heal;
        Amount = Math.Max(1, heal);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card != _card) return;

        if (!_pending && _heal > 0)
        {
            Flash();
            await CreatureCmd.Heal(Owner, _heal);
        }

        await PowerCmd.Remove(this);
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Side) return;

        if (_pending)
        {
            _pending = false;
            Flash();
            await PowerCmd.Apply<AnanlinPeaceOfMindPower>(choiceContext, Owner, 1, Owner, _card);
            return;
        }

        await PowerCmd.Remove(this);
    }
}
