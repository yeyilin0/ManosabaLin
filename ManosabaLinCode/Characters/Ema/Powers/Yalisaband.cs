using Godot;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Threading.Tasks;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Models;

namespace ManosabaLin.Characters.Ema.Powers;

[RegisterPower]
public class Yalisabond : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    public async Task ApplyBondDeltaAsync(int affinityDelta, int estrangementDelta)
    {
        if (Owner?.Player == null) return;

        var choiceContext = new ThrowingPlayerChoiceContext();

        if (affinityDelta > 0)
        {
            await PlayerCmd.GainEnergy(affinityDelta, Owner.Player);
            await PowerCmd.Apply<YlsmPower>(
                choiceContext, Owner, affinityDelta, Owner, null, false);
        }

        if (estrangementDelta > 0)
        {
            var magic = Owner.GetPower<YlsmPower>();
            if (magic != null)
            {
                for (int i = 0; i < estrangementDelta; i++)
                {
                    if (magic.Amount <= 0) break;
                    magic.Amount--;
                    await CardPileCmd.Draw(choiceContext, 1, Owner.Player);
                }
            }
        }
    }

}
