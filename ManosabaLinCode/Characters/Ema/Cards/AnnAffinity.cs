using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Emalin;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class AnnAffinity : ManosabaEmalinCardTemplate
{
    public AnnAffinity() : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<BondPower>(); }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new IntVar("PickCount", 1)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var owner = Owner;
        var creature = owner.Creature;

        var bond = creature.GetPower<BondPower>();
        if (bond != null) bond.Affinity++;

        var pickCount = DynamicVars["PickCount"].IntValue;

        var prefs = new CardSelectorPrefs(
            new LocString("AnnAffinity", "选择一张卡改变费用"), 0, pickCount);
        var selected = await CardSelectCmd.FromDeckGeneric(owner, prefs, null);

        foreach (var card in selected)
        {
            if (bond != null && bond.Affinity > bond.Estrangement)
            {
                card.EnergyCost.SetThisCombat(0);
            }
            else
            {
                var randomCost = owner.RunState.Rng.CombatEnergyCosts.NextInt(3);
                card.EnergyCost.SetThisCombat(randomCost);
            }

            CardCmd.PreviewCardPileAdd(
                await CardPileCmd.Add(card, PileType.Hand));
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["PickCount"].UpgradeValueBy(1);
    }
}