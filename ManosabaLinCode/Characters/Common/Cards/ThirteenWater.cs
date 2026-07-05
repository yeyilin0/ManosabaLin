using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(LinCardPool))]
public sealed class ThirteenWater() : ManosabaCardTemplate(-1, CardType.Skill, CardRarity.Ancient, TargetType.Self)
{
    protected override IEnumerable<ICardComponent> CanonicalComponents =>
        [new UniqueComponent()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        var boss = source.CombatState.Enemies.FirstOrDefault(e =>
            e.GetPower<ThirteenWaterIntelPower>() != null);
        if (boss != null)
        {
            var intelPower = boss.GetPower<ThirteenWaterIntelPower>();
            if (intelPower != null)
                await PowerCmd.Remove(intelPower);
            boss.SetCurrentHpInternal(boss.CurrentHp / 2);
        }

        foreach (var player in source.CombatState.Players)
        {
            if (player == source.Owner) continue;
            var hand = PileType.Hand.GetPile(player);
            var cards = hand.Cards.Where(c => c is ThirteenWater).ToList();
            foreach (var card in cards)
                await CardPileCmd.RemoveFromCombat(card);
        }
    }
}
