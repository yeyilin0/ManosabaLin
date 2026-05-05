using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class CardFourteen() : ManosabaCardTemplate(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    // 需要消耗的正义层数
    private int RequiredJusticeAmount => 3;


    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(33m, ValueProp.Move),
        new PowerVar<WeakPower>(3m),
        new("JusticePower", 3m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<WeakPower>();
            yield return HoverTipFactory.FromPower<JusticePower>();
        }
    }

    // 控制卡牌是否可打出
    protected override bool IsPlayable
    {
        get
        {
            if (!base.IsPlayable)
                return false;

            var justicePower = Owner.Creature.GetPower<JusticePower>();
            var justiceAmount = justicePower?.Amount ?? 0;

            return justiceAmount >= RequiredJusticeAmount;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var source = this;
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        // 第一步：消耗 3 层正义
        var justicePower = source.Owner.Creature.GetPower<JusticePower>();
        if (justicePower != null && justicePower.Amount >= RequiredJusticeAmount)
            await PowerCmd.ModifyAmount(
                choiceContext,
                justicePower,
                -RequiredJusticeAmount,
                source.Owner.Creature,
                source,
                false
            );

        // 第二步：造成 33 点伤害
        await DamageCmd.Attack(source.DynamicVars.Damage.BaseValue)
            .FromCard(source)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);

        // 第三步：给予 3 层虚弱
        await PowerCmd.Apply<WeakPower>(
            choiceContext,
            cardPlay.Target,
            source.DynamicVars.Weak.BaseValue,
            source.Owner.Creature,
            source
        );
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(11m); // 伤害 33 → 44
    }
}