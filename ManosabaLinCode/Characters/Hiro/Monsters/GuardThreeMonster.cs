using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Godot;
using ManosabaLin.Characters.Ema.Afflictions;
using ManosabaLin.Characters.Hiro.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace ManosabaLin.Characters.Hiro.Monsters;

[RegisterMonster]
public sealed class GuardThreeMonster : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 300, 280);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 300, 280);

    private int AttackDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 14, 12);
    private int ErosionDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 14);
    private int WithAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 40, 30);
    private int ShieldAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 30, 25);
    private int JusticeAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 2, 3);
    private int MaxJustice => 5;
    private int Phase2SelfDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 20, 15);
    private int Phase2AttackDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 14);
    private int _phase2DeathTextCount;

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://ManosabaLin/scenes/monsters/guard_three.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals()
    {
        return RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(
            AssetProfile.VisualsScenePath!);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var erosionAttack = new MoveState("EROSION_ATTACK", ErosionAttackMove,
            new SingleAttackIntent(AttackDamage), new CardDebuffIntent());

        var withShield = new MoveState("WITH_SHIELD", WithShieldMove,
            new BuffIntent(), new DefendIntent());

        var punishErosion = new MoveState("PUNISH_EROSION", PunishErosionMove,
            new SingleAttackIntent(ErosionDamage), new CardDebuffIntent());

        var increaseJustice = new MoveState("INCREASE_JUSTICE", IncreaseJusticeMove,
            new BuffIntent(), new DebuffIntent(), new CardDebuffIntent());

        erosionAttack.FollowUpState = withShield;
        withShield.FollowUpState = punishErosion;
        punishErosion.FollowUpState = increaseJustice;
        increaseJustice.FollowUpState = erosionAttack;

        var phase2Attack = new MoveState("PHASE2_ATTACK", Phase2AttackMove,
            new SingleAttackIntent(Phase2AttackDamage), new BuffIntent());

        var taskMove = new MoveState("TASK_MOVE", TaskMove,
            new DebuffIntent());

        var addCards = new MoveState("ADD_CARDS", AddCardsMove,
            new CardDebuffIntent());

        phase2Attack.FollowUpState = taskMove;
        taskMove.FollowUpState = addCards;
        addCards.FollowUpState = phase2Attack;

        var states = new MonsterState[]
        {
            erosionAttack, withShield, punishErosion, increaseJustice,
            phase2Attack, taskMove, addCards
        };

        return new MonsterMoveStateMachine(states, erosionAttack);
    }

    public override async Task AfterAddedToRoom()
    {
        _phase2DeathTextCount = 0;

        await PowerCmd.Apply<UncontrolledJusticePower>(
            new ThrowingPlayerChoiceContext(), Creature, JusticeAmount, Creature, null);
    }

    // ========== 阶段1 ==========

    private async Task ErosionAttackMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(AttackDamage)
            .FromMonster(this)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);

        foreach (var player in CombatState.Players)
        {
            var hand = PileType.Hand.GetPile(player).Cards
                .Where(c => c.Affliction == null)
                .ToList();
            if (hand.Count > 0)
            {
                var card = hand[Rng.NextInt(hand.Count)];
                await CardCmd.AfflictAndPreview<ErosionAffliction>([card], 1, CardPreviewStyle.None);
            }
        }
    }

    private async Task WithShieldMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.5f);
        await PowerCmd.Apply<WithPower>(
            new ThrowingPlayerChoiceContext(), Creature, WithAmount, Creature, null);
        await CreatureCmd.GainBlock(Creature, ShieldAmount, ValueProp.Move, null);
    }

    private async Task PunishErosionMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(ErosionDamage)
            .FromMonster(this)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);

        foreach (var player in CombatState.Players)
        {
            var erodedCards = PileType.Hand.GetPile(player).Cards
                .Where(c => c.Affliction is ErosionAffliction)
                .ToList();
            if (erodedCards.Count == 0) continue;

            await DamageCmd.Attack(ErosionDamage)
                .FromMonster(this)
                .Targeting(player.Creature)
                .WithHitFx("vfx/vfx_attack_blunt")
                .Execute(null);

            var recorder = player.Creature.GetPower<HerehiroPower>();
            if (recorder == null)
            {
                await PowerCmd.Apply<HerehiroPower>(
                    new ThrowingPlayerChoiceContext(), player.Creature, 0, Creature, null);
                recorder = player.Creature.GetPower<HerehiroPower>();
            }

            foreach (var card in erodedCards)
            {
                CardCmd.ClearAffliction(card);
                recorder!.RememberedCards.Add(card);
                await CardPileCmd.RemoveFromCombat(card);
            }
        }
    }

    private async Task IncreaseJusticeMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.5f);

        var justice = Creature.GetPower<UncontrolledJusticePower>();
        if (justice != null && justice.Amount < MaxJustice)
        {
            await PowerCmd.Apply<UncontrolledJusticePower>(
                new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
        }

        await PowerCmd.Apply<WithPower>(
            new ThrowingPlayerChoiceContext(), Creature, WithAmount, Creature, null);

        foreach (var player in CombatState.Players)
        {
            await PowerCmd.Apply<WithPower>(
                new ThrowingPlayerChoiceContext(), player.Creature, WithAmount / 2, Creature, null);
        }

        int playerCount = CombatState.Players.Count();
        var topPlayer = CombatState.Players
            .OrderByDescending(p => p.Creature.GetPower<WithPower>()?.Amount ?? 0)
            .First();

        var hand = PileType.Hand.GetPile(topPlayer).Cards
            .Where(c => c.Affliction == null)
            .ToList();
        var toErode = hand.OrderBy(_ => Rng.NextFloat()).Take(playerCount).ToList();
        foreach (var card in toErode)
            await CardCmd.AfflictAndPreview<ErosionAffliction>([card], 1, CardPreviewStyle.None);
    }

    // ========== 阶段2 ==========

    private async Task Phase2AttackMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(), Creature,
            Phase2SelfDamage, ValueProp.Unpowered | ValueProp.Unblockable, null, null);

        await DamageCmd.Attack(Phase2AttackDamage)
            .FromMonster(this)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);

        var intelPower = Creature.GetPower<ThirteenWaterIntelPower>();
        if (intelPower != null)
        {
            int failedCount = intelPower.LastTaskFailedCount;
            for (int i = 0; i < failedCount; i++)
            {
                var players = CombatState.Players.Where(p => p.Creature.IsAlive).ToList();
                if (players.Count > 0)
                {
                    var target = players[Rng.NextInt(players.Count)];
                    await CreatureCmd.Damage(
                        new ThrowingPlayerChoiceContext(), target.Creature,
                        Phase2AttackDamage / 2, ValueProp.Move, Creature, null);
                }
            }
        }
    }

    private async Task TaskMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.5f);

        foreach (var player in CombatState.Players.Where(p => p.Creature.IsAlive))
        {
            await PowerCmd.Apply<ThirteenWaterTaskPower>(
                new ThrowingPlayerChoiceContext(), player.Creature, 0, Creature, null);
        }
    }

    private async Task AddCardsMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.5f);

        foreach (var player in CombatState.Players)
        {
            var card = CombatState.CreateCard<WitchMark>(player);
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
        }
    }

    public override Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented,
        float deathAnimLength)
    {
        if (creature == Creature && !wasRemovalPrevented)
        {
            _phase2DeathTextCount++;
            GuardThreeWrongTextVfx.Spawn(Creature, _phase2DeathTextCount + 1);
        }

        return Task.CompletedTask;
    }
}
