using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(SherrylinCardPool))]
public sealed class SilverBlaze() : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromCard<SilverBlazeToken>(IsUpgraded);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        var handCards = PileType.Hand.GetPile(Owner).Cards
            .Where(c => c != source && c.Type == CardType.Attack)
            .ToList();

        if (handCards.Count == 0) return;

        var rng = Owner.RunState.Rng.CombatCardSelection;
        var selectedAttack = rng.NextItem(handCards);
        if (selectedAttack == null) return;

        // 读取被消耗牌的基础伤害（和 Thrash 一样的逻辑）
        decimal capturedDamage = 0m;
        if (selectedAttack.DynamicVars.ContainsKey("CalculatedDamage"))
            capturedDamage = selectedAttack.DynamicVars.CalculatedDamage.Calculate(null);
        else if (selectedAttack.DynamicVars.ContainsKey("Damage"))
            capturedDamage = selectedAttack.DynamicVars.Damage.BaseValue;
        else if (selectedAttack.DynamicVars.ContainsKey("OstyDamage"))
            capturedDamage = selectedAttack.DynamicVars.OstyDamage.BaseValue;

        capturedDamage = Hook.ModifyDamage(
            Owner.RunState, Owner.Creature.CombatState, null,
            Owner.Creature, capturedDamage, ValueProp.Move,
            selectedAttack, null, ModifyDamageHookType.All, CardPreviewMode.None,
            out _);

        await CardCmd.Exhaust(choiceContext, selectedAttack);

        // 生成 token，基础伤害 = 1 + 被消耗牌的伤害
        var token = CombatState.CreateCard<SilverBlazeToken>(Owner);
        token.DynamicVars.Damage.BaseValue += capturedDamage;

        await CardPileCmd.AddGeneratedCardToCombat(token, PileType.Hand, Owner);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        // 升级效果在 SilverBlazeToken 的 OnUpgrade 中处理（+3）
    }
}
