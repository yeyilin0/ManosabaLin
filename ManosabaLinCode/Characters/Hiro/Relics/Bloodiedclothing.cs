using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Enchantments;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Hiro.Relics;

[RegisterRelic(typeof(HiroRelicPool))]
public sealed class Bloodiedclothing : ManosabaRelicTemplate
{
    public const int MaxHpLoss = 26;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        var relic = this;

        // 失去 26 最大生命上限
        await CreatureCmd.LoseMaxHp(
            new BlockingPlayerChoiceContext(),
            relic.Owner.Creature,
            MaxHpLoss,
            false);

        // 使牌组里所有技能牌获得伶俐附魔
        var skillCards = PileType.Deck.GetPile(relic.Owner)
            .Cards
            .Where(c => c.Type == CardType.Skill)
            .ToList();

        foreach (var card in skillCards)
            CardCmd.Enchant<Adroit>(card, 1m);
    }
}
