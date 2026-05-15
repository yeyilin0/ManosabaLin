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
// public sealed class TheGreatWitchYukiMonster : ModMonsterTemplate
// {
//     public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 350, 320);
//     public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 350, 320);
//
//     private int MagicDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 15, 12);
//     private int CurseDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 20, 16);
//     private int PoisonAmount => 8;
//     private int WeakAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);
//     private int FrailAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);
//
//     public override MonsterAssetProfile AssetProfile => new(
//         VisualsScenePath: "res://ManosabaLin/scenes/monsters/great_witch_yuki.tscn"
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
//         // === Phase 1 (HP > 50%) - witchcraft ===
//         var magicBolt = new MoveState("MAGIC_BOLT_MOVE", MagicBoltMove,
//             new SingleAttackIntent(MagicDamage));
//
//         var hex = new MoveState("HEX_MOVE", HexMove,
//             new AbstractIntent[] { new DebuffIntent(), new DebuffIntent() });
//
//         var darkShield = new MoveState("DARK_SHIELD_MOVE", DarkShieldMove,
//             new AbstractIntent[] { new DefendIntent(), new BuffIntent() });
//
//         var curse = new MoveState("CURSE_MOVE", CurseMove,
//             new AbstractIntent[] { new SingleAttackIntent(CurseDamage), new CardDebuffIntent() });
//
//         // === Phase 2 (HP <= 50%) - unleash power ===
//         var darkRitual = new MoveState("DARK_RITUAL_MOVE", DarkRitualMove,
//             new AbstractIntent[] { new BuffIntent(), new BuffIntent(), new BuffIntent() });
//
//         var voidBlast = new MoveState("VOID_BLAST_MOVE", VoidBlastMove,
//             new AbstractIntent[] { new MultiAttackIntent(MagicDamage, 3) });
//
//         var apocalypse = new MoveState("APOCALYPSE_MOVE", ApocalypseMove,
//             new AbstractIntent[] { new SingleAttackIntent(CurseDamage), new DebuffIntent(), new DebuffIntent() });
//
//         // Phase 1 loop
//         magicBolt.FollowUpState = hex;
//         hex.FollowUpState = darkShield;
//         darkShield.FollowUpState = curse;
//         curse.FollowUpState = magicBolt;
//
//         // Phase 2 loop
//         darkRitual.FollowUpState = voidBlast;
//         voidBlast.FollowUpState = apocalypse;
//         apocalypse.FollowUpState = darkRitual;
//
//         var states = new MonsterState[]
//         {
//             magicBolt, hex, darkShield, curse,
//             darkRitual, voidBlast, apocalypse
//         };
//
//         return new MonsterMoveStateMachine(states, magicBolt);
//     }
//
//     public override async Task AfterAddedToRoom()
//     {
//         await Task.CompletedTask;
//         await PowerCmd.Apply<GreatWitchYukiPhasePower>(
//             new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
//     }
//
//     private async Task MagicBoltMove(IReadOnlyList<Creature> targets)
//     {
//         await CreatureCmd.TriggerAnim(Creature, "Cast", 0.5f);
//         await DamageCmd.Attack(MagicDamage)
//             .FromMonster(this)
//             .WithAttackerFx(null, AttackSfx)
//             .WithHitFx("vfx/vfx_attack_slash")
//             .Execute(null);
//     }
//
//     private async Task HexMove(IReadOnlyList<Creature> targets)
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
//     private async Task DarkShieldMove(IReadOnlyList<Creature> targets)
//     {
//         await CreatureCmd.TriggerAnim(Creature, "Cast", 0.5f);
//         await CreatureCmd.GainBlock(Creature, 15m, ValueProp.Move, null);
//         await PowerCmd.Apply<StrengthPower>(
//             new ThrowingPlayerChoiceContext(), Creature, 2, Creature, null);
//     }
//
//     private async Task CurseMove(IReadOnlyList<Creature> targets)
//     {
//         await CreatureCmd.TriggerAnim(Creature, "Cast", 0.5f);
//         await DamageCmd.Attack(CurseDamage)
//             .FromMonster(this)
//             .WithAttackerFx(null, AttackSfx)
//             .WithHitFx("vfx/vfx_attack_slash")
//             .Execute(null);
//         foreach (var target in targets)
//         {
//             var player = target.Player;
//             if (player != null)
//             {
//                 var card = CombatState.CreateCard<WitchMark>(player);
//                 await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
//             }
//         }
//     }
//
//     private async Task DarkRitualMove(IReadOnlyList<Creature> targets)
//     {
//         await CreatureCmd.TriggerAnim(Creature, "Cast", 0.8f);
//         await PowerCmd.Apply<StrengthPower>(
//             new ThrowingPlayerChoiceContext(), Creature, 3, Creature, null);
//         await PowerCmd.Apply<WithPower>(
//             new ThrowingPlayerChoiceContext(), Creature, 50, Creature, null);
//         await CreatureCmd.GainBlock(Creature, 20m, ValueProp.Move, null);
//     }
//
//     private async Task VoidBlastMove(IReadOnlyList<Creature> targets)
//     {
//         await CreatureCmd.TriggerAnim(Creature, "Cast", 0.5f);
//         await DamageCmd.Attack(MagicDamage)
//             .FromMonster(this)
//             .WithAttackerFx(null, AttackSfx)
//             .WithHitFx("vfx/vfx_attack_slash")
//             .WithHitCount(3)
//             .Execute(null);
//     }
//
//     private async Task ApocalypseMove(IReadOnlyList<Creature> targets)
//     {
//         await CreatureCmd.TriggerAnim(Creature, "Cast", 0.8f);
//         await DamageCmd.Attack(CurseDamage)
//             .FromMonster(this)
//             .WithAttackerFx(null, AttackSfx)
//             .WithHitFx("vfx/vfx_attack_slash")
//             .Execute(null);
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
// }
