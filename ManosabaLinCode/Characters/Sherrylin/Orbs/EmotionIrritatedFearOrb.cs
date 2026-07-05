using Godot;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Sherrylin.Orbs;

[RegisterOrb]
public sealed class EmotionIrritatedFearOrb : EmotionOrb<EmotionIrritatedFear>
{
    protected override Color OrbColor => new(0.9f, 0.2f, 0.5f);

    public override bool ShouldPlay(CardModel card, AutoPlayType autoPlayType)
    {
        if (card.Owner?.Creature != Owner.Creature) return true;
        if (autoPlayType != AutoPlayType.None) return true;
        if (card.Type == CardType.Attack) return false;
        return true;
    }

    public override async Task BeforeTurnEndOrbTrigger(PlayerChoiceContext ctx)
    {
        await CreatureCmd.Damage(ctx, Owner.Creature, 3m, ValueProp.Unpowered, null, null);

        var blockAmount = Owner.Creature.Block;

        var allies = Owner.Creature.CombatState.GetTeammatesOf(Owner.Creature)
            .Where(c => c.IsAlive)
            .Append(Owner.Creature);

        foreach (var ally in allies)
            await CreatureCmd.GainBlock(ally, blockAmount, ValueProp.Unpowered, null);
    }
}