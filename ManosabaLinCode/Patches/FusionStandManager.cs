using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ManosabaLin.Patches;

internal static class FusionStandManager
{
    internal static readonly Dictionary<MonsterModel, string> PartnerVisualPaths = [];
    internal static readonly Dictionary<MonsterModel, string> PartnerMonsterIds = [];
    internal static readonly Dictionary<string, Queue<string>> PartnerNameQueues = [];

    internal static event Action? OnPairsBuilt;

    private static int _combatHash;

    internal static bool IsActiveForCurrentCombat()
    {
        return RunManager.Instance?.DebugOnlyGetState() != null;
    }

    internal static void ClearForNewCombat(int combatHash)
    {
        if (combatHash == _combatHash) return;

        _combatHash = combatHash;
        PartnerVisualPaths.Clear();
        PartnerMonsterIds.Clear();
        PartnerNameQueues.Clear();
    }

    internal static void NotifyPairsBuilt()
    {
        OnPairsBuilt?.Invoke();
    }

    internal static bool EnsurePartner(MonsterModel monster)
    {
        if (!IsActiveForCurrentCombat()) return false;
        if (PartnerMonsterIds.ContainsKey(monster)) return true;

        var combat = monster.Creature?.CombatState;
        if (combat == null) return false;

        ClearForNewCombat(combat.GetHashCode());

        var runState = RunManager.Instance?.DebugOnlyGetState();
        var actIndex = runState?.CurrentActIndex ?? 0;
        var isElite = combat.Encounter?.RoomType == RoomType.Elite;
        var runSeed = runState?.Rng.StringSeed ?? "0";
        var monsterId = ((AbstractModel)monster).Id.Entry;
        var sameMonsterIndex = combat.Enemies
            .TakeWhile(enemy => enemy != monster.Creature)
            .Count(enemy => enemy.Monster is { } other &&
                            ((AbstractModel)other).Id.Entry.Equals(monsterId, StringComparison.OrdinalIgnoreCase));

        var pool = GetPool(actIndex, isElite)
            .Where(id => !id.Equals(monsterId, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (pool.Count == 0) return false;

        var seed = StringHelper.GetDeterministicHashCode(
            $"{runSeed}_{monsterId}_N{sameMonsterIndex}_FUSION_STAND_ACT{actIndex}");
        var partnerId = pool[new Random(seed).Next(pool.Count)];

        PartnerMonsterIds[monster] = partnerId;
        PartnerVisualPaths[monster] = MakeVisualPath(partnerId);

        if (!PartnerNameQueues.TryGetValue(monsterId, out var queue))
        {
            queue = new Queue<string>();
            PartnerNameQueues[monsterId] = queue;
        }

        queue.Enqueue(partnerId);

        MainFile.Logger.Info($"[FusionStand] {monsterId} gained stand partner {partnerId}");
        NotifyPairsBuilt();
        return true;
    }

    internal static string MakeVisualPath(string monsterId)
    {
        return "res://scenes/creature_visuals/" + StringHelper.Slugify(monsterId).ToLowerInvariant() + ".tscn";
    }

    internal static IReadOnlyList<string> GetPool(int actIndex, bool isElite)
    {
        var pool = new List<string>();

        if (isElite)
        {
            if (actIndex >= 0)
            {
                pool.AddRange(Act0Pool);
                pool.AddRange(Act0ElitePool);
            }

            if (actIndex >= 1)
            {
                pool.AddRange(Act1Pool);
                pool.AddRange(Act1ElitePool);
            }

            if (actIndex >= 2)
            {
                pool.AddRange(Act2Pool);
                pool.AddRange(Act2ElitePool);
            }
        }
        else
        {
            if (actIndex >= 0)
                pool.AddRange(Act0Pool);

            if (actIndex >= 1)
            {
                pool.AddRange(Act1Pool);
                pool.AddRange(Act0Pool);
                pool.AddRange(Act0ElitePool);
            }

            if (actIndex >= 2)
            {
                pool.AddRange(Act2Pool);
                pool.AddRange(Act1Pool);
                pool.AddRange(Act1ElitePool);
                pool.AddRange(Act0Pool);
                pool.AddRange(Act0ElitePool);
            }
        }

        return pool;
    }

    private static readonly string[] Act0Pool =
    [
        "Flyconid", "BruteRubyRaider", "AssassinRubyRaider", "AxeRubyRaider", "CrossbowRubyRaider",
        "TrackerRubyRaider", "Fogmog", "Mawler", "FuzzyWurmCrawler", "Inklet", "Nibbit",
        "SnappingJaxfruit", "SlitheringStrangler", "LeafSlimeM", "LeafSlimeS", "TwigSlimeS",
        "TwigSlimeM", "ShrinkerBeetle", "VineShambler", "CubexConstruct", "PunchConstruct",
        "Toadpole", "DampCultist", "CalcifiedCultist", "GremlinMerc", "FatGremlin",
        "SneakyGremlin", "Seapunk", "FossilStalker", "LivingFog", "TwoTailedRat", "SewerClam",
        "HauntedShip", "SludgeSpinner", "CorpseSlug", "GasBomb", "Exoskeleton", "HunterKiller"
    ];

    private static readonly string[] Act0ElitePool =
    [
        "Byrdonis", "BygoneEffigy", "PhrogParasite", "TerrorEel", "PhantasmalGardener",
        "SkulkingColony"
    ];

    private static readonly string[] Act1Pool =
    [
        "Tunneler", "SpinyToad", "Chomper", "HunterKiller", "GlobeHead", "Parafright",
        "BowlbugEgg", "BowlbugNectar", "BowlbugRock", "BowlbugSilk", "LouseProgenitor",
        "SlumberingBeetle", "ThievingHopper", "Exoskeleton", "Myte", "Ovicopter", "ToughEgg",
        "DampCultist", "CalcifiedCultist", "Fogmog", "Mawler", "Seapunk", "Inklet", "Nibbit",
        "CubexConstruct", "PunchConstruct"
    ];

    private static readonly string[] Act1ElitePool =
    [
        "Decimillipede", "Entomancer", "InfestedPrism", "Byrdonis", "BygoneEffigy"
    ];

    private static readonly string[] Act2Pool =
    [
        "Axebot", "OwlMagistrate", "DevotedSculptor", "FrogKnight", "SlimedBerserker",
        "GlobeHead", "TurretOperator", "LivingShield", "TheLost", "TheForgotten",
        "ScrollOfBiting", "Fabricator", "Guardbot", "Zapbot", "Noisebot", "Stabbot",
        "PunchConstruct", "CubexConstruct", "Parafright", "VineShambler", "PhrogParasite",
        "Fogmog", "Mawler"
    ];

    private static readonly string[] Act2ElitePool =
    [
        "MechaKnight", "SoulNexus", "FlailKnight", "SpectralKnight", "MagiKnight"
    ];
}
