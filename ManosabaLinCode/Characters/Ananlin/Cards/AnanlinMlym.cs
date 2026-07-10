using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Emalin;
using ManosabaLin.Characters.Emalin.Enchantments;
using ManosabaLin.Characters.Ananlin;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ManosabaLin.Characters.Ema.Powers;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinMlym : ManosabaCardTemplate
{
    public AnanlinMlym() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly) { }

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<MlyPower>(1m)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            yield return CardKeyword.Retain;
            yield return CardKeyword.Exhaust;
        }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<MlyPower>();
            foreach (var tip in HoverTipFactory.FromEnchantment<Mlypower>())
                yield return tip;
        }
    }

    private static bool CanBeExchanged(CardModel card)
    {
        // 跳过已有附魔的牌
        if (card.Enchantment != null) return false;

        // 跳过特殊类型的牌
        if (card.Rarity == CardRarity.Status) return false;
        if (card.Rarity == CardRarity.Curse) return false;
        if (card.Rarity == CardRarity.Quest) return false;

        return true;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        if (cardPlay.Target == null) return;

        var targetPlayer = cardPlay.Target.Player;
        if (targetPlayer == null || targetPlayer == Owner) return;

        var owner = Owner;

        await PowerCmd.Apply<MlyPower>(choiceContext, cardPlay.Target, DynamicVars["MlyPower"].BaseValue, Owner.Creature, this, false);

        // 只选取可以交换的牌（排除已有附魔和特殊稀有度）
        var myHand = PileType.Hand.GetPile(owner).Cards
            .Where(c => c != this && CanBeExchanged(c))
            .ToList();
        var theirHand = PileType.Hand.GetPile(targetPlayer).Cards
            .Where(c => CanBeExchanged(c))
            .ToList();

        foreach (var card in myHand)
            await CardPileCmd.RemoveFromCombat(card);
        foreach (var card in theirHand)
            await CardPileCmd.RemoveFromCombat(card);

        // 我的牌复制给队友，附魔
        foreach (var card in myHand)
        {
            var newCard = CombatState.CreateCard(card.CanonicalInstance, targetPlayer);
            if (card.CurrentUpgradeLevel > 0)
            {
                for (int i = 0; i < card.CurrentUpgradeLevel; i++)
                    CardCmd.Upgrade(newCard);
            }
            CardCmd.Enchant(ModelDb.Enchantment<Mlypower>().ToMutable(), newCard, 1m);
            await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, targetPlayer);
        }

        // 队友的牌复制给我，附魔
        foreach (var card in theirHand)
        {
            var newCard = CombatState.CreateCard(card.CanonicalInstance, owner);
            if (card.CurrentUpgradeLevel > 0)
            {
                for (int i = 0; i < card.CurrentUpgradeLevel; i++)
                    CardCmd.Upgrade(newCard);
            }
            CardCmd.Enchant(ModelDb.Enchantment<Mlypower>().ToMutable(), newCard, 1m);
            await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, owner);
        }

        await PowerCmd.Remove<MlyPower>(cardPlay.Target);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
        RemoveKeyword(CardKeyword.Exhaust);
    }
}