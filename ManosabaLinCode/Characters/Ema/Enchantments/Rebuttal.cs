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

    public override void RecalculateValues()
    {
        // 每回合重置计数
        Amount = 0;
    }

    // ×5：计数器到达5的倍数时多打出一次整张卡
    public override int EnchantPlayCount(int originalPlayCount)
    {
        // OnPlay 会先 +1，所以检查 (Amount + 1) % 5
        return (Amount + 1) % 5 == 0 ? originalPlayCount + 1 : originalPlayCount;
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        Amount++;
        var count = Amount;
        var card = Card;
        var owner = card.Owner.Creature;

        // ×1：每点计数造成1点伤害（每次都触发）
        if (cardPlay?.Target is { IsAlive: true } target)
            await CreatureCmd.Damage(choiceContext, target, 1m,
                ValueProp.Unpowered, owner, null);
        // ×2：获得1点力量
        if (count % 2 == 0)
            await PowerCmd.Apply<StrengthPower>(choiceContext, owner, 1m, owner, null);

        // ×3：给敌方全体1层易伤
        if (count % 3 == 0)
        {
            var enemies = card.CombatState.Enemies.Where(e => e is { IsAlive: true });
            foreach (var enemy in enemies)
                await PowerCmd.Apply<VulnerablePower>(choiceContext, enemy, 1m, owner, null);
        }

        // ×4：按照计数器数值造成等次数的1点伤害
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
                    ValueProp.Unpowered, owner, null);
            }
        }

    }
}
