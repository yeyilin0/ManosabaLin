// ThirteenWaterPlayerIntelPower.cs — 玩家身上的情报标记
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using ManosabaLin.Characters.Hiro.Cards;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public sealed class ThirteenWaterPlayerIntelPower : ManosabaPowerTemplate
{
    private const int IntelTarget = 13;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (Amount >= IntelTarget)
        {
            await PowerCmd.Remove(this);

            var player = Owner.Player;
            var card = Owner.CombatState.CreateCard<ThirteenWater>(player);
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
            var deckPile = PileType.Deck.GetPile(player);
            await CardPileCmd.Add(card, deckPile, CardPilePosition.Random);
        }
    }
}
