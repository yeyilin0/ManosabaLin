// using System.Collections.Generic;
// using System.Threading.Tasks;
// using ManosabaLin.Characters.Hiro.Powers;
// using MegaCrit.Sts2.Core.Commands;
// using MegaCrit.Sts2.Core.Entities.Ascension;
// using MegaCrit.Sts2.Core.Entities.Creatures;
// using MegaCrit.Sts2.Core.Entities.Powers;
// using MegaCrit.Sts2.Core.GameActions.Multiplayer;
// using MegaCrit.Sts2.Core.Helpers;
// using MegaCrit.Sts2.Core.Models.Powers;
// using MegaCrit.Sts2.Core.MonsterMoves.Intents;
// using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
// using MegaCrit.Sts2.Core.Nodes.Combat;
// using MegaCrit.Sts2.Core.ValueProps;
// using STS2RitsuLib.Interop.AutoRegistration;
// using STS2RitsuLib.Scaffolding.Content;
// using STS2RitsuLib.Scaffolding.Godot;
//
// namespace ManosabaLin.Characters.Hiro.Monsters;
//
// [RegisterMonster]
// public sealed class MalicesMonster : ModMonsterTemplate
// {
//     public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 280, 260);
//     public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 280, 260);
//
//     private int ClawDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 13, 11);
//     private int BiteDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 16, 14);
//     private int MaelstromDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 10, 8);
//     private int PoisonAmount => 6;
//     private int WeakAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);
//     private int FrailAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);
//
//     public override MonsterAssetProfile AssetProfile => new(
//         VisualsScenePath: "res://ManosabaLin/scenes/monsters/malices.tscn"
//     );
//
//     protected override NCreatureVisuals? TryCreateCreatureVisuals()
//     {
//         return RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(
//             AssetProfile.VisualsScenePath!);
//     }
//
//     protected override MonsterMoveStateMachine GenerateMoveStateMachine()
//     {
//         // === Phase 1 (HP > 50%) ===
//         var claw = new MoveState("CLAW_MOVE", ClawMove,
//             new SingleAttackIntent(ClawDamage));
//
//         var corrupt = new MoveState("CORRUPT_MOVE", CorruptMove,
//             new AbstractIntent[] { new DebuffIntent(), new DebuffIntent() });
//
//         var bite = new MoveState("BITE_MOVE", BiteMove,
//             new AbstractIntent[] { new SingleAttackIntent(BiteDamage), new DebuffIntent() });
//
//         var shadowVeil = new MoveState("SHADOW_VEIL_MOVE", ShadowVeilMove,
//             new AbstractIntent[] { new DefendIntent(), new BuffIntent() });
//
//         // === Phase 2 (HP <= 50%) ===
//         var maelstrom = new MoveState("MAELSTROM_MOVE", MaelstromMove,
//             new AbstractIntent[] { new MultiAttackIntent(MaelstromDamage, 4) });
//
//         var corruptAll = new MoveState("CORRUPT_ALL_MOVE", CorruptAllMove,
//             new AbstractIntent[] { new DebuffIntent(), new DebuffIntent(), new DebuffIntent() });
//
//         var devour = new MoveState("DEVOUR_MOVE", DevourMove,
//             new AbstractIntent[] { new SingleAttackIntent(BiteDamage), new BuffIntent() });
//
//         // Phase 1 loop
//         claw.FollowUpState = corrupt;
//         corrupt.FollowUpState = bite;
//         bite.FollowUpState = shadowVeil;
//         shadowVeil.FollowUpState = claw;
//
//         // Phase 2 loop
//         maelstrom.FollowUpState = corruptAll;
//         corruptAll.FollowUpState = devour;
//         devour.FollowUpState = maelstrom;
//
//         var states = new MonsterState[]
//         {
//             claw, corrupt, bite, shadowVeil,
//             maelstrom, corruptAll, devour
//         };
//
//         return new MonsterMoveStateMachine(states, claw);
//     }
//
//     public override async Task AfterAddedToRoom()
//     {
//         await Task.CompletedTask;
//         await PowerCmd.Apply<MalicesPhasePower>(
//             new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
//     }
//
//     private async Task ClawMove(IReadOnlyList<Creature> targets)
//     {
//         await DamageCmd.Attack(ClawDamage)
//             .FromMonster(this)
//             .WithAttackerFx(null, AttackSfx)
//             .WithHitFx("vfx/vfx_attack_slash")
//             .Execute(null);
//     }
//
//     private async Task CorruptMove(IReadOnlyList<Creature> targets)
//     {
//         await CreatureCmd.TriggerAnim(Creature, "Cast", 0.5f);
//         foreach (var target in targets)
//         {
//             await PowerCmd.Apply<WeakPower>(
//                 new ThrowingPlayerChoiceContext(), target, WeakAmount, Creature, null);
//             await PowerCmd.Apply<FrailPower>(
//                 new ThrowingPlayerChoiceContext(), target, FrailAmount, Creature, null);
//         }
//     }
//
//     private async Task BiteMove(IReadOnlyList<Creature> targets)
//     {
//         await DamageCmd.Attack(BiteDamage)
//             .FromMonster(this)
//             .WithAttackerFx(null, AttackSfx)
//             .WithHitFx("vfx/vfx_attack_blunt")
//             .Execute(null);
//         foreach (var target in targets)
//         {
//             await PowerCmd.Apply<PoisonPower>(
//                 new ThrowingPlayerChoiceContext(), target, PoisonAmount, Creature, null);
//         }
//     }
//
//     private async Task ShadowVeilMove(IReadOnlyList<Creature> targets)
//     {
//         await CreatureCmd.TriggerAnim(Creature, "Cast", 0.5f);
//         await CreatureCmd.GainBlock(Creature, 12m, ValueProp.Move, null);
//         await PowerCmd.Apply<StrengthPower>(
//             new ThrowingPlayerChoiceContext(), Creature, 2, Creature, null);
//     }
//
//     private async Task MaelstromMove(IReadOnlyList<Creature> targets)
//     {
//         await CreatureCmd.TriggerAnim(Creature, "Cast", 0.5f);
//         await DamageCmd.Attack(MaelstromDamage)
//             .FromMonster(this)
//             .WithAttackerFx(null, AttackSfx)
//             .WithHitFx("vfx/vfx_attack_slash")
//             .WithHitCount(4)
//             .Execute(null);
//     }
//
//     private async Task CorruptAllMove(IReadOnlyList<Creature> targets)
//     {
//         await CreatureCmd.TriggerAnim(Creature, "Cast", 0.5f);
//         foreach (var target in targets)
//         {
//             await PowerCmd.Apply<WeakPower>(
//                 new ThrowingPlayerChoiceContext(), target, WeakAmount, Creature, null);
//             await PowerCmd.Apply<FrailPower>(
//                 new ThrowingPlayerChoiceContext(), target, FrailAmount, Creature, null);
//             await PowerCmd.Apply<PoisonPower>(
//                 new ThrowingPlayerChoiceContext(), target, PoisonAmount, Creature, null);
//         }
//     }
//
//     private async Task DevourMove(IReadOnlyList<Creature> targets)
//     {
//         await DamageCmd.Attack(BiteDamage)
//             .FromMonster(this)
//             .WithAttackerFx(null, AttackSfx)
//             .WithHitFx("vfx/vfx_attack_blunt")
//             .Execute(null);
//         await PowerCmd.Apply<StrengthPower>(
//             new ThrowingPlayerChoiceContext(), Creature, 3, Creature, null);
//     }
// }
