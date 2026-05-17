using MinionLib.Component.Core;
﻿using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(LinCardPool))]
public sealed class Hiroparanoid : ManosabaCardTemplate
{
    public Hiroparanoid() : base(1, CardType.Status, CardRarity.Ancient, TargetType.Self)
    {
    }

    public override int MaxUpgradeLevel => 0;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new DynamicVar("DiscardCount", 1m); }
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    // ★ 被抽到时触发（Void 风格）
    protected override async Task AfterCardDrawn(
        PlayerChoiceContext choiceContext,
        CardModel card,
        bool fromHandDraw, ComponentContext componentContext)
    {
        var source = this;
        if (card != source) return;

        await Cmd.Wait(0.25f);

        // 失去 1 点能量
        await PlayerCmd.LoseEnergy(1m, source.Owner);

        // Cascade 风格：自动打出自己（从抽牌堆）
        await CardPileCmd.AutoPlayFromDrawPile(choiceContext, source.Owner, 1, CardPilePosition.Top, false);
    }

    // ★ 打出：选一张牌丢弃（Survivor 风格）
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        var card = (await CardSelectCmd.FromHandForDiscard(
            choiceContext,
            source.Owner,
            new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt,
                source.DynamicVars["DiscardCount"].IntValue),
            null,
            source
        )).FirstOrDefault();

        if (card == null) return;

        await CardCmd.Discard(choiceContext, card);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
    }
}