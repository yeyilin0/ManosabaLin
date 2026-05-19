using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Emalin;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class TheOnlyClue : ManosabaCardTemplate
{
    public TheOnlyClue() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<BondPower>();
            yield return HoverTipFactory.FromPower<NyxmPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var owner = Owner;
        var creature = owner.Creature;

        var bond = creature.GetPower<BondPower>();
        if (bond != null) bond.Affinity++;

        await PowerCmd.Apply<NyxmPower>(
            choiceContext, creature, 2, creature, this, false);

        await CardPileCmd.Draw(choiceContext, 2m, owner);

        var handCards = PileType.Hand.GetPile(owner).Cards;
        if (handCards.Count > 0)
        {
            var prefs = new CardSelectorPrefs(
                new LocString("TheOnlyClue", "选择1张卡弃掉"), 1, 1);
            var selected = await CardSelectCmd.FromHand(
                choiceContext, owner, prefs, null, this);
            var discarded = selected.FirstOrDefault();
            if (discarded != null)
            {
                if (bond != null && bond.Affinity > bond.Estrangement)
                    discarded.BaseReplayCount++;

                await CardPileCmd.Add(discarded, PileType.Discard);
            }
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
