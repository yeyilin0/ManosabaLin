using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Cards;
using ManosabaLin.Characters.Emalin.Components;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Rooms;
using MinionLib.Component.Interfaces;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Ema.Powers;

[RegisterPower]
public sealed class EmaBadEndingRewardPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        if (!Owner.IsPlayer) return;
        var target = Owner.Player!.Deck.Cards.OfType<EmaEnding>().FirstOrDefault();
        if (target is null) return;

        var cards = Owner.Player!.Deck.Cards
            .Where(c => c is not EmaEnding && c.Rarity is CardRarity.Rare or CardRarity.Uncommon)
            .ToList()
            .StableShuffle(Owner.Player.RunState.Rng.Shuffle)
            .Take(Amount);

        foreach (var card in cards)
            target.AddComponent(new EmaBadEndingRewardComponent(card));

    }
}
