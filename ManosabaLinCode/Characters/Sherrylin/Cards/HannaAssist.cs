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

[RegisterCard(typeof(SherrylinCardPool))]
public sealed class HannaAssist : ManosabaCardTemplate
{
    public HannaAssist() : base(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        // 自带浮空组件
        this.AddComponent(new LevitationComponent());
    }

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
        new DamageVar(10m, ValueProp.Move),
        new PowerVar<EmotionPower>(12m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<EmotionPower>();
            yield return LevitationComponent.Tip;
        }
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CardPileCmd.Draw(choiceContext, 1, source.Owner);

        var target = cardPlay.Target ?? source.Owner.Creature;
        await CreatureCmd.Damage(choiceContext, target,
            source.DynamicVars.Damage.BaseValue,
            ValueProp.Move, source);

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
