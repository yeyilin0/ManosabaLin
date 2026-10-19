using ManosabaLin.Characters.Common.Powers;
using ManosabaLin.Characters.Ema.Powers;
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
        var owner = card?.Owner;
        if (card == null || owner == null) return;

        var ownerCreature = owner.Creature;
        var combatState = card.CombatState ?? ownerCreature?.CombatState;
        if (ownerCreature == null || combatState == null) return;

        var badge = owner.Relics.OfType<EmaTrialBadge>().FirstOrDefault();
        var count = 1;
        if (badge is not null)
        {
            badge.IncrementCount(this);
            count = badge.AgreeCount;
        }

        var allies = combatState.Allies.Where(static a => a is { IsAlive: true }).ToList();

        // ×1：全体友方3护盾
        foreach (var ally in allies)
            await CreatureCmd.GainBlock(ally, 3m, ValueProp.Move, null);

        // ×2：自己3护盾
        if (count % 2 == 0)
            await CreatureCmd.GainBlock(ownerCreature, 3m, ValueProp.Move, null);

        // ×3：全体临时迅捷
        if (count % 3 == 0)
        {
            foreach (var ally in allies)
                await PowerCmd.Apply<TempDexterity>(choiceContext, ally, 1m, ownerCreature, null, false);
        }

        // ×4：全体临时力量
        if (count % 4 == 0)
        {
            foreach (var ally in allies)
                await PowerCmd.Apply<TempStrength>(choiceContext, ally, 2m, ownerCreature, null, false);
        }

        // ×5：下一张免费 + 全体1能量
        if (count % 5 == 0)
        {
            // ⚠️ 候选集必须两端一致：用 IsEligibleForCostEffect（不看能量）而不是 CanPlay()，
            //    否则 RNG 会在两个对端抽出不同的牌，被设为 0 费的那张不一致 ⇒ StateDivergence。
            var otherCards = PileType.Hand.GetPile(owner)
                .Cards.Where(c => c != card && EmalinCombatHelper.IsEligibleForCostEffect(c))
                .ToList();

            if (otherCards.Count > 0)
            {
                var rng = owner.RunState.Rng.CombatCardSelection;
                var randomCard = rng.NextItem(otherCards);
                randomCard?.SetToFreeThisTurn();
            }

            foreach (var p in combatState.Players)
                await PlayerCmd.GainEnergy(1m, p);
        }
    }
}
