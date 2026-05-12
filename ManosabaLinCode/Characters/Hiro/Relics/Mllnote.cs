using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Hiro.Relics;

[RegisterRelic(typeof(HiroRelicPool))]
public sealed class Mllnote : ManosabaRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override bool HasUponPickupEffect => true;

    public override async Task AfterObtained()
    {
        var relic = this;

        // 移除卡组里所有死亡回溯
        var deathRewindCards = PileType.Deck.GetPile(relic.Owner)
            .Cards
            .Where(c => c is DeathRewind)
            .ToList();

        foreach (var card in deathRewindCards)
            await CardPileCmd.RemoveFromDeck(card, showPreview: false);

        // 加一张新的死亡回溯进卡组
        var newCard = relic.Owner.RunState.CreateCard<DeathRewind>(relic.Owner);
        CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(newCard, PileType.Deck));
    }
}
