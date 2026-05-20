using MinionLib.Component.Core;
﻿using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Emalin;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Ema.Cards;

/// <summary>换身的谎言 - 1费技能, 抽1张, 疑问计数≥2时选择手牌变化为本角色随机牌, 升级变化2张</summary>
[RegisterCard(typeof(EmalinCardPool))]
public sealed class Theswaplie : ManosabaCardTemplate
{
    public Theswaplie() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => 
    [
        new CardsVar(1),
        new IntVar("TransformCount", 1)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);

        var doubtCount = EmalinCombatHelper.GetDoubtPlaysThisTurn(Owner.Creature, CombatState);
        if (doubtCount < 2) return;

        var transformCount = DynamicVars["TransformCount"].IntValue;
        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 0, transformCount);
        var selected = await CardSelectCmd.FromHand(choiceContext, Owner, prefs, null, source);

        var rng = Owner.RunState.Rng.CombatCardSelection;

        foreach (var original in selected)
        {
            if (original.CombatState == null) continue;

            // 从本角色卡池随机选模板，用 CombatState.CreateCard 正确注册
            var poolCards = Owner.Character.CardPool.AllCards
                .Where(c => c.Rarity != CardRarity.Basic)
                .ToList();

            if (poolCards.Count == 0) continue;

            var newCardTemplate = rng.NextItem(poolCards);
            var newCard = CombatState.CreateCard(newCardTemplate, Owner);

            await CardCmd.Exhaust(choiceContext, original);
            await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, Owner, CardPilePosition.Bottom);
            CardCmd.Preview(newCard);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["TransformCount"].UpgradeValueBy(1);
    }
}
