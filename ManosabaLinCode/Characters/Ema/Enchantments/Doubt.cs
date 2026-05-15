using ManosabaLin.Characters.Ema.Relics;
using ManosabaLin.Characters.Hiro.Enchantments;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Emalin.Enchantments;

[RegisterEnchantment]
public class Doubt : ModEnchantmentTemplate
{
    public override bool ShowAmount => true;
    public override bool HasExtraCardText => true;

    public override EnchantmentAssetProfile AssetProfile => new(
        IconPath: "res://ManosabaLin/images/enchantments/doubt.png"
    );

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        var card = Card;
        var owner = card.Owner;
        var ownerCreature = owner.Creature;

        var badge = owner.Relics.OfType<EmaTrialBadge>().FirstOrDefault();
        badge?.IncrementCount(this);
        var count = badge?.DoubtCount ?? 0;

        // ×1：获得1点护盾
        await CreatureCmd.GainBlock(ownerCreature, 1m, ValueProp.Move, cardPlay);

        // ×2：抽一张卡
        if (count % 2 == 0)
            await CardPileCmd.Draw(choiceContext, 1m, owner);

        // ×3：恢复一点能量
        if (count % 3 == 0)
            await PlayerCmd.GainEnergy(1m, owner);

        // ×4：从弃牌堆随机拿回一张卡到手卡
        if (count % 4 == 0)
        {
            var discardPile = PileType.Discard.GetPile(owner);
            var discardCards = discardPile.Cards.ToList();
            if (discardCards.Count > 0)
            {
                var retrieved = owner.RunState.Rng.CombatCardSelection.NextItem(discardCards);
                await CardPileCmd.Add(retrieved, PileType.Hand);
            }
        }

        // ×5：随机一张手牌获得重放，选择一张牌组卡获得反驳/赞同/疑问附魔
        // ×5：随机一张手牌获得重放，随机一张手牌获得随机审判附魔
        if (count % 5 == 0)
        {
            var handCards = PileType.Hand.GetPile(owner).Cards
                .Where(c => c != card)
                .ToList();

            if (handCards.Count > 0)
            {
                var rng = owner.RunState.Rng.CombatCardSelection;

                // 随机一张手牌获得重放
                var replayCard = rng.NextItem(handCards);
                replayCard.BaseReplayCount++;
                CardCmd.Preview(replayCard);

                // 随机一张手牌获得随机审判附魔（排除刚给过重放的）
                var enchantTargets = handCards.Where(c => c != replayCard).ToList();
                if (enchantTargets.Count > 0)
                {
                    var enchantCard = rng.NextItem(enchantTargets);
                    var enchantmentOptions = new EnchantmentModel[]
                    {
                        ModelDb.Enchantment<Rebuttal>().ToMutable(),
                        ModelDb.Enchantment<Agreement>().ToMutable(),
                        ModelDb.Enchantment<Doubt>().ToMutable()
                    };
                    var chosenEnchantment = rng.NextItem(enchantmentOptions);
                    CardCmd.Enchant(chosenEnchantment, enchantCard, 1m);
                    CardCmd.Preview(enchantCard);
                }
            }
        }
    }
}