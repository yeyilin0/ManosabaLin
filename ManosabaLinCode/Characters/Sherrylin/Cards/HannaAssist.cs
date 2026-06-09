using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Components;
using ManosabaLin.Characters.Sherrylin.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Cards;

/// <summary>
/// 汉娜的协力：浮空组件，抽卡然后造成伤害获得情绪，升级增加伤害。
/// </summary>
[RegisterCard(typeof(SherrylinCardPool))]
public sealed class HannaAssist() : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            yield return CardKeyword.Retain;
            yield return CardKeyword.Exhaust;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(8m, ValueProp.Move),
        new PowerVar<EmotionPower>(1m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<EmotionPower>(); }
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        // 添加浮空组件
        source.TryAddComponent(new LevitationComponent());

        // 抽1张
        await CardPileCmd.Draw(choiceContext, 1, source.Owner);

        // 造成伤害
        var target = cardPlay.Target ?? source.Owner.Creature;
        await CreatureCmd.Damage(choiceContext, target,
            source.DynamicVars.Damage.BaseValue,
            ValueProp.Move, source);

        // 获得情绪
        await PowerCmd.Apply<EmotionPower>(
            choiceContext, source.Owner.Creature,
            source.DynamicVars["EmotionPower"].IntValue,
            source.Owner.Creature, source, false);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Damage.UpgradeValueBy(4m);
    }
}
