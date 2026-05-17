using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.Characters.Emalin;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class EmaPowertwotwocard() : ManosabaEmalinCardTemplate(2, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    private const int RequiredWithAmount = 100;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<RitualCeremonyPower>(2m)
    };

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<WithPower>();
            yield return HoverTipFactory.FromPower<RitualCeremonyPower>();
        }
    }

    protected override bool IsPlayableC
    {
        get
        {
            if (!base.IsPlayableC) return false;

            var with = Owner.Creature.GetPower<WithPower>();
            return (with?.Amount ?? 0) >= RequiredWithAmount;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await PowerCmd.Apply<RitualCeremonyPower>(
            choiceContext, source.Owner.Creature,
            source.DynamicVars["RitualCeremonyPower"].BaseValue,
            source.Owner.Creature, source, false
        );
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["RitualCeremonyPower"].UpgradeValueBy(1m);
    }
}
