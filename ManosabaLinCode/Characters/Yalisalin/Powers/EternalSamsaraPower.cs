using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Yalisalin.Powers;

/// <summary>
///     永劫的轮回（传火·燎原库）：本回合打出的牌在回合结束时洗回抽牌堆。
///     消耗堆里的牌不捞回（烧掉的就烧掉了）；记录只留在本回合，回合结束随能力一起清空。
///     <para>
///     记录表是<b>实例字段</b>而非 SavedProperty：它完全由两端都会收到的 AfterCardPlayed 事件推导，
///     不参与联机同步也不会分歧（与「不作答」的 _playedAttackThisTurn 同款口径）。
///     </para>
/// </summary>
[RegisterPower]
public sealed class EternalSamsaraPower : ManosabaPowerTemplate
{
    private readonly List<CardModel> _playedThisTurn = [];

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner)
            return Task.CompletedTask;

        if (!_playedThisTurn.Contains(cardPlay.Card))
            _playedThisTurn.Add(cardPlay.Card);

        return Task.CompletedTask;
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Side)
            return;

        foreach (var card in _playedThisTurn)
        {
            if (card.HasBeenRemovedFromState)
                continue;

            if (card.Pile?.Type != PileType.Discard)
                continue;

            await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Random);
        }

        _playedThisTurn.Clear();
        await PowerCmd.Remove(this);
    }
}
