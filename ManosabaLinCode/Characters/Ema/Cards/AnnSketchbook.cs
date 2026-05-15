using ManosabaLin.Characters.Common;
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

/// <summary>安安的素描本 - 1费技能, 疑问附魔, 检视抽牌堆顶部3张选1, 升级看4张选2</summary>
[RegisterCard(typeof(EmalinCardPool))]
public sealed class AnnSketchbook : ManosabaEmalinCardTemplate
{
    public AnnSketchbook() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<string> RegisteredKeywordIds =>
        new[] { EmalinKeywordRules.DoubtKeywordId };

    protected override IEnumerable<DynamicVar> CanonicalVars => [new CardsVar(3)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var drawPile = PileType.Draw.GetPile(Owner).Cards.ToList();
        var lookCount = DynamicVars.Cards.IntValue;
        var pickCount = IsUpgraded ? 2 : 1;
        var topCards = drawPile.Take(lookCount).ToList();
        if (topCards.Count == 0) return;

        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 0, pickCount);
        var selected = await CardSelectCmd.FromSimpleGrid(choiceContext, topCards, Owner, prefs);
        foreach (var card in selected)
            await CardPileCmd.Add(card, PileType.Hand);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1);
    }
}