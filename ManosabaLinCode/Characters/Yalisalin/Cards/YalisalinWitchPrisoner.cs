using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.Powers;
using ManosabaLin.Characters.Yalisalin.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Yalisalin.Cards;

[RegisterCard(typeof(YalisalinCardPool))]
public sealed class YalisalinWitchPrisoner() : ManosabaCardTemplate(3, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<TempStrength>("TempStrength", 6m),
        new PowerVar<TempDexterity>("TempDexterity", 6m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<TempStrength>();
            yield return HoverTipFactory.FromPower<TempDexterity>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        await PowerCmd.Apply<TempStrength>(
            choiceContext, Owner.Creature,
            DynamicVars["TempStrength"].BaseValue,
            Owner.Creature,
            this,
            false
        );

        await PowerCmd.Apply<TempDexterity>(
            choiceContext, Owner.Creature,
            DynamicVars["TempDexterity"].BaseValue,
            Owner.Creature,
            this,
            false
        );

        // 予燎：6 格火色逐格随机分给敌人，超出量表的部分一次性补结算消耗效果
        await YalisalinFireColorCardHelpers.GiveRandomlyAmongEnemies(
            choiceContext, Owner, IgniteAmount, this, overflowTriggersConsume: true);
    }

    private const int IgniteAmount = 6;

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
