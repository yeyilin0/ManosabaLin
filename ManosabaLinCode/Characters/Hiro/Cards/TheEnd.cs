using MinionLib.Component.Core;
﻿using ManosabaLin.Audio;
using ManosabaLin.Characters.Common;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(LinCardPool))]
public sealed class TheEnd : ManosabaCardTemplate
{
    public TheEnd() : base(0, CardType.Skill, CardRarity.Ancient, TargetType.None)
    {
    }

    protected override HashSet<CardTag> CanonicalTags => new();

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new DynamicVar("KillCount", 0); }
    }

    [SavedProperty] public bool HasBeenPlayed { get; set; }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        ManosabaAudio.TryPlayOneShot("the_end_theme.mp3".BgmAudioPath());

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        var enemies = source.CombatState.Creatures
            .Where(c => !c.IsPlayer && c.IsAlive && c.IsHittable)
            .ToList();

        foreach (var enemy in enemies)
        {
            await CreatureCmd.Kill(enemy);
            await Cmd.Wait(0.1f);
        }

        source.DynamicVars["KillCount"].BaseValue = enemies.Count;

        HasBeenPlayed = true;
    }

    protected override async Task AfterCombatEnd(CombatRoom _, ComponentContext componentContext)
    {
        var source = this;

        if (!HasBeenPlayed)
            return;

        var deckCards = PileType.Deck.GetPile(source.Owner).Cards.ToList();
        foreach (var card in deckCards)
            if (card is DeathRewind)
                await CardPileCmd.RemoveFromDeck(card);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}