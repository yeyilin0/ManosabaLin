using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using System.Linq;

namespace ManosabaLin.Characters.Emalin.Enchantments;

[RegisterEnchantment]
public class Witchification : ModEnchantmentTemplate
{
    public override bool ShowAmount => false;
    public override bool HasExtraCardText => true;

    public override EnchantmentAssetProfile AssetProfile => new(
        IconPath: "res://ManosabaLin/images/enchantments/witchification.png"
    );

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        var enemies = Card.Owner.Creature.CombatState.Enemies
            .Where(e => e is { IsAlive: true })
            .ToList();

        if (enemies.Count == 0) return;

        var target = Card.Owner.RunState.Rng.CombatTargets.NextItem(enemies);
        await CardCmd.AutoPlay(choiceContext, Card, target);
    }
}