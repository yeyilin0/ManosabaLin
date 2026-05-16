using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Emalin;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class MargaretEstrangement : ManosabaEmalinCardTemplate
{
    public MargaretEstrangement() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<BondPower>(); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var owner = Owner;
        var creature = owner.Creature;

        // 疏远+1
        var bond = creature.GetPower<BondPower>();
        if (bond != null) bond.Estrangement++;

        // 获得玛格的魔法
        await PowerCmd.Apply<MgmPower>(
            choiceContext, creature, 1, creature, this, false);

        // 选择一个队友，没有队友就自己
        Player targetPlayer = owner;
        var allies = CombatState.Players.Where(p => p != owner).ToList();
        if (allies.Count > 0)
        {
            var rng = owner.RunState.Rng.CombatTargets;
            targetPlayer = rng.NextItem(allies);
        }

        // 从目标牌组中随机选一张牌复制
        var deckCards = PileType.Deck.GetPile(targetPlayer).Cards.ToList();
        if (deckCards.Count == 0) return;

        var rng2 = owner.RunState.Rng.CombatCardSelection;
        var sourceCard = rng2.NextItem(deckCards);

        // 复制卡牌
        var clone = owner.RunState.CloneCard(sourceCard);

        // 疏远>亲近时，复制的卡能免费打出一次
        if (bond != null && bond.Estrangement > bond.Affinity)
            clone.SetToFreeThisCombat();

        CardCmd.PreviewCardPileAdd(
            await CardPileCmd.Add(clone, PileType.Hand));
    }
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}