using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Ema.Vfx;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Component.Core;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(LinCardPool))]
public sealed class EmaBadEnding() : ManosabaCardTemplate(3, CardType.Power, CardRarity.Ancient, TargetType.AllEnemies)
{
    private const string EffectHoverLocEntry = "MANOSABA_LIN_CARD_EMA_BAD_ENDING_EFFECT";
    protected override IEnumerable<string> ExtraRunAssetPaths => base.ExtraRunAssetPaths.Concat(BadEndingVfx.AssetPaths);

    public override CardAssetProfile AssetProfile => base.AssetProfile with
    {
        AncientTextBgPath = "ancient_empty_text_bg.png".CardsImagePath()
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(10, DamageProps.card)];

    public override int MaxUpgradeLevel => 0;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return CardEffectHoverTipFactory.FromCard(this, EffectHoverLocEntry);
            yield return HoverTipFactory.FromPower<EmaBadEndingPower>();
            yield return HoverTipFactory.FromPower<EmaBadEndingRewardPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var playback = BadEndingVfx.Open(Owner.Creature);
        await PowerCmd.Apply<EmaBadEndingPower>(choiceContext, CombatState!.Enemies, 1, Owner.Creature, this);

        if (playback != null) await playback.ImpactReady;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState)
            .Execute(choiceContext);

        await PowerCmd.Apply<EmaBadEndingRewardPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
        BadEndingVfx.NotifyDecision(Owner.Creature);
        if (playback != null) await playback.OpeningFinished;
    }

    protected override Task AfterRoomEntered(AbstractRoom room, ComponentContext componentContext)
    {
        if (room is CombatRoom)
        {
            BadEndingVfx.Prepare(Owner.Creature);
            if (Owner.Creature.GetPower<EmaBadEndingRewardPower>() is { } power) BadEndingVfx.Restore(power);
        }
        return Task.CompletedTask;
    }

    protected override Task BeforeCombatStart(ComponentContext componentContext)
    {
        BadEndingVfx.Prepare(Owner.Creature);
        return Task.CompletedTask;
    }

    protected override Task AfterCardEnteredCombat(CardModel card, ComponentContext componentContext)
    {
        if (ReferenceEquals(card, this)) BadEndingVfx.Prepare(Owner.Creature);
        return Task.CompletedTask;
    }
}
