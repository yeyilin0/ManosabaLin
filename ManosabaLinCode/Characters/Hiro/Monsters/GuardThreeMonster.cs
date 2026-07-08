// GuardThreeMonster.cs
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Godot;
using ManosabaLin.Characters.Ema.Afflictions;
using ManosabaLin.Characters.Hiro.Cards;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using Timer = Godot.Timer;

namespace ManosabaLin.Characters.Hiro.Monsters;

[RegisterMonster]
public sealed class GuardThreeMonster : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 380, 360);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 380, 360);

    private int AttackDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 22, 20);
    private int ErosionDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 14);
    private int WithAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 30, 20);
    private int ShieldAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 40, 30);
    private const int InitialJusticeAmount = 1;
    private const int MaxJustice = 5;
    private int Phase2SelfDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 60, 50);
    private int Phase2AttackDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 30, 25);
    private int Phase2ExtraDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 15);
    private int _phase2DeathTextCount;
    private bool _isPhaseTwo;
    private Timer? _phaseOneTextTimer;
    private string _lastVisual = "phase1";

    public IEnumerable<MoveState> CreatePhaseTwoStandMoves()
    {
        yield return new MoveState("PHASE2_ATTACK", Phase2AttackMove,
            new SingleAttackIntent(Phase2AttackDamage), new BuffIntent());
        yield return new MoveState("TASK_MOVE", TaskMove,
            new DebuffIntent());
        yield return new MoveState("ADD_CARDS", AddCardsMove,
            new CardDebuffIntent());
    }

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

        var phaseTwoMoves = CreatePhaseTwoStandMoves().ToArray();
        var phase2Attack = phaseTwoMoves[0];
        var taskMove = phaseTwoMoves[1];
        var addCards = phaseTwoMoves[2];

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
        _isPhaseTwo = false;
        StartPhaseOneTextTimer();

        await PowerCmd.Apply<UncontrolledJusticePower>(
            new ThrowingPlayerChoiceContext(), Creature, InitialJusticeAmount, Creature, null);
    }

    public async Task EnterPhaseTwo()
    {
        if (_isPhaseTwo) return;

        _isPhaseTwo = true;
        StopPhaseOneTextTimer();

        NRunMusicController.Instance?.PlayCustomMusic("event:/ManosabaLin/sfx/music/GuardThreePha2");

        await GuardThreePhaseTransitionOverlay.PlayAsync();
        GuardThreeWrongTextVfx.SpawnPersistentWrong(Creature, 4);
    }

    // ========== 阶段1 ==========

    private async Task ErosionAttackMove(IReadOnlyList<Creature> targets)
    {
        UpdateVisual("phase1");
        await DamageCmd.Attack(AttackDamage)
            .FromMonster(this)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);

        foreach (var player in CombatState.Players)
        {
            var drawPile = PileType.Draw.GetPile(player);
            if (drawPile.Cards.Count > 0)
            {
                var card = drawPile.Cards[0];
                await CardCmd.AfflictAndPreview<ErosionAffliction>([card], 1, CardPreviewStyle.None);
            }
        }
    }

    private async Task WithShieldMove(IReadOnlyList<Creature> targets)
    {
        UpdateVisual("phase1");
        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.5f);
        await PowerCmd.Apply<WithPower>(
            new ThrowingPlayerChoiceContext(), Creature, WithAmount, Creature, null);
        await CreatureCmd.GainBlock(Creature, ShieldAmount, ValueProp.Move, null);
    }

    private async Task PunishErosionMove(IReadOnlyList<Creature> targets)
    {
        UpdateVisual("phase1");
        await DamageCmd.Attack(ErosionDamage)
            .FromMonster(this)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);

        foreach (var player in CombatState.Players)
        {
            var erodedCards = new[] { PileType.Hand, PileType.Draw, PileType.Discard, PileType.Exhaust }
                .SelectMany(pile => pile.GetPile(player).Cards)
                .Where(c => c.Affliction is ErosionAffliction)
                .ToList();
            if (erodedCards.Count == 0) continue;

            var recorder = player.Creature.GetPower<HerehiroPower>();
            if (recorder == null)
            {
                recorder = await PowerCmd.Apply<HerehiroPower>(
                    new ThrowingPlayerChoiceContext(), player.Creature, erodedCards.Count, Creature, null);
            }
            else
            {
                await PowerCmd.Apply<HerehiroPower>(
                    new ThrowingPlayerChoiceContext(), player.Creature, erodedCards.Count, Creature, null);
            }

            foreach (var card in erodedCards)
            {
                await CreatureCmd.Damage(
                    new ThrowingPlayerChoiceContext(), player.Creature,
                    ErosionDamage, ValueProp.Move, null, null);

                recorder?.RememberedCards.Add(card);
                await CardPileCmd.RemoveFromCombat(card);
            }
        }
    }

    private async Task IncreaseJusticeMove(IReadOnlyList<Creature> targets)
    {
        UpdateVisual("phase1");
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

        var drawPile = PileType.Draw.GetPile(topPlayer);
        var toErode = drawPile.Cards.Take(playerCount).ToList();
        foreach (var card in toErode)
            await CardCmd.AfflictAndPreview<ErosionAffliction>([card], 1, CardPreviewStyle.None);
    }

    // ========== 阶段2 ==========

    private async Task Phase2AttackMove(IReadOnlyList<Creature> targets)
    {
        UpdateVisual("phase2");
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
                        Phase2ExtraDamage, ValueProp.Move, null, null);
                }
            }

            intelPower.LastTaskFailedCount = 0;
        }
    }

    private async Task TaskMove(IReadOnlyList<Creature> targets)
    {
        UpdateVisual("phase2");
        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.5f);

        foreach (var player in CombatState.Players.Where(p => p.Creature.IsAlive))
        {
            // 玩家失去1层 ThirteenWaterPlayerIntelPower
            var playerIntel = player.Creature.GetPower<ThirteenWaterPlayerIntelPower>();
            if (playerIntel != null && playerIntel.Amount > 0)
                await PowerCmd.Decrement(playerIntel);

            // 施加1层 ThirteenWaterTaskPower
            await PowerCmd.Apply<ThirteenWaterTaskPower>(
                new ThrowingPlayerChoiceContext(), player.Creature, 1, Creature, null);
        }
    }

    private async Task AddCardsMove(IReadOnlyList<Creature> targets)
    {
        UpdateVisual("phase2");
        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.5f);

        // 自身获得30 WithPower
        await PowerCmd.Apply<WithPower>(
            new ThrowingPlayerChoiceContext(), Creature, 30, Creature, null);

        foreach (var player in CombatState.Players)
        {
            // 生成2张 Hiroparanoid
            for (int i = 0; i < 2; i++)
            {
                var card = CombatState.CreateCard<Hiroparanoid>(player);
                await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
            }
        }
    }

    // ========== 视觉 ==========

    private void UpdateVisual(string name)
    {
        if (name == _lastVisual) return;
        _lastVisual = name;

        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Creature);
        if (creatureNode == null) return;

        var body = (Sprite2D)creatureNode.Visuals.GetCurrentBody();
        var tween = creatureNode.CreateTween();

        tween.TweenProperty(body, (NodePath)"modulate", Colors.Black, 0.2);

        tween.TweenCallback(Callable.From(() =>
        {
            var path = $"guard_three_{name}.png".MonstersImagePath();
            body.Texture = PreloadManager.Cache.GetTexture2D(path);
        }));

        tween.TweenProperty(body, (NodePath)"modulate", Colors.White, 0.3);
    }

    // ========== 死亡/复活 ==========

    public override bool ShouldDie(Creature creature)
    {
        if (creature != Creature) return true;
        return true;
    }

    public override Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented,
        float deathAnimLength)
    {
        if (creature == Creature && wasRemovalPrevented && _isPhaseTwo)
        {
            _phase2DeathTextCount++;
            GuardThreeWrongTextVfx.SpawnFloatingWrong(Creature, _phase2DeathTextCount + 1);
        }

        return Task.CompletedTask;
    }

    // ========== 阶段1文字定时器 ==========

    private void StartPhaseOneTextTimer()
    {
        StopPhaseOneTextTimer();

        var room = NCombatRoom.Instance;
        if (room == null) return;

        _phaseOneTextTimer = new Timer
        {
            WaitTime = 5f,
            OneShot = false,
            Autostart = true
        };
        room.AddChild(_phaseOneTextTimer);
        _phaseOneTextTimer.Timeout += SpawnPhaseOneTextLine;
    }

    private void StopPhaseOneTextTimer()
    {
        if (_phaseOneTextTimer == null) return;

        _phaseOneTextTimer.Timeout -= SpawnPhaseOneTextLine;
        _phaseOneTextTimer.QueueFree();
        _phaseOneTextTimer = null;
    }

    private void SpawnPhaseOneTextLine()
    {
        if (!_isPhaseTwo && Creature.IsAlive)
            GuardThreeWrongTextVfx.SpawnPhaseOneLine(Creature);
    }
}
