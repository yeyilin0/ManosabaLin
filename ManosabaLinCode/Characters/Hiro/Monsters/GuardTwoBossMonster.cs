using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Scaffolding.Godot;

namespace ManosabaLin.Characters.Hiro.Monsters;

[RegisterMonster]
public sealed class GuardTwoBossMonster : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 380, 350);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 380, 350);

    private int Turn3Damage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 18, 15);
    private int FailDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 10, 8);
    private int WithAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 60, 50);
    private int ShieldAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 50, 40);

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://ManosabaLin/scenes/monsters/guard_two_boss.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals()
    {
        return RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(
            AssetProfile.VisualsScenePath!);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        // 意图1：审判之眼 - 给玩家CheckMovesPhasePower
        var checkMoves = new MoveState("CHECK_MOVES", CheckMovesMove, new DebuffIntent());

        // 意图2：黑手印记 - 给玩家上黑手
        var blackHand = new MoveState("BLACK_HAND_MOVE", BlackHandMove, new CardDebuffIntent());

        // 意图3：审判之锤 - 给玩家JudgmentHammerPhasePower + 攻击
        var attack3 = new MoveState("ATTACK_3_MOVE", Attack3Move, new DebuffIntent(),
            new SingleAttackIntent(Turn3Damage), new BuffIntent(), new DefendIntent());

        // 额外意图：审判之锤
        var attack3Extra = new MoveState("ATTACK_3_EXTRA_MOVE", Attack3Move, new DebuffIntent(),
            new SingleAttackIntent(Turn3Damage), new BuffIntent(), new DefendIntent());

        // 意图4：双倍审判 - 最高优先级，不被JudgmentHammerPhasePower修改
        var attack4 = new MoveState("ATTACK_4_MOVE", Attack4Move, new MultiAttackIntent(Turn3Damage, 2),
            new BuffIntent(), new DefendIntent());

        var condititionalBranch = new ConditionalBranchState("MANY_BLACK_HANDS");

        condititionalBranch.AddState(attack3Extra,
            () => CombatState.GetOpponentsOf(Creature).Where(c => c.IsPlayer && c.IsAlive).Sum(p =>
                PileType.Hand.GetPile(p.Player!).Cards.Count(c => c.HasComponent<BlackHandComponent>()) - 3) >= 0);
        condititionalBranch.AddState(checkMoves, () => true);

        checkMoves.FollowUpState = blackHand;
        blackHand.FollowUpState = attack3;
        attack3.FollowUpState = condititionalBranch;
        attack3Extra.FollowUpState = checkMoves;
        attack4.FollowUpState = checkMoves;

        return new MonsterMoveStateMachine([checkMoves, blackHand, attack3, attack3Extra, attack4, condititionalBranch],
            checkMoves);
    }

    public override async Task AfterAddedToRoom()
    {
        ManosabaAudio.TryPlayOneShot("guard_two_boss_theme.mp3".BgmAudioPath(), 0.8f);
        // 进入战斗即获得 “免死亿次” 能力
        await PowerCmd.Apply<GuardTwoBossLastStandPower>(
            new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
    }

    // 意图1：审判之眼 - 给玩家CheckMovesPhasePower，效果在能力里
    public async Task CheckMovesMove(IReadOnlyList<Creature> targets)
    {
        UpdateVisual("phase1");
        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.5f);

        // 给所有玩家施加CheckMovesPhasePower
        foreach (var player in CombatState.Players)
        {
            if (player.Creature is { IsAlive: true } creature)
            {
                await PowerCmd.Apply<CheckMovesPhasePower>(
                    new ThrowingPlayerChoiceContext(), creature, 1, Creature, null);
            }
        }
    }

    // 意图2：黑手印记 - 给玩家卡牌施加黑手组件
    private async Task BlackHandMove(IReadOnlyList<Creature> targets)
    {
        UpdateVisual("phase2");
        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.5f);
        foreach (var player in CombatState.Players)
        {
            var cards = player.PlayerCombatState!.AllCards
                .Where(c => !c.HasComponent<BlackHandComponent>() && c.Pile!.Type != PileType.Exhaust)
                .ToList();
            var halfCount = cards.Count / 2;
            var targetCards = cards.StableShuffle(Rng).Take(halfCount);

            foreach (var targetCard in targetCards)
                targetCard.TryAddComponent(new BlackHandComponent());
        }
    }

    // 意图3：审判之锤 - 给玩家 JudgmentHammerPhasePower + 攻击
    public async Task Attack3Move(IReadOnlyList<Creature> targets)
    {
        UpdateVisual("phase3");

        // 给所有玩家施加 JudgmentHammerPhasePower
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

        await DamageCmd.Attack(Turn3Damage)
            .FromMonster(this)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_blunt")
            .WithHitCount(2)
            .Execute(null);

        await PowerCmd.Apply<WithPower>(
            new ThrowingPlayerChoiceContext(), Creature, WithAmount, Creature, null);

        await CreatureCmd.GainBlock(Creature, ShieldAmount, ValueProp.Move, null);

        // 清除所有黑手组件
        foreach (var card in CombatState.Players
                     .SelectMany(p => p.PlayerCombatState!.AllCards.Where(c => c.Pile!.Type != PileType.Exhaust)))
            card.TryRemoveComponent<BlackHandComponent>();
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

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (creature != Creature) return;

        var player = Creature.CombatState?.Players.FirstOrDefault();
        if (player == null) return;

        // 给予"变身的魔法"奖励卡
        var rewardCardId = new ModelId("CARD", "MANOSABA_LIN_CARD_TRANSFORMATION_MAGIC");
        var rewardCard = ModelDb.GetById<CardModel>(rewardCardId);
        if (rewardCard != null)
        {
            var options = new CardCreationOptions(
                new List<CardModel> { rewardCard },
                CardCreationSource.Other,
                CardRarityOddsType.Uniform
            );

            var rewards = new List<Reward>
            {
                new CardReward(options, 1, player, null)
            };
            await RewardsCmd.OfferCustom(player, rewards);
        }
    }
}
