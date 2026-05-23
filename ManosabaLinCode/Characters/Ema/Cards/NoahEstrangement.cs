using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Emalin;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class NoahEstrangement() : ManosabaCardTemplate(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<BondPower>(), HoverTipFactory.FromPower<CrimsonbutterflyPower>()
    ];


    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var owner = Owner;
        var creature = owner.Creature;

        var bond = creature.GetPower<BondPower>();
        if (bond != null) bond.Estrangement++;

        var handCards = PileType.Hand.GetPile(owner).Cards.ToList();
        if (handCards.Count > 0)
        {
            var replayPrefs = new CardSelectorPrefs(SelectionScreenPrompt, 1);
            var replaySelected = await CardSelectCmd.FromHand(
                choiceContext, owner, replayPrefs, null, null!);
            var replayCard = replaySelected.FirstOrDefault();
            if (replayCard != null)
                replayCard.BaseReplayCount++;
        }

        var discardHand = PileType.Hand.GetPile(owner).Cards.ToList();
        if (discardHand.Count > 0)
        {
            var discardPrefs = new CardSelectorPrefs(SelectionScreenPrompt, 1, 1);
            var discardSelected = await CardSelectCmd.FromHand(
                choiceContext, owner, discardPrefs, null, null!);
            var discardCard = discardSelected.FirstOrDefault();
            if (discardCard != null)
            {
                await CardCmd.Discard(choiceContext, discardCard);
            }
        }

        if (bond != null && bond.Estrangement > bond.Affinity)
        {
            await PowerCmd.Apply<CrimsonbutterflyPower>(
                choiceContext, creature, 1, creature, this, false);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
