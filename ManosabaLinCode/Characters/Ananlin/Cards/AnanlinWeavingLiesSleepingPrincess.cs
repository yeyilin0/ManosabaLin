using ManosabaLin.Characters.Ananlin.Powers;
using ManosabaLin.Characters.Ananlin.Relics;
using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Common.Powers;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace ManosabaLin.Characters.Ananlin.Cards;

[RegisterCard(typeof(AnanlinCardPool))]
public sealed class AnanlinWeavingLiesSleepingPrincess()
    : ManosabaCardTemplate(4, CardType.Skill, CardRarity.Rare, TargetType.Self),
        IAnanlinPeaceOfMindSpecialCard
{
    private const string SilencePerLieKey = "SilencePerLie";
    private const string PeaceThresholdKey = "PeaceThreshold";
    private const string SelfLossKey = "SelfLoss";
    private const string WitchPerGeneratedCardKey = "WitchPerGeneratedCard";

    protected override IEnumerable<ICardComponent> CanonicalComponents =>
    [
        new SleepingPrincessLie()
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<SilentPower>(SilencePerLieKey, 3m),
        new PowerVar<AnanlinLiePower>(1m),
        new PowerVar<AnanlinPeaceOfMindPower>(PeaceThresholdKey, 3m),
        new DamageVar(SelfLossKey, 1m, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move),
        new PowerVar<TempStrengthDown>(1m),
        new PowerVar<WithPower>(WitchPerGeneratedCardKey, 50m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<SilentPower>(),
        HoverTipFactory.FromPower<AnanlinLiePower>(),
        HoverTipFactory.FromPower<AnanlinPeaceOfMindPower>(),
        HoverTipFactory.FromPower<TempStrengthDown>(),
        HoverTipFactory.FromPower<WithPower>()
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);

        var sketchbook = this.Sketchbook();
        var lieCount = Math.Max(0, CurrentSilence() / DynamicVars[SilencePerLieKey].IntValue);
        if (lieCount > 0)
            await PowerCmd.Apply<AnanlinLiePower>(choiceContext, Owner.Creature, lieCount, Owner.Creature, this);

        var totalLies = CurrentLies();
        var lostPeace = await this.LosePeaceOfMind(choiceContext, int.MaxValue);
        var attackIntentEnemies = CombatState.Enemies
            .Where(static enemy => enemy.IsAlive)
            .Where(enemy => HasAttackIntent(enemy.Monster?.NextMove))
            .ToArray();

        foreach (var enemy in attackIntentEnemies)
            sketchbook?.TryForgetRecordedAttack(enemy);

        if (totalLies > 0)
        {
            if (lostPeace >= DynamicVars[PeaceThresholdKey].IntValue)
                RewriteEnemiesToLoseLife(totalLies);
            else if (lostPeace > 0)
                await ApplyTemporaryStrengthDown(choiceContext, lostPeace * totalLies);
        }

        await ConsumeWitchificationAndGenerateRetainCards(choiceContext, sketchbook);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }

    private int CurrentSilence()
    {
        return Math.Max(0, (int)(Owner.Creature.GetPower<SilentPower>()?.Amount ?? 0));
    }

    private int CurrentLies()
    {
        return Math.Max(0, (int)(Owner.Creature.GetPower<AnanlinLiePower>()?.Amount ?? 0));
    }

    private void RewriteEnemiesToLoseLife(int lieCount)
    {
        var rewritten = 0;
        foreach (var enemy in CombatState.Enemies.Where(static enemy => enemy is { IsAlive: true, Monster: not null }))
        {
            enemy.Monster!.SetMoveImmediate(CreateSelfLossMove(enemy.Monster, lieCount), forceTransition: true);
            rewritten++;
        }

        AnanlinSilenceIntentManager.RecordIntentRewrites(CombatState, rewritten);
    }

    private async Task ApplyTemporaryStrengthDown(PlayerChoiceContext choiceContext, int amount)
    {
        if (amount <= 0) return;

        foreach (var enemy in CombatState.Enemies.Where(static enemy => enemy.IsAlive))
            await PowerCmd.Apply<TempStrengthDown>(choiceContext, enemy, amount, Owner.Creature, this);
    }

    private MoveState CreateSelfLossMove(MonsterModel monster, int hitCount)
    {
        var damage = DynamicVars[SelfLossKey].BaseValue;
        return new MoveState(
            $"MANOSABA_LIN_ANANLIN_SLEEPING_PRINCESS_LIE_{hitCount}",
            async _ =>
            {
                var context = new ThrowingPlayerChoiceContext();
                for (var i = 0; i < hitCount; i++)
                {
                    await CreatureCmd.Damage(
                        context,
                        monster.Creature,
                        damage,
                        ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
                        monster.Creature,
                        this,
                        null);
                }
            },
            new MultiAttackIntent((int)damage, hitCount));
    }

    private async Task ConsumeWitchificationAndGenerateRetainCards(
        PlayerChoiceContext choiceContext,
        AnansSketchbook? sketchbook)
    {
        var witch = Owner.Creature.GetPower<WithPower>();
        var witchAmount = Math.Max(0, (int)(witch?.Amount ?? 0));
        if (witch is not null && witchAmount > 0)
            await PowerCmd.ModifyAmount(choiceContext, witch, -witchAmount, Owner.Creature, this);

        if (sketchbook is null || witchAmount <= 0) return;

        var count = witchAmount / DynamicVars[WitchPerGeneratedCardKey].IntValue;
        for (var i = 0; i < count; i++)
        {
            var card = RollPlayableRecordedCard(sketchbook);
            if (card is null) break;

            card.AddKeyword(CardKeyword.Retain);
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
        }
    }

    private CardModel? RollPlayableRecordedCard(AnansSketchbook sketchbook)
    {
        if (CombatState is not { } combatState) return null;

        var rng = Owner.RunState.Rng.CombatCardGeneration;
        var candidates = sketchbook
            .GetRecordedCardPools()
            .SelectMany(pool => sketchbook.GetRecordableCardsFromPool(pool))
            .Where(static template => !template.EnergyCost.CostsX)
            .OrderBy(_ => rng.NextFloat())
            .ToArray();

        foreach (var template in candidates)
        {
            var card = combatState.CreateCard(template, Owner);
            card.AddKeyword(CardKeyword.Retain);
            if (IsCurrentlyPlayable(card, combatState))
                return card;
        }

        return null;
    }

    private static bool HasAttackIntent(MoveState? move)
    {
        return move?.Intents.Any(static intent => intent is AttackIntent) == true;
    }

    private static bool IsCurrentlyPlayable(CardModel card, ICombatState combatState)
    {
        if (!HasValidTarget(card, combatState)) return false;

        return card.CanPlay();
    }

    private static bool HasValidTarget(CardModel card, ICombatState combatState)
    {
        return card.TargetType switch
        {
            TargetType.AnyEnemy => combatState.Enemies.Any(card.IsValidTarget),
            TargetType.AnyAlly => combatState.PlayerCreatures.Any(card.IsValidTarget),
            _ => card.IsValidTarget(null)
        };
    }
}
