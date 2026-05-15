// using System.Collections.Generic;
// using System.Threading.Tasks;
// using ManosabaLin.Characters.Hiro.Cards;
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
// public sealed class GuardTwoMonster : ModMonsterTemplate
// {
//     public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 220, 200);
//     public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 220, 200);
//
//     private int SlashDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 10);
//     private int HeavyDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 18, 15);
//     private int PoisonAmount => 8;
//     private int FrailAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);
//     private int VulnerableAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);
//
//     public override MonsterAssetProfile AssetProfile => new(
//         VisualsScenePath: "res://ManosabaLin/scenes/monsters/guard_two.tscn"
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
//         var slash = new MoveState("SLASH_MOVE", SlashMove,
//             new SingleAttackIntent(SlashDamage));
//
//         var shieldBash = new MoveState("SHIELD_BASH_MOVE", ShieldBashMove,
//             new AbstractIntent[] { new SingleAttackIntent(SlashDamage), new DefendIntent() });
//
//         var enfeeble = new MoveState("ENFEEBLE_MOVE", EnfeebleMove,
//             new AbstractIntent[] { new DebuffIntent(), new DebuffIntent() });
//
//         var heavyStrike = new MoveState("HEAVY_STRIKE_MOVE", HeavyStrikeMove,
//             new SingleAttackIntent(HeavyDamage));
//
//         // === Phase 2 (HP <= 50%) ===
//         var berserkSlash = new MoveState("BERSERK_SLASH_MOVE", BerserkSlashMove,
//             new AbstractIntent[] { new MultiAttackIntent(SlashDamage, 3) });
//
//         var toxicCloud = new MoveState("TOXIC_CLOUD_MOVE", ToxicCloudMove,
//             new AbstractIntent[] { new DebuffIntent(), new DebuffIntent() });
//
//         var rageStrike = new MoveState("RAGE_STRIKE_MOVE", RageStrikeMove,
//             new AbstractIntent[] { new SingleAttackIntent(HeavyDamage), new BuffIntent() });
//
//         // Phase 1 loop
//         slash.FollowUpState = shieldBash;
//         shieldBash.FollowUpState = enfeeble;
//         enfeeble.FollowUpState = heavyStrike;
//         heavyStrike.FollowUpState = slash;
//
//         // Phase 2 loop
//         berserkSlash.FollowUpState = toxicCloud;
//         toxicCloud.FollowUpState = rageStrike;
//         rageStrike.FollowUpState = berserkSlash;
//
//         var states = new MonsterState[]
//         {
//             slash, shieldBash, enfeeble, heavyStrike,
//             berserkSlash, toxicCloud, rageStrike
//         };
//
//         return new MonsterMoveStateMachine(states, slash);
//     }
//
//     public override async Task AfterAddedToRoom()
//     {
//         await Task.CompletedTask;
//         await PowerCmd.Apply<GuardTwoPhasePower>(
//             new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
//     }
//
//     private async Task SlashMove(IReadOnlyList<Creature> targets)
//     {
//         await DamageCmd.Attack(SlashDamage)
//             .FromMonster(this)
//             .WithAttackerFx(null, AttackSfx)
//             .WithHitFx("vfx/vfx_attack_slash")
//             .Execute(null);
//     }
//
//     private async Task ShieldBashMove(IReadOnlyList<Creature> targets)
//     {
//         await DamageCmd.Attack(SlashDamage)
//             .FromMonster(this)
//             .WithAttackerFx(null, AttackSfx)
//             .WithHitFx("vfx/vfx_attack_blunt")
//             .Execute(null);
//         await CreatureCmd.GainBlock(Creature, 10m, ValueProp.Move, null);
//     }
//
//     private async Task EnfeebleMove(IReadOnlyList<Creature> targets)
//     {
//         await CreatureCmd.TriggerAnim(Creature, "Cast", 0.5f);
//         foreach (var target in targets)
//         {
//             await PowerCmd.Apply<FrailPower>(
//                 new ThrowingPlayerChoiceContext(), target, FrailAmount, Creature, null);
//             await PowerCmd.Apply<VulnerablePower>(
//                 new ThrowingPlayerChoiceContext(), target, VulnerableAmount, Creature, null);
//         }
//     }
//
//     private async Task HeavyStrikeMove(IReadOnlyList<Creature> targets)
//     {
//         await DamageCmd.Attack(HeavyDamage)
//             .FromMonster(this)
//             .WithAttackerFx(null, AttackSfx)
//             .WithHitFx("vfx/vfx_attack_blunt")
//             .Execute(null);
//     }
//
//     private async Task BerserkSlashMove(IReadOnlyList<Creature> targets)
//     {
//         await DamageCmd.Attack(SlashDamage)
//             .FromMonster(this)
//             .WithAttackerFx(null, AttackSfx)
//             .WithHitFx("vfx/vfx_attack_slash")
//             .WithHitCount(3)
//             .Execute(null);
//     }
//
//     private async Task ToxicCloudMove(IReadOnlyList<Creature> targets)
//     {
//         await CreatureCmd.TriggerAnim(Creature, "Cast", 0.5f);
//         foreach (var target in targets)
//         {
//             await PowerCmd.Apply<PoisonPower>(
//                 new ThrowingPlayerChoiceContext(), target, PoisonAmount, Creature, null);
//             await PowerCmd.Apply<FrailPower>(
//                 new ThrowingPlayerChoiceContext(), target, FrailAmount, Creature, null);
//         }
//     }
//
//     private async Task RageStrikeMove(IReadOnlyList<Creature> targets)
//     {
//         await DamageCmd.Attack(HeavyDamage)
//             .FromMonster(this)
//             .WithAttackerFx(null, AttackSfx)
//             .WithHitFx("vfx/vfx_attack_blunt")
//             .Execute(null);
//         await PowerCmd.Apply<StrengthPower>(
//             new ThrowingPlayerChoiceContext(), Creature, 2, Creature, null);
//     }
// }
