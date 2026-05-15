using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Emalin.Enchantments;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace ManosabaLin.Characters.Ema.Powers;

[RegisterPower]
public class PrisonBlueprintPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature != Owner) return;
        if (cardPlay.Card.Enchantment is not Agreement) return;

        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Move, cardPlay);

        foreach (var ally in Owner.CombatState.Allies.Where(a => a is { IsAlive: true } && a != Owner))
            await CreatureCmd.GainBlock(ally, 2m * Amount, ValueProp.Move, cardPlay);
    }
}
