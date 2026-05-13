using ManosabaLin.Characters.Common.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Emalin.Enchantments;

[RegisterEnchantment]
public class Agreement : ModEnchantmentTemplate
{
    public override bool ShowAmount => true;
    public override bool HasExtraCardText => true;

    public override EnchantmentAssetProfile AssetProfile => new(
        IconPath: "res://ManosabaLin/images/enchantments/agreement.png"
    );

    public override void RecalculateValues()
    {
        Amount = 0;
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        Amount++;
        var count = Amount;
        var card = Card;
        var owner = card.Owner.Creature;
        var combatState = card.CombatState;

        // ×1：全体友方包括自己获得3护盾（每次都触发）
        foreach (var ally in combatState.Allies.Where(a => a is { IsAlive: true }))
            await CreatureCmd.GainBlock(ally, 3m, ValueProp.Move, cardPlay);

        // ×2：自己获得3点护盾
        if (count % 2 == 0)
            await CreatureCmd.GainBlock(owner, 3m, ValueProp.Move, cardPlay);

        // ×3：全体友方获得1层临时迅捷
        if (count % 3 == 0)
        {
            foreach (var ally in combatState.Allies.Where(a => a is { IsAlive: true }))
                await PowerCmd.Apply<TempDexterity>(choiceContext, ally, 1m, owner, null);
        }

        // ×4：全体友方包括自己获得2层临时力量
        if (count % 4 == 0)
        {
            foreach (var ally in combatState.Allies.Where(a => a is { IsAlive: true }))
                await PowerCmd.Apply<TempStrength>(choiceContext, ally, 2m, owner, null);
        }

        // ×5：下一张卡免费打出，全体友方获得1点能量
        if (count % 5 == 0)
        {
            // 下一张手牌设为免费
            var nextCard = PileType.Hand.GetPile(card.Owner)
                .Cards.FirstOrDefault(c => c != card && c.CanPlay());
            nextCard?.SetToFreeThisTurn();

            // 全体友方获得1点能量
            foreach (var player in combatState.Players)
                await PlayerCmd.GainEnergy(1m, player);
        }
    }
}
