using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Ema.Vfx;
using ManosabaLin.Characters.Emalin.Actions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Rooms;
using MinionLib.Component.Core;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(LinCardPool))]
public sealed class EmaTrueEnding() : ManosabaCardTemplate(2, CardType.Power, CardRarity.Ancient, TargetType.Self)
{
    private const string EffectHoverLocEntry = "MANOSABA_LIN_CARD_EMA_TRUE_ENDING_EFFECT";

    // The game's run loader retains this scene and its preload dependencies before card play.
    protected override IEnumerable<string> ExtraRunAssetPaths => base.ExtraRunAssetPaths.Concat(TrueEndingVfx.AssetPaths);

    public override CardAssetProfile AssetProfile => base.AssetProfile with
    {
        AncientTextBgPath = "ancient_empty_text_bg.png".CardsImagePath()
    };

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return CardEffectHoverTipFactory.FromCard(this, EffectHoverLocEntry);
            yield return HoverTipFactory.FromPower<EmaTrueEndingRewardAction>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var power = await PowerCmd.Apply<EmaTrueEndingPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
        if (power != null && TrueEndingVfx.Open(power) is { } playback)
            await playback.OpeningFinished;
    }

    protected override Task AfterRoomEntered(AbstractRoom room, ComponentContext componentContext)
    {
        if (room is CombatRoom) TrueEndingVfx.Prepare(Owner.Creature);
        // Room hooks include deck cards, but do not include combat powers.
        if (room is CombatRoom && Owner.Creature.GetPower<EmaTrueEndingPower>() is { } power)
            TrueEndingVfx.Restore(power);
        return Task.CompletedTask;
    }

    protected override Task BeforeCombatStart(ComponentContext componentContext)
    {
        TrueEndingVfx.Prepare(Owner.Creature);
        return Task.CompletedTask;
    }

    protected override Task AfterCardEnteredCombat(CardModel card, ComponentContext componentContext)
    {
        if (ReferenceEquals(card, this)) TrueEndingVfx.Prepare(Owner.Creature);
        return Task.CompletedTask;
    }
}
