using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Sherrylin.Powers;

/// <summary>
/// 双相升华能力：每打出5张手卡，随机升级一张手卡（升级后额外升级一张抽牌堆卡）。
/// </summary>
[RegisterPower]
public sealed class DualAscensionPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public bool SourceUpgraded { get; set; }

    private int _cardsPlayed;

    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner) return;

        _cardsPlayed++;

        if (_cardsPlayed < Amount) return;

        _cardsPlayed = 0;
        Flash();

        var rng = Owner.Player.RunState.Rng.CombatCardSelection;

        // 随机升级一张手卡
        var hand = PileType.Hand.GetPile(Owner.Player).Cards.ToList();
        if (hand.Count > 0)
        {
            var target = hand[rng.NextInt(hand.Count)];
            CardCmd.Upgrade(target);
        }

        // 升级后额外升级一张抽牌堆卡
        if (SourceUpgraded)
        {
            var drawPile = PileType.Draw.GetPile(Owner.Player).Cards.ToList();
            if (drawPile.Count > 0)
            {
                var drawTarget = drawPile[rng.NextInt(drawPile.Count)];
                CardCmd.Upgrade(drawTarget);
            }
        }
    }
}
