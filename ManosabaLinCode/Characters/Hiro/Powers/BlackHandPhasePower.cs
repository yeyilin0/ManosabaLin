using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Emalin.Components;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ManosabaLin.Extensions;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public class BlackHandPhasePower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Side) return;
        if (Owner?.Player == null) return;

        var player = Owner.Player;
        var rng = player.RunState.Rng.CombatCardSelection;

        // 给手牌、抽牌堆、弃牌堆中一半的卡添加黑手组件
        var allCards = PileType.Draw.GetPile(player).Cards
            .Concat(PileType.Hand.GetPile(player).Cards)
            .Concat(PileType.Discard.GetPile(player).Cards)
            .Where(c => !c.HasComponent<BlackHandComponent>())
            .Distinct()
            .ToList();

        var halfCount = Math.Max(1, allCards.Count / 2);
        var cardsToMark = allCards.OrderBy(_ => rng.NextDouble()).Take(halfCount);

        foreach (var card in cardsToMark)
            card.TryAddComponent(new BlackHandComponent());

        // 回合结束时移除自身
        await PowerCmd.Remove<BlackHandPhasePower>(Owner);
    }
}