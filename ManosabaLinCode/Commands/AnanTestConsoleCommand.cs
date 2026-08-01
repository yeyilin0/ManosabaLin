using System.Text;
using ManosabaLin.Characters.Ananlin.Cards;
using ManosabaLin.Characters.Ananlin.Powers;
using ManosabaLin.Characters.Ananlin.Relics;
using ManosabaLin.Characters.Hiro.Monsters;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ManosabaLin.Commands;

public sealed class AnanTestConsoleCommand : AbstractConsoleCmd
{
    private static readonly Type[] SilenceCards =
    [
        typeof(AnanlinSilentForeshadow),
        typeof(AnanlinLowerVoice),
        typeof(AnanlinInterrogationPause),
        typeof(AnanlinOppressiveSilence),
        typeof(AnanlinRhetoricalQuestion),
        typeof(AnanlinNoAnswer),
        typeof(AnanlinClerkHint),
        typeof(AnanlinDeleteStress),
        typeof(AnanlinJudgmentEve),
        typeof(AnanlinKeepDistance),
        typeof(AnanlinLowVoiceRepetition)
    ];

    private static readonly Type[] SketchCards =
    [
        typeof(AnanlinTracing),
        typeof(AnanlinIndexPage),
        typeof(AnanlinThreeColorBookmark),
        typeof(AnanlinMarginalNote),
        typeof(AnanlinBorrowingPeriod),
        typeof(AnanlinCrossReference),
        typeof(AnanlinMiliaAssist),
        typeof(AnanlinNoahAssist),
        typeof(AnanlinTrialReading),
        typeof(AnanlinSpreadProofreading),
        typeof(AnanlinTemporaryBorrowing)
    ];

    public override string CmdName => "anan_test";

    public override string Args => "<status|setup|record|cards|silence|blank|margin|guard3>";

    public override string Description => "Debug helpers for Ananlin sketchbook and silence card testing.";

    public override bool IsNetworked => true;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (args.Length == 0 || args[0].Equals("help", StringComparison.OrdinalIgnoreCase))
            return new CmdResult(true, Usage);

        return args[0].ToLowerInvariant() switch
        {
            "status" => Status(issuingPlayer),
            "setup" => RequireRunPlayer(issuingPlayer, player =>
                new CmdResult(Setup(player), true, "Ananlin test setup queued. Run `anan_test status` after it finishes.")),
            "record" => Record(issuingPlayer, args.Skip(1).ToArray()),
            "cards" => Cards(issuingPlayer, args.Skip(1).ToArray()),
            "silence" => Silence(issuingPlayer, args.Skip(1).ToArray()),
            "blank" => Cards(issuingPlayer, ["blank", args.ElementAtOrDefault(1) ?? "hand"]),
            "margin" => Cards(issuingPlayer, ["margin", args.ElementAtOrDefault(1) ?? "hand"]),
            "guard3" => GuardThree(issuingPlayer),
            _ => new CmdResult(false, Usage)
        };
    }

    public override CompletionResult GetArgumentCompletions(Player? player, string[] args)
    {
        if (args.Length <= 1)
            return CompleteArgument(
                ["status", "setup", "record", "cards", "silence", "blank", "margin", "guard3"],
                [],
                args.FirstOrDefault() ?? "");

        if (args.Length == 2 && args[0].Equals("cards", StringComparison.OrdinalIgnoreCase))
            return CompleteArgument(["silence", "sketch", "all", "blank", "margin"], [args[0]], args[1]);

        if (args.Length == 3 && args[0].Equals("cards", StringComparison.OrdinalIgnoreCase))
            return CompleteArgument(["hand", "draw", "discard", "exhaust"], [args[0], args[1]], args[2]);

        return new CompletionResult
        {
            Type = CompletionType.Argument,
            ArgumentContext = CmdName
        };
    }

    private static CmdResult RequireRunPlayer(Player? player, Func<Player, CmdResult> command)
    {
        if (player is null || !RunManager.Instance.IsInProgress)
            return new CmdResult(false, "Start or load a run first.");

        return command(player);
    }

    private static CmdResult RequireCombatPlayer(Player? player, Func<Player, CmdResult> command)
    {
        if (player is null || !RunManager.Instance.IsInProgress)
            return new CmdResult(false, "Start or load a run first.");
        if (!CombatManager.Instance.IsInProgress || player.PlayerCombatState is null)
            return new CmdResult(false, "Enter combat first.");

        return command(player);
    }

    private static CmdResult Status(Player? player)
    {
        if (player is null || !RunManager.Instance.IsInProgress)
            return new CmdResult(false, "No active run.");

        var sketchbook = player.Relics.OfType<AnansSketchbook>().FirstOrDefault();
        var builder = new StringBuilder();
        builder.AppendLine(sketchbook is null
            ? "Sketchbook: missing"
            : $"Sketchbook: pools=[{string.Join(", ", sketchbook.RecordedPoolEntries)}], silence={sketchbook.CurrentSilence}, attacks={sketchbook.AttacksPlayedThisTurn}, skills={sketchbook.SkillsPlayedThisTurn}");

        if (CombatManager.Instance.IsInProgress && player.PlayerCombatState is not null)
        {
            builder.AppendLine($"Combat: hand={PileType.Hand.GetPile(player).Cards.Count}, draw={PileType.Draw.GetPile(player).Cards.Count}, discard={PileType.Discard.GetPile(player).Cards.Count}, exhaust={PileType.Exhaust.GetPile(player).Cards.Count}, energy={player.PlayerCombatState.Energy}");
            var enemies = CombatManager.Instance.DebugOnlyGetState()?.Enemies ?? [];
            builder.AppendLine(enemies.Count == 0
                ? "Enemies: none"
                : "Enemies: " + string.Join("; ", enemies.Select(DescribeEnemy)));
        }
        else
        {
            builder.AppendLine("Combat: not in combat");
        }

        return new CmdResult(true, builder.ToString().TrimEnd());
    }

    private static async Task Setup(Player player)
    {
        var sketchbook = await EnsureSketchbook(player);
        RecordDefaultPools(sketchbook);

        if (!CombatManager.Instance.IsInProgress || player.PlayerCombatState is null) return;

        await AddCards(player, SilenceCards, PileType.Draw);
        await AddCards(player, SketchCards, PileType.Discard);
        await AddCards(player, [typeof(BlankPage), typeof(MarginPage)], PileType.Hand);
        await PowerCmd.Apply<SilentPower>(new BlockingPlayerChoiceContext(), player.Creature, 13, player.Creature, null);
        await PlayerCmd.GainEnergy(10, player);
    }

    private static CmdResult Record(Player? issuingPlayer, string[] args)
    {
        return RequireRunPlayer(issuingPlayer, player =>
        {
            var task = RecordTask(player, args);
            return new CmdResult(task, true, "Pool recording queued. Run `anan_test status` after it finishes.");
        });
    }

    private static async Task RecordTask(Player player, string[] args)
    {
        var sketchbook = await EnsureSketchbook(player);

        if (args.Length == 0)
        {
            RecordDefaultPools(sketchbook);
            return;
        }

        foreach (var arg in args)
        {
            var pool = FindCardPool(arg);
            if (pool is not null)
                sketchbook.TryRecordPool(pool);
        }

        sketchbook.InvokeDisplayAmountChanged();
    }

    private static CmdResult Cards(Player? issuingPlayer, string[] args)
    {
        return RequireCombatPlayer(issuingPlayer, player =>
        {
            var group = args.ElementAtOrDefault(0) ?? "all";
            if (!TryGetCardGroup(group, out var cardTypes))
                return new CmdResult(false, "Unknown card group. Use silence, sketch, all, or blank.");

            var pileName = args.ElementAtOrDefault(1) ?? "hand";
            if (!TryParseEnum<PileType>(pileName, out var pile) || !pile.IsCombatPile() || pile == PileType.Play)
                return new CmdResult(false, "Pile must be hand, draw, discard, or exhaust.");

            return new CmdResult(AddCards(player, cardTypes, pile), true, $"Adding {cardTypes.Length} {group} cards to {pile}.");
        });
    }

    private static async Task<int> AddCards(Player player, IReadOnlyList<Type> cardTypes, PileType pile)
    {
        var added = 0;
        var combat = CombatManager.Instance.DebugOnlyGetState();
        if (combat is null) return added;

        foreach (var cardType in cardTypes)
        {
            if (pile == PileType.Hand && PileType.Hand.GetPile(player).Cards.Count >= CardPile.MaxCardsInHand)
                break;

            var card = combat.CreateCard(ModelDb.GetById<CardModel>(ModelDb.GetId(cardType)), player);
            var result = await CardPileCmd.Add(card, pile, CardPilePosition.Bottom, null, skipVisuals: true);
            if (result.success)
                added++;
        }

        Log.Info($"anan_test added {added}/{cardTypes.Count} cards to {pile}.");
        return added;
    }

    private static CmdResult Silence(Player? issuingPlayer, string[] args)
    {
        return RequireCombatPlayer(issuingPlayer, player =>
        {
            if (args.Length == 0 || !int.TryParse(args[0], out var amount))
                amount = 13;

            return new CmdResult(
                PowerCmd.Apply<SilentPower>(new BlockingPlayerChoiceContext(), player.Creature, amount, player.Creature, null),
                true,
                $"Adding {amount} Silence.");
        });
    }

    private static CmdResult GuardThree(Player? issuingPlayer)
    {
        return RequireCombatPlayer(issuingPlayer, _ =>
        {
            var guard = CombatManager.Instance.DebugOnlyGetState()
                ?.Enemies
                .Select(static creature => creature.Monster)
                .OfType<GuardThreeMonster>()
                .FirstOrDefault();

            if (guard is null)
                return new CmdResult(false, "GuardThreeMonster is not in this combat. Use `fight MANOSABA_LIN_ENCOUNTER_GUARD_THREE_ENCOUNTER` first.");

            return new CmdResult(GuardThreeTask(guard), true, "GuardThree phase-two test queued. Run `anan_test status` after the transition.");
        });
    }

    private static async Task GuardThreeTask(GuardThreeMonster guard)
    {
        await guard.EnterPhaseTwo();

        var node = NCombatRoom.Instance?.GetCreatureNode(guard.Creature);
        if (node is null)
        {
            Log.Warn("anan_test guard3: creature node not found.");
            return;
        }

        Log.Info($"anan_test guard3 hitbox pos={node.Hitbox.GlobalPosition} size={node.Hitbox.Size} intent={node.IntentContainer.Position}");
    }

    private static async Task<AnansSketchbook> EnsureSketchbook(Player player)
    {
        var sketchbook = player.Relics.OfType<AnansSketchbook>().FirstOrDefault();
        return sketchbook ?? await RelicCmd.Obtain<AnansSketchbook>(player);
    }

    private static void RecordDefaultPools(AnansSketchbook sketchbook)
    {
        sketchbook.TryRecordPool(ModelDb.CardPool<IroncladCardPool>());
        sketchbook.TryRecordPool(ModelDb.CardPool<SilentCardPool>());
        sketchbook.TryRecordPool(ModelDb.CardPool<DefectCardPool>());
        sketchbook.InvokeDisplayAmountChanged();
    }

    private static CardPoolModel? FindCardPool(string token)
    {
        var normalized = token.Trim().ToUpperInvariant();
        return ModelDb.AllCardPools.FirstOrDefault(pool => pool.Id.Entry == normalized)
            ?? ModelDb.AllCardPools.FirstOrDefault(pool => pool.Id.Entry.Contains(normalized, StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryGetCardGroup(string group, out Type[] cardTypes)
    {
        switch (group.ToLowerInvariant())
        {
            case "silence":
                cardTypes = SilenceCards;
                return true;
            case "sketch":
                cardTypes = SketchCards;
                return true;
            case "blank":
                cardTypes = [typeof(BlankPage)];
                return true;
            case "margin":
                cardTypes = [typeof(MarginPage)];
                return true;
            case "all":
                cardTypes = SilenceCards.Concat(SketchCards).ToArray();
                return true;
            default:
                cardTypes = [];
                return false;
        }
    }

    private static string DescribeEnemy(Creature enemy)
    {
        var move = enemy.Monster?.NextMove;
        return move is null
            ? $"{enemy.Monster?.Id.Entry ?? "UNKNOWN"}: no move"
            : $"{enemy.Monster?.Id.Entry ?? "UNKNOWN"}: {move.StateId} [{string.Join(", ", move.Intents.Select(static intent => intent.GetType().Name))}]";
    }

    private const string Usage =
        "anan_test status\n" +
        "anan_test setup\n" +
        "anan_test record [pool-id...]\n" +
        "anan_test cards <silence|sketch|all|blank|margin> [hand|draw|discard|exhaust]\n" +
        "anan_test silence [amount]\n" +
        "anan_test blank [hand|draw|discard|exhaust]\n" +
        "anan_test margin [hand|draw|discard|exhaust]\n" +
        "anan_test guard3";
}
