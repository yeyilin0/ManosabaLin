using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using ManosabaLin.Characters.Emalin.Components;
using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Component.Interfaces;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;

namespace ManosabaLin.Characters.Hiro.Monsters;

[RegisterMonster]
public sealed class GuardTwoBossMonster : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 380, 350);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 380, 350);

    private int Turn3Damage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 18, 15);
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

    private Dictionary<Player, HashSet<CardType>> _cardPlaysThisTurn = new();
    private bool _skipBlackHandNextTurn;

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var checkMoves = new MoveState("CHECK_MOVES", CheckMovesMove,
            new DebuffIntent());

        var blackHand = new MoveState("BLACK_HAND_MOVE", BlackHandMove,
            new AbstractIntent[] { new DebuffIntent(), new CardDebuffIntent() });

        var attack3 = new MoveState("ATTACK_3_MOVE", Attack3Move,
            new AbstractIntent[] { new SingleAttackIntent(Turn3Damage), new BuffIntent(), new DefendIntent() });

        var attack4 = new MoveState("ATTACK_4_MOVE", Attack4Move,
            new AbstractIntent[] { new MultiAttackIntent(Turn3Damage, 2), new BuffIntent(), new DefendIntent() });

        checkMoves.FollowUpState = blackHand;
        blackHand.FollowUpState = attack3;
        attack3.FollowUpState = checkMoves;
        attack4.FollowUpState = checkMoves;

        var states = new MonsterState[] { checkMoves, blackHand, attack3, attack4 };

        return new MonsterMoveStateMachine(states, checkMoves);
    }

    public override async Task AfterAddedToRoom()
    {
        UpdateVisual("monsters/guard_two_boss_phase1.png");
        await Task.CompletedTask;
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

        UpdateVisual("monsters/guard_two_boss_rage.png");

        var withAmount = Creature.GetPowerAmount<WithPower>();
        var shieldAmount = withAmount * 3;
        if (shieldAmount > 0)
            await CreatureCmd.GainBlock(Creature, shieldAmount, ValueProp.Move, null);

        _skipBlackHandNextTurn = true;
        SetMoveImmediate(
            new MoveState("ATTACK_4_MOVE", Attack4Move,
                new AbstractIntent[] { new MultiAttackIntent(Turn3Damage, 2), new BuffIntent(), new DefendIntent() }),
            true);
    }

    private async Task CheckMovesMove(IReadOnlyList<Creature> targets)
    {
        UpdateVisual("monsters/guard_two_boss_phase1.png");

        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.5f);

        var failedCount = 0;
        foreach (var player in CombatState.Players)
        {
            var played = _cardPlaysThisTurn.GetValueOrDefault(player, new HashSet<CardType>());
            var hasAll = played.Contains(CardType.Attack)
                      && played.Contains(CardType.Skill)
                      && played.Contains(CardType.Power);

            if (!hasAll && player.Creature is { IsAlive: true })
            {
                failedCount++;
                await DamageCmd.Attack(FailDamage)
                    .FromMonster(this)
                    .WithAttackerFx(null, AttackSfx)
                    .WithHitFx("vfx/vfx_attack_blunt")
                    .Targeting(player.Creature)
                    .Execute(null);
            }
        }

        if (failedCount > 0)
        {
            await PowerCmd.Apply<WithPower>(
                new ThrowingPlayerChoiceContext(), Creature, WithAmount * failedCount, Creature, null);
        }

        _cardPlaysThisTurn.Clear();
    }

    private async Task BlackHandMove(IReadOnlyList<Creature> targets)
    {
        UpdateVisual("monsters/guard_two_boss_phase2.png");

        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.5f);

        var players = CombatState.Players.ToList();

        foreach (var player in players)
        {
            var allCards = PileType.Draw.GetPile(player).Cards
                .Concat(PileType.Hand.GetPile(player).Cards)
                .Concat(PileType.Discard.GetPile(player).Cards)
                .Where(c => !c.HasComponent<BlackHandComponent>())
                .Distinct()
                .ToList();

            var halfCount = Math.Max(1, allCards.Count / 2);
            var cardsToMark = allCards.OrderBy(_ => Rng.NextDouble()).Take(halfCount);

            foreach (var card in cardsToMark)
                card.TryAddComponent(new BlackHandComponent());
        }

        var maxHandCount = 0;
        foreach (var player in players)
        {
            var handCount = PileType.Hand.GetPile(player).Cards.Count;
            if (handCount > maxHandCount)
                maxHandCount = handCount;
        }

        if (maxHandCount <= 0) return;

        foreach (var player in players)
        {
            var drawCards = PileType.Draw.GetPile(player).Cards
                .Where(c => !c.HasComponent<BlackHandComponent>())
                .ToList();

            var addCount = Math.Min(maxHandCount, drawCards.Count);
            var extraCards = drawCards.OrderBy(_ => Rng.NextDouble()).Take(addCount);

            foreach (var card in extraCards)
                card.TryAddComponent(new BlackHandComponent());
        }
    }

    private async Task Attack3Move(IReadOnlyList<Creature> targets)
    {
        UpdateVisual("monsters/guard_two_boss_phase3.png");

        await DamageCmd.Attack(Turn3Damage)
            .FromMonster(this)
            .WithAttackerFx(null, AttackSfx)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(null);

        await PowerCmd.Apply<WithPower>(
            new ThrowingPlayerChoiceContext(), Creature, WithAmount, Creature, null);

        await CreatureCmd.GainBlock(Creature, ShieldAmount, ValueProp.Move, null);

        if (!_skipBlackHandNextTurn)
        {
            await CheckBlackHandDamage();
        }
        _skipBlackHandNextTurn = false;
    }

    private async Task Attack4Move(IReadOnlyList<Creature> targets)
    {
        UpdateVisual("monsters/guard_two_boss_rage.png");

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

        _skipBlackHandNextTurn = false;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var player = cardPlay.Card.Owner;
        if (player == null) return;

        if (!_cardPlaysThisTurn.ContainsKey(player))
            _cardPlaysThisTurn[player] = new HashSet<CardType>();

        _cardPlaysThisTurn[player].Add(cardPlay.Card.Type);
    }

    private async Task CheckBlackHandDamage()
    {
        var players = CombatState.Players.ToList();
        var playerCount = players.Count;
        var playerCounts = new List<(Player player, int count)>();

        foreach (var player in players)
        {
            var count = PileType.Hand.GetPile(player).Cards
                .Count(c => c.HasComponent<BlackHandComponent>());
            playerCounts.Add((player, count));
        }

        if (playerCounts.Count == 0) return;

        var maxCount = playerCounts.Max(p => p.count);
        if (maxCount <= 0) return;

        foreach (var (player, count) in playerCounts.Where(p => p.count == maxCount))
        {
            if (player.Creature is { IsAlive: true })
            {
                await DamageCmd.Attack(count)
                    .FromMonster(this)
                    .WithAttackerFx(null, AttackSfx)
                    .WithHitFx("vfx/vfx_attack_blunt")
                    .Targeting(player.Creature)
                    .Execute(null);
            }
        }

        foreach (var player in players)
        {
            var allCards = PileType.Draw.GetPile(player).Cards
                .Concat(PileType.Hand.GetPile(player).Cards)
                .Concat(PileType.Discard.GetPile(player).Cards)
                .Where(c => c.HasComponent<BlackHandComponent>())
                .ToList();

            foreach (var card in allCards)
                (card as IComponentsCardModel)?.RemoveComponent<BlackHandComponent>();
        }

        if (maxCount > 3 * playerCount)
        {
            _skipBlackHandNextTurn = true;
            UpdateVisual("monsters/guard_two_boss_phase3.png");
            SetMoveImmediate(
                new MoveState("ATTACK_3_MOVE", Attack3Move,
                    new AbstractIntent[] { new SingleAttackIntent(Turn3Damage), new BuffIntent(), new DefendIntent() }),
                true);
        }
    }

    public void UpdateVisual(string path)
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Creature);
        if (creatureNode == null)
            return;
        ((Sprite2D)creatureNode.Visuals.GetCurrentBody()).Texture = PreloadManager.Cache.GetTexture2D(ImageHelper.GetImagePath(path));
        var scale = creatureNode.Visuals.GetCurrentBody().Scale;
        var tween = creatureNode.CreateTween();
        tween.TweenProperty(creatureNode.Visuals.GetCurrentBody(), (NodePath)"scale", scale, 1.2000000476837158).From(scale * 0.5f).SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Sine);
        tween.Parallel().TweenProperty(creatureNode.Visuals.GetCurrentBody(), (NodePath)"modulate", Colors.White, 0.5).From(Colors.Black);
    }
}