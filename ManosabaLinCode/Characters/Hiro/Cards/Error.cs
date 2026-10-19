using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using ManosabaLin.Characters.Hiro.Vfx;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(LinCardPool))]
public sealed class Error : ManosabaCardTemplate
{
    private const string TheEndEffectHoverLocEntry = "MANOSABA_LIN_CARD_THE_END_EFFECT";

    private int _handAttackCount;

    public Error() : base(2, CardType.Attack, CardRarity.Ancient, TargetType.AnyEnemy)
    {
    }

    public override CardAssetProfile AssetProfile => base.AssetProfile with
    {
        AncientTextBgPath = "ancient_empty_text_bg.png".CardsImagePath()
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Retain; }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromCard<TheEnd>(),
        CardEffectHoverTipFactory.FromCard(
            ModelDb.Card<TheEnd>(),
            TheEndEffectHoverLocEntry),
       
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DynamicVar("ErrorHandAttackCount", 0m);
            yield return new DynamicVar("TokenThreshold", 12m);
        }
    }

    protected override Task AfterCardEnteredCombat(CardModel card, ComponentContext componentContext)
    {
        if (card != this || IsClone)
            return Task.CompletedTask;

        _handAttackCount = 0;
        SyncHandAttackCountVar();
        return Task.CompletedTask;
    }

    protected override Task BeforeCardPlayed(CardPlay cardPlay, ComponentContext componentContext)
    {
        if (cardPlay.Card == this)
            return Task.CompletedTask;
        if (cardPlay.Card.Owner != Owner)
            return Task.CompletedTask;
        if (cardPlay.Card.Type != CardType.Attack)
            return Task.CompletedTask;

        _handAttackCount++;
        SyncHandAttackCountVar();
        return Task.CompletedTask;
    }

    private void SyncHandAttackCountVar()
    {
        _ = DynamicVars;
        DynamicVars["ErrorHandAttackCount"].BaseValue = _handAttackCount;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var target = cardPlay.Target;
        if (target == null) return;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        var handAttackCount = (int)DynamicVars["ErrorHandAttackCount"].BaseValue;
        var hpLoss = (int)(target.MaxHp * 0.01m * handAttackCount);
        var earnsTheEnd = handAttackCount >= 12;
        var glass = ErrorGlassVfx.Play(source.Owner.Creature, earnsTheEnd);
        try
        {
            if (glass != null && !await glass.Impact)
                return;
            if (hpLoss > 0)
                await CreatureCmd.Damage(choiceContext, target, hpLoss, DamageProps.cardHpLoss, source, cardPlay);

            if (earnsTheEnd)
            {
                var token = source.CombatState.CreateCard<TheEnd>(source.Owner);
                await CardPileCmd.AddGeneratedCardToCombat(token, PileType.Hand, source.Owner);
                if (glass != null && await glass.Prepared)
                    glass.RetainUntilTheEnd();
            }
            else if (glass != null)
                await glass.Finished;

        }
        finally
        {
            if (glass?.IsRetained != true)
                glass?.Dispose();
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
