using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;
using ManosabaLin.Characters.Ema.Afflictions;
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
    private int WithAmount4 => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 20, 10);
    /// <summary>该 BOSS 每次攻击自带的护盾量；诈尸护盾也以它为下限，避免诈尸时无盾可破。</summary>
    public int ShieldAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 50, 40);

    /// <summary>补课也失败时置位，放行后续死亡：避免与引擎防递归（recursion 上限 10）打架，也避免战斗卡死。</summary>
    private bool _releasedDeath;

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
        var checkMoves = new MoveState("CHECK_MOVES", CheckMovesMove, new DebuffIntent());

        var blackHand = new MoveState("BLACK_HAND_MOVE", BlackHandMove, new CardDebuffIntent());

        var attack3 = new MoveState("ATTACK_3_MOVE", Attack3Move, new DebuffIntent(),
            new SingleAttackIntent(Turn3Damage), new BuffIntent(), new DefendIntent());

        var attack3Extra = new MoveState("ATTACK_3_EXTRA_MOVE", Attack3ExtraMove, new DebuffIntent(),
            new SingleAttackIntent(Turn3Damage), new BuffIntent(), new DefendIntent());

        var attack4 = new MoveState("ATTACK_4_MOVE", Attack4Move, new MultiAttackIntent(Turn3Damage, 2),
            new BuffIntent(), new DefendIntent());

        var condititionalBranch = new ConditionalBranchState("MANY_BLACK_HANDS");

        condititionalBranch.AddState(attack3Extra,
            () => CombatState.GetOpponentsOf(Creature).Where(c => c.IsPlayer && c.IsAlive).Sum(p =>
                PileType.Hand.GetPile(p.Player!).Cards.Count(c => c.Affliction is BlackHandAffliction) - 3) >= 0);
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
        _releasedDeath = false;
        var power = await PowerCmd.Apply<GuardTwoBossLastStandPower>(
            new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
        if (power == null)
            Log.Error("[GuardTwoBoss] GuardTwoBossLastStandPower failed to apply at combat start; " +
                      "the last-stand safety net (ShouldDie/AfterPreventingDeath) will try to recover it.");
    }

    // ========== 诈尸双保险 ==========
    // 曾观察到：双人局里本 BOSS 被普通伤害直接击杀，既没有触发诈尸也没有任何日志（大闹一场后正常发奖励）。
    // 正常路径下 GuardTwoBossLastStandPower（powers 先于 monster 被询问）会拦下死亡并执行诈尸；
    // 若它因任何原因不在场，ShouldDie 根本不会被它问到 —— 于是在这里垫一层：
    // 能力缺失时先拦下死亡，交给 AfterPreventingDeath 现场补课并补跑诈尸；补课也失败才放行死亡。
    public override bool ShouldDie(Creature creature)
    {
        if (creature != Creature) return true;
        if (_releasedDeath) return true;
        if (Creature.GetPower<GuardTwoBossLastStandPower>() != null)
            return true; // 正常局面：轮不到这里（能力自己会拦），保持原行为。
        Log.Error("[GuardTwoBoss] Last-stand power is MISSING at death time; intercepting death to recover.");
        return false;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature != Creature) return;

        var power = Creature.GetPower<GuardTwoBossLastStandPower>();
        if (power == null)
        {
            power = await PowerCmd.Apply<GuardTwoBossLastStandPower>(
                new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
            if (power != null)
                Log.Error("[GuardTwoBoss] Last-stand power recovered during death prevention; resuming the last-stand sequence.");
        }

        if (power == null)
        {
            // 补课失败：放行死亡（本次不再拦截，等引擎按正常流程处理），防止战斗无法结束。
            _releasedDeath = true;
            Log.Error("[GuardTwoBoss] Recovery failed; releasing the death so the combat cannot soft-lock.");
            return;
        }

        await power.AfterPreventingDeath(creature);
    }

    public async Task CheckMovesMove(IReadOnlyList<Creature> targets)
    {
        UpdateVisual("phase1");
        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.5f);

        foreach (var player in CombatState.Players)
        {
            if (player.Creature is { IsAlive: true } creature)
            {
                await PowerCmd.Apply<CheckMovesPhasePower>(
                    new ThrowingPlayerChoiceContext(), creature, 1, Creature, null);
            }
        }
    }

    private async Task BlackHandMove(IReadOnlyList<Creature> targets)
    {
        UpdateVisual("phase2");
        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.5f);
        foreach (var player in CombatState.Players)
        {
            var cards = player.PlayerCombatState!.AllCards
                .Where(c => c.Affliction is not BlackHandAffliction && c.Pile!.Type != PileType.Exhaust)
                .ToList();
            var halfCount = cards.Count / 2;
            var targetCards = cards.StableShuffle(Rng).Take(halfCount);

            foreach (var targetCard in targetCards)
                await CardCmd.AfflictAndPreview<BlackHandAffliction>(
                    [targetCard], 1, CardPreviewStyle.None);
        }
    }

    public async Task Attack3Move(IReadOnlyList<Creature> targets)
    {
        UpdateVisual("phase3");

        foreach (var player in CombatState.Players)
        {
            if (player.Creature is { IsAlive: true })
            {
                await PowerCmd.Apply<JudgmentHammerPhasePower>(
                    new ThrowingPlayerChoiceContext(), player.Creature, 1, Creature, null);
            }
        }

        await DamageCmd.Attack(Turn3Damage)
            .FromMonster(this)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);

        await PowerCmd.Apply<WithPower>(
            new ThrowingPlayerChoiceContext(), Creature, WithAmount, Creature, null);

        await CreatureCmd.GainBlock(Creature, ShieldAmount, ValueProp.Move, null);
    }

    public async Task Attack3ExtraMove(IReadOnlyList<Creature> targets)
    {
        await Attack3Move(targets);

        foreach (var card in CombatState.Players
                     .SelectMany(p => p.PlayerCombatState!.AllCards.Where(c => c.Pile!.Type != PileType.Exhaust)))
            CardCmd.ClearAffliction(card);
    }

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
            new ThrowingPlayerChoiceContext(), Creature, WithAmount4, Creature, null);

        await CreatureCmd.GainBlock(Creature, ShieldAmount, ValueProp.Move, null);
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

    private bool _isPhaseTwoMusicPlayed = false;

    public void PlayLastStandMusic()
    {
        if (_isPhaseTwoMusicPlayed) return;

        NRunMusicController.Instance?.PlayCustomMusic("event:/ManosabaLin/music/GuardTwotwo");
        _isPhaseTwoMusicPlayed = true;
    }

    public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
    {
        if (creature != Creature) return;

        PlayLastStandMusic();

        // 「死亡被阻止」这一趟就是诈尸，绝不能在这里发奖励：
        // RewardsCmd.OfferCustom 会发起一次玩家选择（RewardsSet.Offer → CardReward.OnSelect），
        // 而它此刻正嵌在伤害管线里，会把同一次伤害中紧随其后的
        // Hook.AfterPreventingDeath（SetMaxAndCurrentHp 无限血 + GainBlock 护盾）整段打断 ——
        // 实测表现为「诈尸音效响起，但没复活、没护盾、原地停在 0 血」。
        // 战胜奖励留到 wasRemovalPrevented == false 的那次真死再发。
        if (wasRemovalPrevented) return;

        var linCardPool = ModelDb.CardPool<LinCardPool>();
        if (linCardPool == null) return;

        foreach (var player in CombatState.Players)
        {
            var options = CardCreationOptions.ForNonCombatWithUniformOdds(
                new[] { linCardPool },
                c => c.Id.Entry == "MANOSABA_LIN_CARD_TRANSFORMATION_MAGIC");
            var rewards = new List<Reward>
            {
                new CardReward(options, 1, player, null)
            };
            await RewardsCmd.OfferCustom(player, rewards);
        }
    }
}
