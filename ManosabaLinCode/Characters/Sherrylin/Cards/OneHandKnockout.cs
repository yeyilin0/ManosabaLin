using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(SherrylinCardPool))]
public sealed class OneHandKnockout() : ManosabaCardTemplate(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyPlayer)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<BufferPower>(1m),
        new EnergyVar(2)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<BufferPower>(); }
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var target = cardPlay.Target ?? source.Owner.Creature;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        await PowerCmd.Apply<BufferPower>(
            choiceContext, target,
            source.DynamicVars["BufferPower"].IntValue,
            source.Owner.Creature, source, false);

        await PlayerCmd.GainEnergy(
            source.DynamicVars.Energy.IntValue,
            source.Owner);

        PlayerCmd.EndTurn(target.Player, false);

        await CardPileCmd.RemoveFromCombat(source);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["BufferPower"].UpgradeValueBy(1m);
    }
}
