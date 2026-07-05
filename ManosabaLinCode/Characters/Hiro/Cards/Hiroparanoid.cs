using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(LinCardPool))]
public sealed class Hiroparanoid : ManosabaCardTemplate
{
    public Hiroparanoid() : base(1, CardType.Status, CardRarity.Ancient, TargetType.Self)
    {
    }

    public override int MaxUpgradeLevel => 0;

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new DynamicVar("DiscardCount", 1m); }
    }

    // ★ 被抽到时失去 1 点能量
    protected override async Task AfterCardDrawn(
        PlayerChoiceContext choiceContext,
        CardModel card,
        bool fromHandDraw, ComponentContext componentContext)
    {
        var source = this;
        if (card != source) return;

        await PlayerCmd.LoseEnergy(1m, source.Owner);
    }

    // ★ 打出：选一张牌丢弃
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
