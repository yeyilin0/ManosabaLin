using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Emalin;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
[RegisterCharacterStarterCard(typeof(Emalin.Emalin))]
public class EmalinMentalTrauma : ManosabaEmalinCardTemplate
{
    public EmalinMentalTrauma() : base(0, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<WithPower>(); }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new PowerVar<WithPower>(20m)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        // 获得 20 层魔女化
        await PowerCmd.Apply<WithPower>(
            choiceContext, Owner.Creature,
            DynamicVars["WithPower"].BaseValue,
            Owner.Creature,
            this,
            false
        );

        // 升级后抽 1 张牌
        if (IsUpgraded) await CardPileCmd.Draw(choiceContext, 1m, Owner);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        // 先古卡的升级由遗物控制进化，这里留空
    }
}
