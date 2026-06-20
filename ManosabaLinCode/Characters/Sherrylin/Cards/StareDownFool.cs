using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(SherrylinCardPool))]
public sealed class StareDownFool() : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<WeakPower>(2m),
        new PowerVar<VulnerablePower>(2m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<WeakPower>();
            yield return HoverTipFactory.FromPower<VulnerablePower>();
        }
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var target = cardPlay.Target ?? source.Owner.Creature;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        var intents = target.Monster?.NextMove?.Intents;
        bool isAttackIntent = false;

        if (intents != null)
        {
            foreach (var intent in intents)
            {
                if (intent.IntentType == IntentType.Attack || intent.IntentType == IntentType.DeathBlow)
                {
                    isAttackIntent = true;
                    break;
                }
            }
        }

        if (isAttackIntent)
        {
            await PowerCmd.Apply<WeakPower>(
                choiceContext, target,
                source.DynamicVars.Weak.BaseValue,
                source.Owner.Creature, source, false);
        }
        else
        {
            await PowerCmd.Apply<VulnerablePower>(
                choiceContext, target,
                source.DynamicVars.Vulnerable.BaseValue,
                source.Owner.Creature, source, false);
            await CardPileCmd.Draw(choiceContext, 1, source.Owner);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Weak.UpgradeValueBy(1m);
        DynamicVars.Vulnerable.UpgradeValueBy(1m);
    }
}
