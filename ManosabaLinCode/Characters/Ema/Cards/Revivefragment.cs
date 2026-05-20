using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Emalin;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

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

    // Auto-trigger when drawn: heal the killed ally, then vanish
    protected override async Task AfterCardDrawn(
        PlayerChoiceContext choiceContext,
        CardModel card,
        bool fromHandDraw, ComponentContext componentContext)
    {
        if (card != this) return;

        await Cmd.Wait(0.25f);

        // Owner.Creature is the killed ally (set at creation time)
        var target = Owner.Creature;
        if (target is { IsAlive: true })
        {
            await CreatureCmd.Heal(target, 1m);
        }

        // Vanish: remove from combat entirely
        await CardPileCmd.RemoveFromCombat(this);
    }

    // Also handle manual play (if player plays it from hand)
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var target = Owner.Creature;
        if (target is { IsAlive: true })
        {
            await CreatureCmd.Heal(target, 1m);
        }

        await CardPileCmd.RemoveFromCombat(this);
    }

    protected override PileType GetResultPileTypeForCardPlayC() => PileType.None;

    protected override void OnUpgrade(ComponentContext componentContext)
    {
    }
}
