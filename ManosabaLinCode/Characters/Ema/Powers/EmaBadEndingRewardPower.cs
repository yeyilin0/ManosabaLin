using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Cards;
using ManosabaLin.Characters.Ema.Vfx;
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
    private const int RewardCardCount = 2;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (cardSource is not EmaBadEnding) BadEndingVfx.Restore(this);
        return Task.CompletedTask;
    }

    public override Task BeforeCombatStart()
    {
        BadEndingVfx.Restore(this);
        return Task.CompletedTask;
    }

    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        BadEndingVfx.Restore(this);
        return Task.CompletedTask;
    }

    public override Task AfterRemoved(Creature oldOwner)
    {
        BadEndingVfx.Finish(oldOwner);
        return Task.CompletedTask;
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        if (!Owner.IsPlayer) return;
        var target = Owner.Player!.Deck.Cards.OfType<EmaEnding>().FirstOrDefault();
        if (target is null) return;

        var cards = Owner.Player!.Deck.Cards
            .Where(c => c is not EmaEnding && c.Rarity is CardRarity.Rare or CardRarity.Uncommon)
            .ToList()
            .StableShuffle(Owner.Player.RunState.Rng.Shuffle)
            .Take(RewardCardCount);

        var index = 0;
        foreach (var card in cards)
        {
            if (target.AddComponent(new EmaBadEndingRewardComponent(card)) != null)
                BadEndingVfx.NotifyReward(Owner, card, index++);
        }

    }
}
