using Godot;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System;

namespace ManosabaLin.Characters.Sherrylin.Orbs;

[RegisterOrb]
public sealed class EmotionDesolateOrb : EmotionOrb<EmotionDesolate>
{
    private decimal _savedBlock;

    protected override Color OrbColor => new(0.3f, 0.3f, 0.8f);

    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
    {
        if (card.Owner?.Creature != Owner.Creature) return true;
        if (autoPlayType != AutoPlayType.None) return true;
        if (card.Type is CardType.Attack or CardType.Power) return false;
        return true;
    }

    public override async Task BeforeTurnEndOrbTrigger(PlayerChoiceContext ctx)
    {
        _savedBlock = Math.Floor(Owner.Creature.Block / 2m);
        var healAmount = Math.Floor(Owner.Creature.Block / 2m);
        if (healAmount > 0)
            await CreatureCmd.Heal(Owner.Creature, healAmount);
    }

    public override async Task AfterTurnStartOrbTrigger(PlayerChoiceContext ctx)
    {
        if (_savedBlock > 0)
        {
            await CreatureCmd.GainBlock(Owner.Creature, _savedBlock, ValueProp.Unpowered, null);
            _savedBlock = 0;
        }

        await OrbCmd.EvokeNext(ctx, Owner);
    }
}