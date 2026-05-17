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
public sealed class Threehiro : ManosabaCardTemplate
{
    public Threehiro() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new DynamicVar("SelectCount", 1m); }
    }

    protected override IEnumerable<string> RegisteredKeywordIds =>
        new[] { Transmigration3Rules.Transmigration3KeywordId };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var owner = source.Owner;

        await CreatureCmd.TriggerAnim(owner.Creature, "Cast", owner.Character.CastAnimDelay);

        var keywordId = Transmigration3Rules.Transmigration3KeywordId;

        // 从抽牌堆、手牌、弃牌堆各选一张卡
        var drawPile = PileType.Draw.GetPile(owner).Cards
            .Where(c => !c.HasModKeyword(keywordId)).ToList();
        var handPile = PileType.Hand.GetPile(owner).Cards
            .Where(c => c != this && !c.HasModKeyword(keywordId)).ToList();
        var discardPile = PileType.Discard.GetPile(owner).Cards
            .Where(c => !c.HasModKeyword(keywordId)).ToList();

        var allSelected = new List<CardModel>();

        if (drawPile.Count > 0)
        {
            var result = await CardSelectCmd.FromSimpleGrid(choiceContext, drawPile, owner,
                new CardSelectorPrefs(source.SelectionScreenPrompt, 1));
            allSelected.AddRange(result);
        }

        if (handPile.Count > 0)
        {
            var result = await CardSelectCmd.FromSimpleGrid(choiceContext, handPile, owner,
                new CardSelectorPrefs(source.SelectionScreenPrompt, 1));
            allSelected.AddRange(result);
        }

        if (discardPile.Count > 0)
        {
            var result = await CardSelectCmd.FromSimpleGrid(choiceContext, discardPile, owner,
                new CardSelectorPrefs(source.SelectionScreenPrompt, 1));
            allSelected.AddRange(result);
        }

        // 只添加关键词，不自动打出
        foreach (var card in allSelected)
        {
            card.AddModKeyword(keywordId);
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
        EnergyCost.UpgradeBy(-1);
    }
}