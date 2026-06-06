using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Enchantments;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Hiro.Relics;

[RegisterRelic(typeof(HiroRelicPool))]
public sealed class Bloodiedclothing : ManosabaRelicTemplate
{
    public const int MaxHpLoss = 26;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool HasUponPickupEffect => true;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            foreach (var tip in HoverTipFactory.FromEnchantment<Adroit>())
                yield return tip;
        }
    }

    public override async Task AfterObtained()
    {
        var relic = this;

        await CreatureCmd.LoseMaxHp(
            new BlockingPlayerChoiceContext(),
            relic.Owner.Creature,
            MaxHpLoss,
            false);

        var skillCards = PileType.Deck.GetPile(relic.Owner)
            .Cards
            .Where(c => c.Type == CardType.Skill && c.Enchantment == null)
            .ToList();

        foreach (var card in skillCards)
            CardCmd.Enchant<Adroit>(card, 4m);
    }
}