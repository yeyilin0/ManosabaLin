using ManosabaLin.Characters.Common.Powers;
using ManosabaLin.Characters.Ema.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
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

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        var card = Card;
        var owner = card.Owner;
        var ownerCreature = owner.Creature;
        var combatState = card.CombatState;

        // 从遗物获取计数器
        var badge = owner.Relics.OfType<EmaTrialBadge>().FirstOrDefault();
        var count = badge?.AgreeCount ?? 0;

        // 增加计数
        badge?.IncrementCount(this);
        count = badge?.AgreeCount ?? count + 1;

        // ×1：全体友方3护盾
        foreach (var ally in combatState.Allies.Where(a => a is { IsAlive: true }))
            await CreatureCmd.GainBlock(ally, 3m, ValueProp.Move, cardPlay);

        // ×2：自己3护盾
        if (count % 2 == 0)
            await CreatureCmd.GainBlock(ownerCreature, 3m, ValueProp.Move, cardPlay);

        // ×3：全体临时迅捷
        if (count % 3 == 0)
        {
            foreach (var ally in combatState.Allies.Where(a => a is { IsAlive: true }))
                await PowerCmd.Apply<TempDexterity>(choiceContext, ally, 1m, ownerCreature, null, false);
        }

        // ×4：全体临时力量
        if (count % 4 == 0)
        {
            foreach (var ally in combatState.Allies.Where(a => a is { IsAlive: true }))
                await PowerCmd.Apply<TempStrength>(choiceContext, ally, 2m, ownerCreature, null, false);
        }

        // ×5：下一张免费 + 全体1能量
        if (count % 5 == 0)
        {
            var nextCard = PileType.Hand.GetPile(owner)
                .Cards.FirstOrDefault(c => c != card && c.CanPlay());
            nextCard?.SetToFreeThisTurn();

            foreach (var p in combatState.Players)
                await PlayerCmd.GainEnergy(1m, p);
        }
    }
}