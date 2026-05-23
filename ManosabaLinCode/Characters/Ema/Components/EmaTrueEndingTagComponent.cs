using ManosabaLin.Characters.Ema.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MinionLib.Component;
using MinionLib.Component.Core;

namespace ManosabaLin.Characters.Emalin.Components;

public sealed partial class EmaTrueEndingTagComponent : CardComponent
{
    [LocArg]
    private bool CanIncreaseAchieveCount => Card?.Owner.Creature.Powers.OfType<EmaTrueEndingPower>()
        .Any(p => !p.AchievedCards.Contains(Card.Id.Entry)) ?? false;

    public override PileType? GetResultPileTypeForCardPlay() => PileType.None;
}
