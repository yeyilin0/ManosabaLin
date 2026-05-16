using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Emalin;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class NoahEstrangement : ManosabaEmalinCardTemplate
{
    public NoahEstrangement() : base(3, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<BondPower>();
            yield return HoverTipFactory.FromPower<CrimsonbutterflyPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var owner = Owner;
        var creature = owner.Creature;

        var bond = creature.GetPower<BondPower>();
        if (bond != null) bond.Estrangement++;

        var handCards = PileType.Hand.GetPile(owner).Cards.ToList();
        if (handCards.Count > 0)
        {
            var replayPrefs = new CardSelectorPrefs(
                new LocString("NoahEstrangement", "选择一张手卡获得重放"), 1, 1);
            var replaySelected = await CardSelectCmd.FromHand(
                choiceContext, owner, replayPrefs, null, this);
            var replayCard = replaySelected.FirstOrDefault();
            if (replayCard != null)
            {
                replayCard.BaseReplayCount++;
                CardCmd.Preview(replayCard);
            }
        }

        var discardHand = PileType.Hand.GetPile(owner).Cards.ToList();
        if (discardHand.Count > 0)
        {
            var discardPrefs = new CardSelectorPrefs(
                new LocString("NoahEstrangement", "选择一张手卡丢弃"), 1, 1);
            var discardSelected = await CardSelectCmd.FromHand(
                choiceContext, owner, discardPrefs, null, this);
            var discardCard = discardSelected.FirstOrDefault();
            if (discardCard != null)
                await CardPileCmd.Add(discardCard, PileType.Discard);
        }

        if (bond != null && bond.Estrangement > bond.Affinity)
        {
            await PowerCmd.Apply<CrimsonbutterflyPower>(
                choiceContext, creature, 1, creature, this, false);
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}