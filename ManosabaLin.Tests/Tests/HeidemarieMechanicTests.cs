using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Heidemarie.Mechanics;
using ManosabaLin.Characters.Heidemarie.Powers;
using ManosabaLin.Characters.Hiro;
using ManosabaLin.Characters.Hiro.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Component.Core;
using STS2RitsuLib.Interop.AutoRegistration;
using TestTheSpire;
using Xunit;

namespace ManosabaLin.Tests.Cases;

public sealed class HeidemarieMechanicTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<Hiro>()
            .AddEnemy<BigDummy>()
            .WithSeed("manosabalin-heidemarie-mechanics");
    }

    protected override Task InitializeAsync()
    {
        SwordTokenGeneration.RegisterTokenCard<TestAuroraSwordCard>(SwordTokenKind.Aurora);
        SwordTokenGeneration.RegisterTokenCard<TestCrimsonSwordCard>(SwordTokenKind.Crimson);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task AuroraChain_only_increases_sword_grave_attack_damage()
    {
        await ApplyPower<AuroraChainPower>(Player.Creature, 3, Player.Creature);

        var enemy = EnemyAt(0);
        var plain = await AddToHand<TestPlainAttackCard>();
        var beforePlain = enemy.CurrentHp;
        await Play(plain, enemy);
        var plainDamage = beforePlain - enemy.CurrentHp;

        var sword = await AddToHand<TestAuroraSwordCard>();
        var beforeSword = enemy.CurrentHp;
        await Play(sword, enemy);
        var swordDamage = beforeSword - enemy.CurrentHp;

        Assert.Equal(TestPlainAttackCard.BaseDamage, plainDamage);
        Assert.True(swordDamage > plainDamage);
    }

    [Fact]
    public async Task Mark_consumes_one_stack_and_adds_fixed_damage_on_single_player_owner_attack()
    {
        var mark = await ApplyPower<MarkPower>(Player.Creature, 2, Player.Creature);
        Assert.NotNull(mark);

        var enemy = EnemyAt(0);
        var attack = await AddToHand<TestPlainAttackCard>();
        var hpBefore = enemy.CurrentHp;

        await Play(attack, enemy);

        Assert.Equal(1, Player.Creature.GetPower<MarkPower>()?.Amount);
        Assert.Equal(TestPlainAttackCard.BaseDamage + MarkPower.Damage, hpBefore - enemy.CurrentHp);
    }

    [Fact]
    public async Task SwordGeneration_replacement_preserves_original_request_and_hand_full_success()
    {
        var listener = await ApplyPower<TestSwordGenerationListenerPower>(Player.Creature, 1, Player.Creature);
        Assert.NotNull(listener);

        var setupAttack = await AddToHand<TestPlainAttackCard>();
        await Play(setupAttack, EnemyAt(0));

        var hand = PileType.Hand.GetPile(Player);
        while (hand.Cards.Count < CardPile.MaxCardsInHand)
            await AddToHand<HiroDefend>();

        var result = await SwordTokenGeneration.AddAuroraSwordsToHand(
            new ThrowingPlayerChoiceContext(),
            Player,
            1,
            listener);

        Assert.Equal(SwordTokenKind.Aurora, result.Request.OriginalKind);
        Assert.Equal(1, result.SuccessCount);
        Assert.Equal(1, result.SuccessCountFor(SwordTokenKind.Crimson));
        Assert.Equal(0, result.EnteredHandCountFor(SwordTokenKind.Crimson));
        Assert.Contains(result.Cards.Single().Card, PileType.Discard.GetPile(Player).Cards);

        Assert.Equal([SwordTokenKind.Aurora], listener.RequestedOriginalKinds);
        Assert.Equal([SwordTokenKind.Aurora], listener.SucceededOriginalKinds);
    }

}

[RegisterCard(typeof(LinCardPool))]
public sealed class TestPlainAttackCard()
    : ManosabaCardTemplate(0, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy, false)
{
    public const int BaseDamage = 2;

    public override CardPoolModel Pool => ModelDb.CardPool<LinCardPool>();

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(BaseDamage, ValueProp.Move)];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);
    }
}

[RegisterCard(typeof(LinCardPool))]
public sealed class TestAuroraSwordCard()
    : ManosabaCardTemplate(0, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy, false), IAuroraSwordCard
{
    public override CardPoolModel Pool => ModelDb.CardPool<LinCardPool>();

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(TestPlainAttackCard.BaseDamage, ValueProp.Move)];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .Execute(choiceContext);
    }
}

[RegisterCard(typeof(LinCardPool))]
public sealed class TestCrimsonSwordCard()
    : ManosabaCardTemplate(0, CardType.Attack, CardRarity.Token, TargetType.AnyEnemy, false), ICrimsonSwordCard
{
    public override CardPoolModel Pool => ModelDb.CardPool<LinCardPool>();

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(TestPlainAttackCard.BaseDamage, ValueProp.Move)];

    protected override Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        return Task.CompletedTask;
    }
}

[RegisterPower]
public sealed class TestSwordGenerationListenerPower : ManosabaPowerTemplate, ISwordGenerationListener
{
    public List<SwordTokenKind> RequestedOriginalKinds { get; } = [];
    public List<SwordTokenKind> SucceededOriginalKinds { get; } = [];

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public Task OnSwordGenerationRequested(PlayerChoiceContext choiceContext, SwordGenerationRequest request)
    {
        RequestedOriginalKinds.Add(request.OriginalKind);
        request.ReplaceWith(SwordTokenKind.Crimson, request.OriginalCount);
        return Task.CompletedTask;
    }

    public Task OnSwordGenerationSucceeded(
        PlayerChoiceContext choiceContext,
        SwordGenerationRequest request,
        SwordGenerationResult result)
    {
        SucceededOriginalKinds.Add(request.OriginalKind);
        return Task.CompletedTask;
    }
}
