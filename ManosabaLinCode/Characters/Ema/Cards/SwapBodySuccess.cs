using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Emalin;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class SwapBodySuccess : ManosabaEmalinCardTemplate
{
    public SwapBodySuccess() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<BondPower>(); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var owner = Owner;
        var creature = owner.Creature;

        // 亲近+1
        var bond = creature.GetPower<BondPower>();
        if (bond != null) bond.Affinity++;

        // 获得一张目标角色卡池零费随机卡
        var charPool = ModelDb.Character<Emalin.Emalin>().CardPool;
        var options = CardCreationOptions.ForNonCombatWithUniformOdds(
            new[] { charPool }).WithFlags(CardCreationFlags.NoRarityModification);
        var cards = CardFactory.CreateForReward(owner, 1, options).ToList();
        if (cards.Count > 0)
        {
            var card = cards[0].Card;
            card.SetToFreeThisTurn();
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, owner);
        }

        // 亲近大于疏远时，目标获得一张自己卡池零费随机卡
        if (bond != null && bond.Affinity > bond.Estrangement)
        {
            var hiroPool = ModelDb.Character<Hiro.Hiro>().CardPool;
            var hiroOptions = CardCreationOptions.ForNonCombatWithUniformOdds(
                new[] { hiroPool }).WithFlags(CardCreationFlags.NoRarityModification);
            var hiroCards = CardFactory.CreateForReward(owner, 1, hiroOptions).ToList();
            if (hiroCards.Count > 0)
            {
                var hiroCard = hiroCards[0].Card;
                hiroCard.SetToFreeThisTurn();
                await CardPileCmd.AddGeneratedCardToCombat(hiroCard, PileType.Hand, owner);
            }
        }
    }
}
