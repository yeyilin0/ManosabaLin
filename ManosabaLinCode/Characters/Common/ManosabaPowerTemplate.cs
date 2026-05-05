using Godot;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace ManosabaLin.Characters.Common;

public abstract class ManosabaPowerTemplate : ModPowerTemplate
{
    public override PowerAssetProfile AssetProfile
    {
        get
        {
            var fileName = $"{GetType().Name.ToLowerInvariant()}.png";

            var icon = fileName.PowerImagePath();
            var resolvedIcon = ResourceLoader.Exists(icon) ? icon : "power.png".PowerImagePath();

            var big = fileName.BigPowerImagePath();
            var resolvedBig = ResourceLoader.Exists(big) ? big : "power.png".BigPowerImagePath();

            return new PowerAssetProfile(resolvedIcon, resolvedBig);
        }
    }

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power.Amount < 0 && !power.AllowNegative)
            power.RemoveInternal();
    }
}