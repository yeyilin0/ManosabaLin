// Rebuttal.cs
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

        var badge = owner.Relics.OfType<EmaTrialBadge>().FirstOrDefault();

        int count;

        if (badge is not null)
        {
            badge.IncrementCount(this);
            count = badge.RebuttalCount;
        }
        else
        {
            count = 1;
        }

        // 脳1锛氭瘡鐐硅鏁伴€犳垚1鐐逛激瀹?
        if (cardPlay?.Target is { IsAlive: true } target)
            await CreatureCmd.Damage(choiceContext, target, 1m,
                ValueProp.Unpowered, ownerCreature, null, null);

        // 脳2锛氳幏寰?鐐瑰姏閲?
        if (count % 2 == 0)
            await PowerCmd.Apply<StrengthPower>(choiceContext, ownerCreature, 1m, ownerCreature, null, false);

        // 脳3锛氭晫鏂瑰叏浣?灞傛槗浼?
        if (count % 3 == 0)
        {
            var enemies = card.CombatState.Enemies.Where(e => e is { IsAlive: true });
            foreach (var enemy in enemies)
                await PowerCmd.Apply<VulnerablePower>(choiceContext, enemy, 1m, ownerCreature, null, false);
        }

        // 脳4锛氭寜璁℃暟鍣ㄦ暟鍊奸€犳垚绛夋鏁扮殑1鐐逛激瀹?
        if (count % 4 == 0)
        {
            for (var i = 0; i < count; i++)
            {
                var randomTarget = card.CombatState.Enemies
                    .Where(e => e is { IsAlive: true })
                    .ToList();
                if (randomTarget.Count == 0) break;
                var t = randomTarget[owner.RunState.Rng.CombatCardSelection.NextInt(randomTarget.Count)];
                await CreatureCmd.Damage(choiceContext, t, 1m,
                    ValueProp.Unpowered, ownerCreature, null, null);
            }
        }
    }
}
