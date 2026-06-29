using System;
using System.Collections.Generic;
using System.Linq;
using FusionMod.Core;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace FusionMod.Patches;

[HarmonyPatch(typeof(MonsterModel), "SetUpForCombat")]
public static class FusionMonsterSetupPatch
{
	internal static readonly List<MonsterModel> Queue = new();
	internal static readonly Dictionary<MonsterModel, string> PartnerVisualPaths = new();
	internal static readonly Dictionary<MonsterModel, string> PartnerNames = new();
	internal static event Action? OnPairsBuilt;
	static int _combatHash;

	static string MakePath(string id) => "res://scenes/creature_visuals/" + StringHelper.Slugify(id).ToLowerInvariant() + ".tscn";

	static readonly string[] Act0Pool = {"Flyconid","BruteRubyRaider","AssassinRubyRaider","AxeRubyRaider","CrossbowRubyRaider","TrackerRubyRaider","Fogmog","Mawler","FuzzyWurmCrawler","Inklet","Nibbit","SnappingJaxfruit","SlitheringStrangler","LeafSlimeM","LeafSlimeS","TwigSlimeS","TwigSlimeM","ShrinkerBeetle","VineShambler","CubexConstruct","PunchConstruct","Toadpole","DampCultist","CalcifiedCultist","GremlinMerc","FatGremlin","SneakyGremlin","Seapunk","FossilStalker","LivingFog","TwoTailedRat","SewerClam","HauntedShip","SludgeSpinner","CorpseSlug","GasBomb","Exoskeleton","HunterKiller"};
	static readonly string[] Act0ElitePool = {"Byrdonis","BygoneEffigy","PhrogParasite","TerrorEel","PhantasmalGardener","SkulkingColony"};
	static readonly string[] Act1Pool = {"Tunneler","SpinyToad","Chomper","HunterKiller","GlobeHead","Parafright","BowlbugEgg","BowlbugNectar","BowlbugRock","BowlbugSilk","LouseProgenitor","SlumberingBeetle","ThievingHopper","Exoskeleton","Myte","Ovicopter","ToughEgg","DampCultist","CalcifiedCultist","Fogmog","Mawler","Seapunk","Inklet","Nibbit","CubexConstruct","PunchConstruct"};
	static readonly string[] Act1ElitePool = {"Decimillipede","Entomancer","InfestedPrism","Byrdonis","BygoneEffigy"};
	static readonly string[] Act2Pool = {"Axebot","OwlMagistrate","DevotedSculptor","FrogKnight","SlimedBerserker","GlobeHead","TurretOperator","LivingShield","TheLost","TheForgotten","ScrollOfBiting","Fabricator","Guardbot","Zapbot","Noisebot","Stabbot","PunchConstruct","CubexConstruct","Parafright","VineShambler","PhrogParasite","Fogmog","Mawler"};
	static readonly string[] Act2ElitePool = {"MechaKnight","SoulNexus","FlailKnight","SpectralKnight","MagiKnight"};

	[HarmonyPostfix]
	public static void Postfix(MonsterModel __instance)
	{
		if (!FusionManager.IsFusionModeActive) return;
		if (!FusionManager.IsCurrentNodeFusion()) return;

		try
		{
			// 融合伤害削弱：给每个融合节点怪物加上FusionWeaken1Power
			try { MegaCrit.Sts2.Core.Commands.PowerCmd.Apply<FusionWeaken1Power>(new MegaCrit.Sts2.Core.GameActions.Multiplayer.ThrowingPlayerChoiceContext(), __instance.Creature, 1m, __instance.Creature, null); } catch { }

			if (!Queue.Contains(__instance)) Queue.Add(__instance);
			var combat = __instance.Creature?.CombatState;
			if (combat == null) return;
			int total = 0; foreach (var _ in combat.Enemies) total++;
			if (Queue.Count < total) return;

			// 新战斗时清除，同一战斗只追加（应对召唤物等中途出现的怪物）
			int hash = __instance.Creature?.CombatState?.GetHashCode() ?? 0;
			if (hash != _combatHash) { PartnerVisualPaths.Clear(); PartnerNames.Clear(); _combatHash = hash; }
			int act = RunManager.Instance?.DebugOnlyGetState()?.CurrentActIndex ?? 0;
			bool isElite = __instance.Creature?.CombatState?.Encounter?.RoomType == MegaCrit.Sts2.Core.Rooms.RoomType.Elite;
			string runSeed = RunManager.Instance?.DebugOnlyGetState()?.Rng.StringSeed ?? "0";

			int dupCounter = 0; string lastMid = "";
			foreach (var m in Queue)
			{
				if (PartnerNames.ContainsKey(m)) continue; // 已有搭档，跳过
				string mid = ((AbstractModel)m).Id.Entry;
				var pool = new List<string>();

				if (isElite) { if (act>=0){pool.AddRange(Act0Pool);pool.AddRange(Act0ElitePool);} if (act>=1){pool.AddRange(Act1Pool);pool.AddRange(Act1ElitePool);} if (act>=2){pool.AddRange(Act2Pool);pool.AddRange(Act2ElitePool);} }
				else { if (act>=0) pool.AddRange(Act0Pool); if (act>=1){pool.AddRange(Act1Pool);pool.AddRange(Act0Pool);pool.AddRange(Act0ElitePool);} if (act>=2){pool.AddRange(Act2Pool);pool.AddRange(Act1Pool);pool.AddRange(Act1ElitePool);pool.AddRange(Act0Pool);pool.AddRange(Act0ElitePool);} }

				pool.RemoveAll(p => p.Equals(mid, StringComparison.OrdinalIgnoreCase));
				if (pool.Count == 0) continue;

				// 同ID怪物用序号区分（存档读档/多人按怪物出现顺序一致）
				if (mid == lastMid) dupCounter++; else { dupCounter = 0; lastMid = mid; }
				int seed = StringHelper.GetDeterministicHashCode($"{runSeed}_{mid}_N{dupCounter}_FUSION_ACT{act}");
				string pick = pool[new Random(seed).Next(pool.Count)];
				PartnerVisualPaths[m] = MakePath(pick);
				PartnerNames[m] = pick;
			}
			Queue.Clear();
			// 构建同ID怪物的轮替名字队列
			FusionLocalizationPatch.NameQueues.Clear();
			foreach (var kv in PartnerNames)
			{
				string eid = ((AbstractModel)kv.Key).Id.Entry;
				if (!FusionLocalizationPatch.NameQueues.ContainsKey(eid))
					FusionLocalizationPatch.NameQueues[eid] = new Queue<string>();
				FusionLocalizationPatch.NameQueues[eid].Enqueue(kv.Value);
			}
			FusionModMain.Logger?.Info($"融合配对: {PartnerNames.Count}怪 act={act} elite={isElite}", 1);
			OnPairsBuilt?.Invoke();
		}
		catch (Exception ex) { FusionModMain.Logger?.Error($"配对异常: {ex}", 2); }
	}
}
