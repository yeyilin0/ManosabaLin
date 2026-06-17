using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System;
using System.Linq;
using System.Threading.Tasks;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;

namespace ManosabaLin.Characters.Sherrylin.Orbs;

[RegisterOrb]
public sealed class EmotionFearOrb : EmotionOrb<EmotionFear>
{
    protected override Color OrbColor => new(0.5f, 0.2f, 0.7f);

    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
    {
        if (card.Owner?.Creature != Owner.Creature) return true;
        if (autoPlayType != AutoPlayType.None) return true;
        if (card.Type == CardType.Attack) return false;
        return true;
    }

    public override async Task BeforeTurnEndOrbTrigger(PlayerChoiceContext ctx)
    {
        var halfBlock = Math.Floor(Owner.Creature.Block / 2m);
        if (halfBlock <= 0) return;

        var allies = Owner.Creature.CombatState.GetTeammatesOf(Owner.Creature)
            .Where(c => c.IsAlive)
            .Append(Owner.Creature)
            .ToList();

        var rng = Owner.Creature.CombatState.RunState.Rng.CombatCardSelection;
        var target = allies[rng.NextInt(allies.Count)];

        await CreatureCmd.GainBlock(target, halfBlock, ValueProp.Unpowered, null);
    }
}