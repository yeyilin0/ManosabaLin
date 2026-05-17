using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using System.Linq;

namespace ManosabaLin.Characters.Emalin.Enchantments;

[RegisterEnchantment]
public class Mlypower : ModEnchantmentTemplate
{
    public override bool ShowAmount => false;
    public override bool HasExtraCardText => true;

    public override EnchantmentAssetProfile AssetProfile => new(
        IconPath: "res://ManosabaLin/images/enchantments/mlypower.png"
    );

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        // 受到 1 点不受能力影响但可被格挡的伤害
        await CreatureCmd.Damage(
            choiceContext,
            Card.Owner.Creature,
            1m,
            ValueProp.Unpowered,    // 不受力量等能力影响，但可被格挡
            null,
            Card);
    }
}