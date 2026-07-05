using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using ManosabaLin.Characters.Hiro.Cards;
using System.Linq;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public sealed class ThirteenWaterPlayerIntelPower : ManosabaPowerTemplate
{
    private const int IntelTarget = 13;
    private int _gainedThisTurn;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
       

        // 累计 ≥13 层 → 移除能力 + 所有玩家各生成一张 ThirteenWater
        if (Amount >= IntelTarget)
        {
            await PowerCmd.Remove(this);

            foreach (var player in Owner.CombatState.Players.Where(p => p.Creature.IsAlive))
            {
                var card = Owner.CombatState.CreateCard<ThirteenWater>(player);
                await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
                var deckPile = PileType.Deck.GetPile(player);
                await CardPileCmd.Add(card, deckPile, CardPilePosition.Random);
            }
        }
    }
}
