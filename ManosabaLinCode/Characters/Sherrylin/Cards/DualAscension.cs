using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Cards;

/// <summary>
/// 双相升华：当你打出5张手卡，随机升级一张手卡，升级变成随机升级一张手卡和一张抽牌堆卡
/// </summary>
[RegisterCard(typeof(SherrylinCardPool))]
public sealed class DualAscension() : ManosabaCardTemplate(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<DualAscensionPower>(5m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<DualAscensionPower>(); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await PowerCmd.Apply<DualAscensionPower>(
            choiceContext, source.Owner.Creature,
            source.DynamicVars["DualAscensionPower"].BaseValue,
            source.Owner.Creature, source, false);

        var power = source.Owner.Creature.GetPower<DualAscensionPower>();
        if (power != null)
            power.SourceUpgraded = IsUpgraded;
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        // 升级改变行为逻辑（通过 IsUpgraded 判断），不改变数值
    }
}
