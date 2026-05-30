using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using STS2RitsuLib.Scaffolding.Godot;

namespace ManosabaLin.Characters.Hiro.Monsters;

[RegisterMonster]
public sealed class GuardTwoBossMonster : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 380, 350);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 380, 350);

    public int Turn3Damage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 18, 15);
    private int FailDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 10, 8);
    private int WithAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 40, 30);
    private int ShieldAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 20, 15);

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://ManosabaLin/scenes/monsters/guard_two_boss.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals()
    {
        return RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(
            AssetProfile.VisualsScenePath!);
    }

    public bool IsDoubleAttackMode { get; set; }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        // 意图1：审判之眼 - 给玩家CheckMovesPhasePower
        var checkMoves = new MoveState("CHECK_MOVES", CheckMovesMove, new DebuffIntent());

        // 意图2：黑手印记 - 给玩家BlackHandPhasePower
        var blackHand = new MoveState("BLACK_HAND_MOVE", BlackHandMove, new DebuffIntent(), new CardDebuffIntent());

        // 意图3：审判之锤 - 给玩家JudgmentHammerPhasePower + 攻击
        var attack3 = new MoveState("ATTACK_3_MOVE", Attack3Move, new SingleAttackIntent(Turn3Damage), new BuffIntent(),
            new DefendIntent());

        // 意图4：双倍审判 - 最高优先级，不被JudgmentHammerPhasePower修改
        var attack4 = new MoveState("ATTACK_4_MOVE", Attack4Move, new MultiAttackIntent(Turn3Damage, 2),
            new BuffIntent(), new DefendIntent());

        var condititionalBranch = new ConditionalBranchState("MANY_BLACK_HANDS");

        condititionalBranch.AddState(attack4,
            () => CombatState.GetOpponentsOf(Creature).Where(c => c.IsPlayer).Sum(p =>
                PileType.Hand.GetPile(p.Player!).Cards.Count(c => c.HasComponent<BlackHandComponent>()) - 2) >= 0);
        condititionalBranch.AddState(checkMoves, () => true);

        checkMoves.FollowUpState = blackHand;
        blackHand.FollowUpState = attack3;
        attack3.FollowUpState = condititionalBranch;
        attack4.FollowUpState = checkMoves;

        return new MonsterMoveStateMachine([checkMoves, blackHand, attack3, attack4, condititionalBranch], checkMoves);
    }

    public override async Task AfterAddedToRoom()
    {
        ManosabaAudio.TryPlayOneShot("guard_two_boss_theme.mp3".BgmAudioPath(), 0.8f);

        // 给boss自身GuardTwoBossPhasePower（10%HP触发）
        await PowerCmd.Apply<GuardTwoBossPhasePower>(
            new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);

        // 进入战斗即获得“免死一次”能力
        await PowerCmd.Apply<GuardTwoBossLastStandPower>(
            new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Creature) return;
        if (Creature.CurrentHp > Creature.MaxHp * 0.1m) return;

        IsDoubleAttackMode = true;
        UpdateVisual("rage");

        var withAmount = Creature.GetPowerAmount<WithPower>();
        var shieldAmount = withAmount * 3;
        if (shieldAmount > 0)
            await CreatureCmd.GainBlock(Creature, shieldAmount, ValueProp.Move, null);

        SetMoveImmediate((MoveState)MoveStateMachine!.States["ATTACK_4_MOVE"]);
    }

    // 意图1：审判之眼 - 给玩家CheckMovesPhasePower，效果在能力里
    public async Task CheckMovesMove(IReadOnlyList<Creature> targets)
    {
        UpdateVisual("phase1");
        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.5f);

        // 给所有玩家施加CheckMovesPhasePower
        foreach (var player in CombatState.Players)
        {
            if (player.Creature is { IsAlive: true })
            {
                await PowerCmd.Apply<CheckMovesPhasePower>(
                    new ThrowingPlayerChoiceContext(), player.Creature, 1, Creature, null);
            }
        }
    }

    // 意图2：黑手印记 - 给玩家BlackHandPhasePower，效果在能力里
    private async Task BlackHandMove(IReadOnlyList<Creature> targets)
    {
        UpdateVisual("phase2");
        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.5f);

        // 给所有玩家施加BlackHandPhasePower
        foreach (var player in CombatState.Players)
        {
            if (player.Creature is { IsAlive: true })
            {
                await PowerCmd.Apply<BlackHandPhasePower>(
                    new ThrowingPlayerChoiceContext(), player.Creature, 1, Creature, null);
            }
        }
    }

    // 意图3：审判之锤 - 给玩家JudgmentHammerPhasePower + 攻击
    public async Task Attack3Move(IReadOnlyList<Creature> targets)
    {
        IsDoubleAttackMode = false;
        UpdateVisual("phase3");

        // 给所有玩家施加JudgmentHammerPhasePower
        foreach (var player in CombatState.Players)
        {
            if (player.Creature is { IsAlive: true })
            {
                await PowerCmd.Apply<JudgmentHammerPhasePower>(
                    new ThrowingPlayerChoiceContext(), player.Creature, 1, Creature, null);
            }
        }

        // 攻击
        await DamageCmd.Attack(Turn3Damage)
            .FromMonster(this)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);

        await PowerCmd.Apply<WithPower>(
            new ThrowingPlayerChoiceContext(), Creature, WithAmount, Creature, null);

        await CreatureCmd.GainBlock(Creature, ShieldAmount, ValueProp.Move, null);
    }

    // 意图4：双倍审判 - 最高优先级
    public async Task Attack4Move(IReadOnlyList<Creature> targets)
    {
        UpdateVisual("rage");

        var healAmount = Creature.MaxHp * 0.5m - Creature.CurrentHp;
        if (healAmount > 0)
            await CreatureCmd.Heal(Creature, healAmount);

        await DamageCmd.Attack(Turn3Damage)
            .FromMonster(this)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_blunt")
            .WithHitCount(2)
            .Execute(null);

        await PowerCmd.Apply<WithPower>(
            new ThrowingPlayerChoiceContext(), Creature, WithAmount, Creature, null);

        await CreatureCmd.GainBlock(Creature, ShieldAmount, ValueProp.Move, null);

        IsDoubleAttackMode = false;
    }

    private string _lastVisual = "phase1";

    private void UpdateVisual(string name)
    {
        if (name == _lastVisual) return;
        _lastVisual = name;

        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Creature);
        if (creatureNode == null)
            return;

        var body = (Sprite2D)creatureNode.Visuals.GetCurrentBody();
        var tween = creatureNode.CreateTween();

        tween.TweenProperty(body, (NodePath)"modulate", Colors.Black, 0.2);

        tween.TweenCallback(Callable.From(() =>
        {
            var path = $"guard_two_boss_{name}.png".MonstersImagePath();
            body.Texture = PreloadManager.Cache.GetTexture2D(path);
        }));

        tween.TweenProperty(body, (NodePath)"modulate", Colors.White, 0.3);
    }
}
