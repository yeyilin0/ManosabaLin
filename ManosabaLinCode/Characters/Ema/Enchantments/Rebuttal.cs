using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using System.Threading.Tasks;

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

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        Amount++;
    }
}
