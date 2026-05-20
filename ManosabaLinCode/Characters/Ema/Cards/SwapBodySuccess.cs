using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Emalin;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Runs;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class SwapBodySuccess : ManosabaCardTemplate
{
    public SwapBodySuccess() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<BondPower>(); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var owner = Owner;
        var creature = owner.Creature;

        var bond = creature.GetPower<BondPower>();
        if (bond != null) bond.Affinity++;

        // 选择目标玩家（多人时选别人，单人默认自己）
        Player targetPlayer = owner;
        var otherPlayers = CombatState.Players.Where(p => p != owner).ToList();
        if (otherPlayers.Count > 0)
        {
            var rng = owner.RunState.Rng.CombatTargets;
            targetPlayer = rng.NextItem(otherPlayers);
        }

        // 获得一张目标角色卡池零费随机卡
        var targetPool = targetPlayer.Character.CardPool;
        var options = CardCreationOptions.ForNonCombatWithUniformOdds(
            new[] { targetPool }).WithFlags(CardCreationFlags.NoRarityModification);
        var cards = CardFactory.CreateForReward(owner, 1, options).ToList();
        if (cards.Count > 0)
        {
            var card = cards[0].Card;
            card.SetToFreeThisTurn();
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, owner);
        }

        // 亲近大于疏远时，再获得一张目标角色卡池零费随机卡
        if (bond != null && bond.Affinity > bond.Estrangement)
        {
            var bonusCards = CardFactory.CreateForReward(owner, 1, options).ToList();
            if (bonusCards.Count > 0)
            {
                var bonusCard = bonusCards[0].Card;
                bonusCard.SetToFreeThisTurn();
                await CardPileCmd.AddGeneratedCardToCombat(bonusCard, PileType.Hand, owner);
            }
        }
    }
    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
