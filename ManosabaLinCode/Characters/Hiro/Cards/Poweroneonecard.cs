using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class Poweroneonecard() : ManosabaCardTemplate(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("PerjuryCost", 2m)
    };

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PerjuryPower>();
            yield return HoverTipFactory.FromPower<Poweroneone>();
        }
    }

    protected override bool IsPlayable
    {
        get
        {
            if (!base.IsPlayable) return false;

            var perjury = Owner.Creature.GetPower<PerjuryPower>();
            var cost = DynamicVars["PerjuryCost"].IntValue;
            return (perjury?.Amount ?? 0) >= cost;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var source = this;
        var cost = source.DynamicVars["PerjuryCost"].IntValue;

        var perjury = source.Owner.Creature.GetPower<PerjuryPower>();
        if (perjury != null && perjury.Amount >= cost)
        {
            if (perjury.Amount == cost)
                await PowerCmd.Remove(perjury);
            else
                await PowerCmd.ModifyAmount(choiceContext, perjury, -cost, source.Owner.Creature, source, false);
        }

        await PowerCmd.Apply<Poweroneone>(
            choiceContext, source.Owner.Creature, 1,
            source.Owner.Creature, source, false
        );
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["PerjuryCost"].UpgradeValueBy(-1m);
    }
}