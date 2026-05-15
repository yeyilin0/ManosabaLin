// using System.Collections.Generic;
// using System.Threading.Tasks;
// using ManosabaLin.Characters.Hiro.Cards;
// using ManosabaLin.Characters.Hiro.Powers;
// using MegaCrit.Sts2.Core.Commands;
// using MegaCrit.Sts2.Core.Entities.Ascension;
// using MegaCrit.Sts2.Core.Entities.Cards;
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
// public sealed class EmaAndHiroMonster : ModMonsterTemplate
// {
//     public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 300, 280);
//     public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 300, 280);
//
//     private int EmaDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 12, 10);
//     private int HiroDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 14, 12);
//     private int ComboDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 20, 16);
//     private int PoisonAmount => 5;
//     private int FrailAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);
//
//     public override MonsterAssetProfile AssetProfile => new(
//         VisualsScenePath: "res://ManosabaLin/scenes/monsters/ema_and_hiro.tscn"
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
//         // === Phase 1 (HP > 50%) - cooperative attacks ===
//         var emaStrike = new MoveState("EMA_STRIKE_MOVE", EmaStrikeMove,
//             new SingleAttackIntent(EmaDamage));
//
//         var hiroDefend = new MoveState("HIRO_DEFEND_MOVE", HiroDefendMove,
//             new AbstractIntent[] { new DefendIntent(), new BuffIntent() });
//
//         var emaPoison = new MoveState("EMA_POISON_MOVE", EmaPoisonMove,
//             new AbstractIntent[] { new DebuffIntent(), new SingleAttackIntent(EmaDamage) });
//
//         var hiroStrike = new MoveState("HIRO_STRIKE_MOVE", HiroStrikeMove,
//             new SingleAttackIntent(HiroDamage));
//
//         // === Phase 2 (HP <= 50%) - synchronized attacks ===
//         var dualAssault = new MoveState("DUAL_ASSAULT_MOVE", DualAssaultMove,
//             new AbstractIntent[] { new MultiAttackIntent(ComboDamage, 2) });
//
//         var witchBond = new MoveState("WITCH_BOND_MOVE", WitchBondMove,
//             new AbstractIntent[] { new BuffIntent(), new DefendIntent() });
//
//         var unitedStrike = new MoveState("UNITED_STRIKE_MOVE", UnitedStrikeMove,
//             new AbstractIntent[] { new SingleAttackIntent(ComboDamage), new DebuffIntent() });
//
//         // Phase 1 loop
//         emaStrike.FollowUpState = hiroDefend;
//         hiroDefend.FollowUpState = emaPoison;
//         emaPoison.FollowUpState = hiroStrike;
//         hiroStrike.FollowUpState = emaStrike;
//
//         // Phase 2 loop
//         dualAssault.FollowUpState = witchBond;
//         witchBond.FollowUpState = unitedStrike;
//         unitedStrike.FollowUpState = dualAssault;
//
//         var states = new MonsterState[]
//         {
//             emaStrike, hiroDefend, emaPoison, hiroStrike,
//             dualAssault, witchBond, unitedStrike
//         };
//
//         return new MonsterMoveStateMachine(states, emaStrike);
//     }
//
//     public override async Task AfterAddedToRoom()
//     {
//         await Task.CompletedTask;
//         await PowerCmd.Apply<EmaAndHiroPhasePower>(
//             new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
//     }
//
//     private async Task EmaStrikeMove(IReadOnlyList<Creature> targets)
//     {
//         await DamageCmd.Attack(EmaDamage)
//             .FromMonster(this)
//             .WithAttackerFx(null, AttackSfx)
//             .WithHitFx("vfx/vfx_attack_slash")
//             .Execute(null);
//     }
//
//     private async Task HiroDefendMove(IReadOnlyList<Creature> targets)
//     {
//         await CreatureCmd.TriggerAnim(Creature, "Cast", 0.5f);
//         await CreatureCmd.GainBlock(Creature, 12m, ValueProp.Move, null);
//         await PowerCmd.Apply<StrengthPower>(
//             new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
//     }
//
//     private async Task EmaPoisonMove(IReadOnlyList<Creature> targets)
//     {
//         await CreatureCmd.TriggerAnim(Creature, "Cast", 0.5f);
//         foreach (var target in targets)
//         {
//             await PowerCmd.Apply<PoisonPower>(
//                 new ThrowingPlayerChoiceContext(), target, PoisonAmount, Creature, null);
//         }
//         await DamageCmd.Attack(EmaDamage)
//             .FromMonster(this)
//             .WithAttackerFx(null, AttackSfx)
//             .WithHitFx("vfx/vfx_attack_slash")
//             .Execute(null);
//     }
//
//     private async Task HiroStrikeMove(IReadOnlyList<Creature> targets)
//     {
//         await DamageCmd.Attack(HiroDamage)
//             .FromMonster(this)
//             .WithAttackerFx(null, AttackSfx)
//             .WithHitFx("vfx/vfx_attack_blunt")
//             .Execute(null);
//     }
//
//     private async Task DualAssaultMove(IReadOnlyList<Creature> targets)
//     {
//         await DamageCmd.Attack(ComboDamage)
//             .FromMonster(this)
//             .WithAttackerFx(null, AttackSfx)
//             .WithHitFx("vfx/vfx_attack_slash")
//             .WithHitCount(2)
//             .Execute(null);
//     }
//
//     private async Task WitchBondMove(IReadOnlyList<Creature> targets)
//     {
//         await CreatureCmd.TriggerAnim(Creature, "Cast", 0.5f);
//         await PowerCmd.Apply<StrengthPower>(
//             new ThrowingPlayerChoiceContext(), Creature, 2, Creature, null);
//         await CreatureCmd.GainBlock(Creature, 15m, ValueProp.Move, null);
//     }
//
//     private async Task UnitedStrikeMove(IReadOnlyList<Creature> targets)
//     {
//         await DamageCmd.Attack(ComboDamage)
//             .FromMonster(this)
//             .WithAttackerFx(null, AttackSfx)
//             .WithHitFx("vfx/vfx_attack_blunt")
//             .Execute(null);
//         foreach (var target in targets)
//         {
//             await PowerCmd.Apply<FrailPower>(
//                 new ThrowingPlayerChoiceContext(), target, FrailAmount, Creature, null);
//         }
//     }
// }
