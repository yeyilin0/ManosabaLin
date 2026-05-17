using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Emalin;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Ema.Cards;

/// <summary>控制面板 - 1费技能, 疑问附魔, 抽3张然后弃1张, 升级抽5张</summary>
[RegisterCard(typeof(EmalinCardPool))]
public sealed class ControlPanel : ManosabaEmalinCardTemplate
{
    public ControlPanel() : base(2, CardType.Skill, CardRarity.Common, TargetType.Self) { }

    protected override IEnumerable<string> RegisteredKeywordIds =>
        new[] { EmalinKeywordRules.DoubtKeywordId };

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);

        var handCards = PileType.Hand.GetPile(Owner).Cards;
        if (handCards.Count == 0) return;

        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1, 1);
        var selected = await CardSelectCmd.FromHand(choiceContext, Owner, prefs, null, this);
        foreach (var card in selected)
            await CardPileCmd.Add(card, PileType.Discard);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Cards.UpgradeValueBy(2);
    }
}