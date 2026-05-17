using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using ManosabaLin.Characters.Emalin;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class EmaTen : ManosabaEmalinCardTemplate
{
    public EmaTen() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.RandomEnemy)
    {
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<SuspectPower>();
            yield return HoverTipFactory.FromPower<HiroMagicRevivePower>();
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(5m, ValueProp.Move),
        new PowerVar<SuspectPower>(1m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        await PowerCmd.Apply<SuspectPower>(
            choiceContext, source.Owner.Creature,
            1m, source.Owner.Creature, source, false);

        await PowerCmd.Apply<HiroMagicRevivePower>(
            choiceContext, source.Owner.Creature,
            1m, source.Owner.Creature, source, false);

        // 从牌组中永久移除此卡
        await CardPileCmd.RemoveFromDeck(source);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}