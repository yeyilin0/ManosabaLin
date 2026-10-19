using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Sherrylin.Orbs;
using ManosabaLin.Characters.Sherrylin.Vfx;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Cards.Emotions;

[RegisterCard(typeof(LinCardPool))]
// 图鉴可见（第 4 参 = shouldShowInCardLibrary；基类默认 false）。
public sealed class EmotionFriendship() : CaseFileCard<EmotionFriendshipOrb>(-1, CardRarity.Ancient, TargetType.Self, true)
{
    public override int MaxUpgradeLevel => 0;
    protected override IEnumerable<string> ExtraRunAssetPaths => base.ExtraRunAssetPaths.Concat(FriendshipVfx.AssetPaths);
    protected override IEnumerable<ICardComponent> CanonicalComponents =>
        [new UniqueComponent()];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        using var friendship = FriendshipVfx.Open(Owner.Creature);
        var field = FriendshipFieldVfx.Open(Owner.Creature);
        try { await base.OnPlay(choiceContext, cardPlay, componentContext); }
        finally { field?.CompleteChanneling(); }
        if (friendship != null) await friendship.Finished;
    }

    protected override Task AfterRoomEntered(AbstractRoom room, ComponentContext componentContext)
    {
        if (room is CombatRoom) FriendshipVfx.Prepare(Owner.Creature);
        return Task.CompletedTask;
    }

    protected override Task BeforeCombatStart(ComponentContext componentContext)
    {
        FriendshipVfx.Prepare(Owner.Creature);
        return Task.CompletedTask;
    }

    protected override Task AfterCardEnteredCombat(CardModel card, ComponentContext componentContext)
    {
        if (ReferenceEquals(card, this)) FriendshipVfx.Prepare(Owner.Creature);
        return Task.CompletedTask;
    }
}
