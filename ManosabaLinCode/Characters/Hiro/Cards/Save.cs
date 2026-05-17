using MinionLib.Component.Core;
﻿using ManosabaLin.Audio;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(LinCardPool))]
public sealed class Save : ManosabaCardTemplate
{
    public Save() : base(0, CardType.Skill, CardRarity.Token, TargetType.AnyPlayer)
    {
    }

    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Minion };

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            yield return CardKeyword.Exhaust;
            yield return CardKeyword.Ethereal;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        ManosabaAudio.TryPlayOneShot("save_theme.mp3".BgmAudioPath());

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        var otherAlly = source.CombatState.GetTeammatesOf(source.Owner.Creature)
            .FirstOrDefault(c => c != source.Owner.Creature && c.IsAlive && c.IsPlayer);

        if (otherAlly != null)
            await PowerCmd.Apply<WithPower>(
                choiceContext,
                otherAlly,
                100m,
                source.Owner.Creature,
                source,
                false
            );
    }

    public static async Task<CardModel> CreateInHand(Player owner, ICombatState combatState)
    {
        var card = combatState.CreateCard<Save>(owner);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, owner);
        return card;
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
    }
}