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

    private int _lastAffinity;
    private int _lastEstrangement;

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        var bond = Owner.GetPower<BondPower>();
        if (bond != null)
        {
            _lastAffinity = bond.Affinity;
            _lastEstrangement = bond.Estrangement;
        }
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;

        var bond = Owner.GetPower<BondPower>();
        if (bond == null) return;

        var affinityDelta = bond.Affinity - _lastAffinity;
        var estrangementDelta = bond.Estrangement - _lastEstrangement;

        // 亲近增加 → 恢复能量，增加亚里沙的魔法
        if (affinityDelta > 0)
        {
            Owner.Player.PlayerCombatState.Energy += affinityDelta;
            await PowerCmd.Apply<YlsmPower>(
                choiceContext, Owner, affinityDelta, Owner, null, false);
        }

        // 疏远增加 → 消耗亚里沙的魔法，抽牌
        if (estrangementDelta > 0)
        {
            var magic = Owner.GetPower<YlsmPower>();
            if (magic != null)
            {
                for (int i = 0; i < estrangementDelta; i++)
                {
                    if (magic.Amount > 0)
                    {
                        magic.Amount--;
                        await CardPileCmd.Draw(choiceContext, 1, Owner.Player);
                    }
                }
            }
        }

        _lastAffinity = bond.Affinity;
        _lastEstrangement = bond.Estrangement;
    }
}