using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Emalin;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Players;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class SwapBodySuccess : ManosabaCardTemplate
{
    public SwapBodySuccess() : base(2, CardType.Skill, CardRarity.Rare, TargetType.AnyPlayer) { }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<BondPower>(); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var owner = Owner;
        var creature = owner.Creature;
        var combatState = CombatState;

        var bond = creature.GetPower<BondPower>();
        if (bond != null) bond.Affinity++;

        // 选择目标玩家
        var targetPlayer = (cardPlay.Target ?? creature).Player ?? owner;

        // 获得一张目标角色卡池零费随机卡
        var targetPool = targetPlayer.Character.CardPool;
        var poolCards = targetPool.AllCards
            .Where(c => c.Rarity != CardRarity.Basic)
            .ToList();

        if (poolCards.Count > 0)
        {
            var rng = owner.RunState.Rng.CombatCardSelection;
            var template = rng.NextItem(poolCards);
            var card = combatState.CreateCard(template, owner);
            card.SetToFreeThisTurn();
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, owner);
        }

        // 亲近大于疏远时，选择的队友再获得一张
        if (bond != null && bond.Affinity > bond.Estrangement && targetPlayer != owner)
        {
            var allyPool = targetPlayer.Character.CardPool;
            var allyPoolCards = allyPool.AllCards
                .Where(c => c.Rarity != CardRarity.Basic)
                .ToList();

            if (allyPoolCards.Count > 0)
            {
                var rng = owner.RunState.Rng.CombatCardSelection;
                var template = rng.NextItem(allyPoolCards);
                var bonusCard = combatState.CreateCard(template, targetPlayer);
                bonusCard.SetToFreeThisTurn();
                await CardPileCmd.AddGeneratedCardToCombat(bonusCard, PileType.Hand, targetPlayer);
            }
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
