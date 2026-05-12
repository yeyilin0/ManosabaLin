using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Hiro.Relics;

[RegisterRelic(typeof(HiroRelicPool))]
public sealed class Magicalsketchbook : ManosabaRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new PowerVar<CrimsonbutterflyPower>(3m); }
    }

    public override async Task AfterSideTurnStart(CombatSide side, ICombatState combatState)
    {
        if (side != Owner.Creature.Side) return;
        if (combatState.RoundNumber != 1) return;

        Flash();

        await PowerCmd.Apply<CrimsonbutterflyPower>(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars["CrimsonbutterflyPower"].BaseValue,
            Owner.Creature,
            null);
    }
}
