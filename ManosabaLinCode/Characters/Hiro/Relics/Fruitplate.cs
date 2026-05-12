using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Enchantments;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models.Enchantments;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Models;

namespace ManosabaLin.Characters.Hiro.Relics;

[RegisterRelic(typeof(HiroRelicPool))]
public sealed class Fruitplate : ManosabaRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        var relic = this;

        var prefs = new CardSelectorPrefs(relic.SelectionScreenPrompt, 1, 1);
        var emaapple = ModelDb.Enchantment<Emaapple>();

        var selected = await CardSelectCmd.FromDeckForEnchantment(
            relic.Owner,
            emaapple,
            1,
            prefs);

        foreach (var card in selected)
        {
            CardCmd.Enchant(emaapple.ToMutable(), card, 1m);
            CardCmd.Preview(card);
        }
    }
}
