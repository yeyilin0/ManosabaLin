using Godot;
using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;
using ManosabaLin.Characters.Sherrylin.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Sherrylin.Orbs;

[RegisterOrb]
public sealed class EmotionCuriosityOrb : EmotionOrb<EmotionCuriosity>
{
    protected override Color OrbColor => new(0.6f, 0.9f, 0.6f);

    public override async Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? clonedBy)
    {
        if (card.Owner != Owner) return;

        var magnifyingGlass = Owner.Relics.OfType<MagnifyingGlass>().FirstOrDefault();
        if (magnifyingGlass == null || !magnifyingGlass.HasTriggeredThisCombat) return;

        var count = magnifyingGlass.CaseReversalDiscardToExhaustCount;
        if (count <= 0) return;
        magnifyingGlass.CaseReversalDiscardToExhaustCount = 0;

        await PowerCmd.Apply<XlmPower>(
            new ThrowingPlayerChoiceContext(), Owner.Creature, count, Owner.Creature, null, false);

        var xlmPower = Owner.Creature.GetPower<XlmPower>();
        if (xlmPower != null)
        {
            var drawCount = (int)(xlmPower.Amount / 4);
            if (drawCount > 0)
                await CardPileCmd.Draw(new ThrowingPlayerChoiceContext(), drawCount, Owner);
        }

        var exhaustCount = PileType.Exhaust.GetPile(Owner).Cards.Count;
        var energyGain = exhaustCount / 4;
        if (energyGain > 0)
            await PlayerCmd.GainEnergy(energyGain, Owner);
    }
}