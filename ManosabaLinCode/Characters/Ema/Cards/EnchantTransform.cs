using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Emalin;
using ManosabaLin.Characters.Emalin.Enchantments;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class EnchantTransform : ManosabaEmalinCardTemplate
{
    public EnchantTransform() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var owner = Owner;

        // 1. 选择一张手卡获得重放
        var handCards = PileType.Hand.GetPile(owner).Cards.ToList();
        if (handCards.Count > 0)
        {
            var replayPrefs = new CardSelectorPrefs(SelectionScreenPrompt, 1, 1);
            var replaySelected = await CardSelectCmd.FromHand(
                choiceContext, owner, replayPrefs, null, this);
            var replayCard = replaySelected.FirstOrDefault();
            if (replayCard != null)
            {
                replayCard.BaseReplayCount++;
                CardCmd.Preview(replayCard);
            }
        }

        // 2. 如果反驳计数>=3，触发变化效果
        var rebuttalCount = EmalinCombatHelper.GetRebuttalPlaysThisTurn(
            owner.Creature, CombatState);
        if (rebuttalCount < 3) return;

        // 选择一张手卡，读取其附魔类型
        var keywordHand = PileType.Hand.GetPile(owner).Cards.ToList();
        if (keywordHand.Count == 0) return;

        var keywordPrefs = new CardSelectorPrefs(SelectionScreenPrompt, 1, 1);
        var keywordSelected = await CardSelectCmd.FromHand(
            choiceContext, owner, keywordPrefs,
            c => c.Enchantment is Rebuttal or Agreement or Doubt, this);
        var keywordCard = keywordSelected.FirstOrDefault();
        if (keywordCard?.Enchantment == null) return;

        var enchantType = keywordCard.Enchantment.GetType();

        // 从所有区域选择带有相同附魔的卡
        var allPileCards = PileType.Draw.GetPile(owner).Cards
            .Concat(PileType.Hand.GetPile(owner).Cards)
            .Concat(PileType.Discard.GetPile(owner).Cards)
            .Where(c => c != keywordCard && c.Enchantment?.GetType() == enchantType)
            .Distinct()
            .ToList();

        if (allPileCards.Count == 0) return;

        var transformPrefs = new CardSelectorPrefs(SelectionScreenPrompt, 0, allPileCards.Count);
        var toTransform = await CardSelectCmd.FromSimpleGrid(
            choiceContext, allPileCards, owner, transformPrefs);

        if (!toTransform.Any()) return;

        // 从三种附魔中随机选一种，所有新卡统一使用
        var enchantTypes = new[] { typeof(Rebuttal), typeof(Agreement), typeof(Doubt) };
        var chosenEnchantType = enchantTypes[owner.RunState.Rng.CombatCardSelection.NextInt(enchantTypes.Length)];

        // 提前获取 Canonical 实例
        var rebuttalCanonical = ModelDb.Enchantment<Rebuttal>();
        var agreementCanonical = ModelDb.Enchantment<Agreement>();
        var doubtCanonical = ModelDb.Enchantment<Doubt>();

        foreach (var card in toTransform)
        {
            if (card.CombatState == null) continue;

            var poolCards = owner.Character.CardPool.AllCards
                .Where(c => c.Rarity != CardRarity.Basic)
                .ToList();

            if (poolCards.Count == 0) continue;

            var newCardTemplate = owner.RunState.Rng.CombatCardSelection.NextItem(poolCards);
            var newCard = CombatState.CreateCard(newCardTemplate, owner);

            if (chosenEnchantType == typeof(Rebuttal))
                CardCmd.Enchant(rebuttalCanonical.ToMutable(), newCard, 1m);
            else if (chosenEnchantType == typeof(Agreement))
                CardCmd.Enchant(agreementCanonical.ToMutable(), newCard, 1m);
            else
                CardCmd.Enchant(doubtCanonical.ToMutable(), newCard, 1m);

            await CardCmd.Exhaust(choiceContext, card);
            await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, owner, CardPilePosition.Bottom);
            CardCmd.Preview(newCard);
        }
    }
    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}