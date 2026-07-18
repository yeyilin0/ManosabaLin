using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Emalin;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(LinCardPool))]
public sealed class Revivefragment : ManosabaCardTemplate
{
    public Revivefragment() : base(0, CardType.Status, CardRarity.Token, TargetType.None) { }

    public override int MaxUpgradeLevel => 0;

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    protected override async Task AfterCardDrawn(
        PlayerChoiceContext choiceContext,
        CardModel card,
        bool fromHandDraw, ComponentContext componentContext)
    {
        if (card != this) return;

        await Cmd.Wait(0.25f);

        var allies = Owner.Creature.CombatState.Allies
            .Where(a => a.IsAlive)
            .ToList();

        foreach (var ally in allies)
            await CreatureCmd.Heal(ally, 1m);

        await CardPileCmd.Draw(choiceContext, 1m, Owner);

        await CardPileCmd.RemoveFromCombat(this);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        await CardPileCmd.Draw(choiceContext, 1m, Owner);
        await CardPileCmd.RemoveFromCombat(this);
    }

    protected override CardLocation GetResultLocationForCardPlayC()
    {
        var resultLocation = base.GetResultLocationForCardPlayC();
        return new CardLocation(resultLocation.player, PileType.None, resultLocation.position);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
    }
}
