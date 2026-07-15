using ManosabaLin.Characters.Ananlin.Cards;
using ManosabaLin.Characters.Ananlin.Relics;

namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinDoorGapConfirmationPower : ManosabaPowerTemplate
{
    private CardModel? _canonicalCard;
    private int _upgradeLevel;
    private int _bonusBlock;
    private int _attacksPlayedWhenArmed;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;
    protected override bool IsVisibleInternal => false;

    internal void Track(CardModel card, int bonusBlock, int attacksPlayedWhenArmed)
    {
        _canonicalCard = card.CanonicalInstance;
        _upgradeLevel = card.CurrentUpgradeLevel;
        _bonusBlock = bonusBlock;
        _attacksPlayedWhenArmed = attacksPlayedWhenArmed;
        Amount = 1;
    }

    public override async Task AfterAutoPostPlayPhaseEntered(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner) return;

        if (_canonicalCard is null || HasPlayedAttackSinceArmed(player))
        {
            await PowerCmd.Remove(this);
            return;
        }

        var copy = CombatState.CreateCard(_canonicalCard, player);
        for (var i = 0; i < _upgradeLevel; i++)
            CardCmd.Upgrade(copy);

        if (_bonusBlock > 0)
        {
            var bonus = await PowerCmd.Apply<AnanlinDoorGapBlockBonusPower>(
                choiceContext,
                Owner,
                _bonusBlock,
                Owner,
                copy);
            bonus?.Track(copy, _bonusBlock);
        }

        Flash();
        await AnanlinCardHelpers.ResolveAsFreeCardEffect(choiceContext, copy, skipCardPileVisuals: false);
        await PowerCmd.Remove(this);
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side == Owner.Side)
            await PowerCmd.Remove(this);
    }

    private bool HasPlayedAttackSinceArmed(Player player)
    {
        var sketchbook = player.Relics.OfType<AnansSketchbook>().FirstOrDefault();
        return sketchbook is not null && sketchbook.AttacksPlayedThisTurn > _attacksPlayedWhenArmed;
    }
}
