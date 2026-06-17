using Godot;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;
using ManosabaLin.Characters.Sherrylin.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Orbs;

[RegisterOrb]
public sealed class EmotionElationOrb : EmotionOrb<EmotionElation>
{
    private int _drawCount;
    private bool _initialized;

    protected override Color OrbColor => new(1f, 0.9f, 0.3f);

    public override async Task AfterCardDrawn(
        PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card.Owner != Owner) return;

        if (!_initialized)
        {
            _initialized = true;
            var maxEnergy = Owner.Creature.Player?.MaxEnergy ?? 0;
            if (maxEnergy > 0)
            {
                await PlayerCmd.GainEnergy(maxEnergy, Owner);
                await PowerCmd.Apply<LoseEnergyPower>(
                    choiceContext, Owner.Creature, (int)maxEnergy,
                    Owner.Creature, null, false);
            }
            return;
        }

        _drawCount++;
        if (_drawCount >= 4)
        {
            _drawCount = 0;

            var losePower = Owner.Creature.GetPower<LoseEnergyPower>();
            if (losePower != null && losePower.Amount > 0)
            {
                losePower.Amount--;
                if (losePower.Amount <= 0)
                {
                    await PowerCmd.Remove(losePower);

                    var maxEnergy = Owner.Creature.Player?.MaxEnergy ?? 0;
                    if (maxEnergy > 0)
                    {
                        await PlayerCmd.GainEnergy(maxEnergy, Owner);
                        await PowerCmd.Apply<LoseEnergyPower>(
                            choiceContext, Owner.Creature, (int)maxEnergy,
                            Owner.Creature, null, false);
                    }
                }
            }
        }
    }
}