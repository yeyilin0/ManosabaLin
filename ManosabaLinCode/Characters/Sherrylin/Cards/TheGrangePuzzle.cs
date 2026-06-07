using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.Characters.Sherrylin.Powers;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Sherrylin.Cards;

[RegisterCard(typeof(SherrylinCardPool))]
public sealed class TheGrangePuzzle() : ManosabaCardTemplate(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    private const string BaseCountKey = "BaseCount";

    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new IntVar(BaseCountKey, 1),
        new IntVar("Bonus", 0)
    };

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<XlmPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        var xlmStacks = (int)Owner.Creature.GetPowerAmount<XlmPower>();
        var bonus = (int)DynamicVars["Bonus"].BaseValue;
        var count = xlmStacks + bonus;
        if (count <= 0) return;

        var exhaustCards = PileType.Exhaust.GetPile(Owner).Cards.ToList();
        if (exhaustCards.Count == 0) return;

        var rng = Owner.RunState.Rng.CombatCardSelection;
        var cardsToUpgrade = exhaustCards.OrderBy(_ => rng.NextFloat()).Take(count).ToList();

        foreach (var card in cardsToUpgrade)
        {
            CardCmd.Upgrade(card, CardPreviewStyle.None);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["Bonus"].UpgradeValueBy(2m);
    }
}