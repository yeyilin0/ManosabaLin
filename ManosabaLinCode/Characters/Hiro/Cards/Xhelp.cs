using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class Xhelp() : ManosabaCardTemplate(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyPlayer)
{
    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(8m, ValueProp.Move),
        new PowerVar<JusticePower>(1m),
        new PowerVar<SuspectPower>(1m)
    };

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<JusticePower>();
            yield return HoverTipFactory.FromPower<SuspectPower>();
            yield return HoverTipFactory.FromPower<WithPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var source = this;
        var target = cardPlay.Target ?? source.Owner.Creature;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        var allies = source.Owner.Creature.CombatState.Creatures
            .Where(c => c.IsAlive && !c.IsEnemy)
            .ToList();

        foreach (var ally in allies)
        {
            // 目标用基础格挡
            await CreatureCmd.GainBlock(ally, source.DynamicVars.Block, cardPlay);

            await PowerCmd.Apply<JusticePower>(
                choiceContext, ally,
                source.DynamicVars["JusticePower"].BaseValue,
                source.Owner.Creature, source, false
            );
        }

        // 目标魔女化换格挡
        var with = target.GetPower<WithPower>();
        if (with != null && with.Amount >= 50)
        {
            var consumeAmount = (int)(with.Amount / 10);
            await PowerCmd.ModifyAmount(choiceContext, with, -consumeAmount, target, source, false);
            // 直接用 Heal 或者通过临时 BlockVar 绕过
            target.GainBlockInternal(consumeAmount * 1);
        }

        // 自己获得 1 层嫌疑
        await PowerCmd.Apply<SuspectPower>(
            choiceContext, source.Owner.Creature, 1,
            source.Owner.Creature, source, false
        );
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(4m);
        DynamicVars["JusticePower"].UpgradeValueBy(1m);
    }
}