using STS2RitsuLib.Interop.AutoRegistration;
using ManosabaLin.Characters.Ananlin.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public class Powerthreethree : ManosabaPowerTemplate
{
    private const int WithPowerPerCard = 50;
    private const int TurnsBeforeDeath = 3;

    [SavedProperty] public int TurnsRemaining { get; set; } = TurnsBeforeDeath;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        TurnsRemaining = TurnsBeforeDeath;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        var source = this;
        if (player != source.Owner.Player) return;
        if (source.Owner.IsDead) return;

        if (source.TurnsRemaining <= 1)
        {
            source.Flash();
            await CreatureCmd.Damage(
                choiceContext,
                source.Owner,
                source.Owner.CurrentHp,
                ValueProp.Unblockable | ValueProp.Unpowered,
                null,
                null);
            await PowerCmd.Remove(source);
            return;
        }

        var selectedCards = await SelectAccompliceAttackCards(choiceContext, player);

        source.TurnsRemaining--;
        if (selectedCards > 0)
            source.Flash();
    }

    private async Task<int> SelectAccompliceAttackCards(PlayerChoiceContext choiceContext, Player player)
    {
        var with = Owner.GetPower<WithPower>();
        var withAmount = with?.Amount ?? 0;
        var cardCount = (int)(withAmount / WithPowerPerCard);
        if (cardCount <= 0) return 0;

        var drawPile = PileType.Draw.GetPile(player);
        var attackCards = drawPile.Cards
            .Where(c => c.Type == CardType.Attack)
            .ToList();

        if (attackCards.Count == 0) return 0;

        var selectCount = Math.Min(cardCount, attackCards.Count);
        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, selectCount, selectCount);
        var selected = (await CardSelectCmd.FromSimpleGrid(choiceContext, attackCards, player, prefs)).ToList();

        foreach (var card in selected)
        {
            card.SetFreeIgnoringCardPlayConditions();
            await CardPileCmd.Add(card, PileType.Hand);
        }

        return selected.Count;
    }
}
