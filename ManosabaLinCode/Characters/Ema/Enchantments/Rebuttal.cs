using ManosabaLin.Characters.Ema.Relics;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Models.Powers;

namespace ManosabaLin.Characters.Emalin.Enchantments;

[RegisterEnchantment]
public class Rebuttal : ModEnchantmentTemplate
{
    public override bool ShowAmount => true;
    public override bool HasExtraCardText => true;

    public override EnchantmentAssetProfile AssetProfile => new(
        IconPath: "res://ManosabaLin/images/enchantments/rebuttal.png"
    );

    public override int EnchantPlayCount(int originalPlayCount)
    {
        var badge = Card?.Owner?.Relics.OfType<EmaTrialBadge>().FirstOrDefault();
        var count = badge?.RebuttalCount ?? 0;
        return (count + 1) % 5 == 0 ? originalPlayCount + 1 : originalPlayCount;
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        var card = Card;
        var owner = card.Owner;
        var ownerCreature = owner.Creature;

        // 从遗物获取计数器并递增
        var badge = owner.Relics.OfType<EmaTrialBadge>().FirstOrDefault();
        badge?.IncrementCount(this);
        var count = badge?.RebuttalCount ?? 0;

        // ×1：每点计数造成1点伤害
        if (cardPlay?.Target is { IsAlive: true } target)
            await CreatureCmd.Damage(choiceContext, target, 1m,
                ValueProp.Unpowered, ownerCreature, null);

        // ×2：获得1点力量
        if (count % 2 == 0)
            await PowerCmd.Apply<StrengthPower>(choiceContext, ownerCreature, 1m, ownerCreature, null, false);

        // ×3：敌方全体1层易伤
        if (count % 3 == 0)
        {
            var enemies = card.CombatState.Enemies.Where(e => e is { IsAlive: true });
            foreach (var enemy in enemies)
                await PowerCmd.Apply<VulnerablePower>(choiceContext, enemy, 1m, ownerCreature, null, false);
        }

        // ×4：按计数器数值造成等次数的1点伤害
        if (count % 4 == 0)
        {
            for (var i = 0; i < count; i++)
            {
                var randomTarget = card.CombatState.Enemies
                    .Where(e => e is { IsAlive: true })
                    .ToList();
                if (randomTarget.Count == 0) break;
                var t = randomTarget[Random.Shared.Next(randomTarget.Count)];
                await CreatureCmd.Damage(choiceContext, t, 1m,
                    ValueProp.Unpowered, ownerCreature, null);
            }
        }
    }
}