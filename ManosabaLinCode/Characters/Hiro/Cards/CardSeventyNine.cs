using MinionLib.Component.Core;
﻿using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.HiroKeywords;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class CardSeventyNine() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    private const string RemoveCountKey = "RemoveCount";
    private const string GrantCountKey = "GrantCount";

    // 固定基础值：失去 1，获得 1
    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new IntVar(RemoveCountKey, 1),
        new IntVar(GrantCountKey, 1)
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new[] { TransmigrationRules.TransmigrationKeywordId.GetModCardKeyword() };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        var handCards = PileType.Hand.GetPile(source.Owner).Cards.ToList();
        if (handCards.Count == 0) return;

        var removeCount = source.DynamicVars[RemoveCountKey].IntValue;
        var grantCount = source.DynamicVars[GrantCountKey].IntValue;
        var rebirthId = TransmigrationRules.TransmigrationKeywordId;

        // 1. 选择要失去"轮回"的牌
        var cardsToRemove = await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(source.SelectionScreenPrompt, removeCount),
            c => c != this && c.HasModKeyword(rebirthId),
            this);

        foreach (var card in cardsToRemove)
        {
            card.RemoveModKeyword(rebirthId);
            RefreshCardVisuals(card);
        }

        // 2. 选择要获得"轮回"的牌
        var cardsToGrant = await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(source.SelectionScreenPrompt, grantCount),
            c => c != this && !c.HasModKeyword(rebirthId),
            this);

        foreach (var card in cardsToGrant)
        {
            card.AddModKeyword(rebirthId);
            RefreshCardVisuals(card);
        }
    }

    private static void RefreshCardVisuals(CardModel card)
    {
        var node = NCard.FindOnTable(card);
        if (node != null) node.UpdateVisuals(card.Pile?.Type ?? PileType.Hand, CardPreviewMode.Normal);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        // 只有获得轮回的牌数 +1（1 → 2），失去轮回保持 1
        DynamicVars[GrantCountKey].UpgradeValueBy(1);
    }
}
