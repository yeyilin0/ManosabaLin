using ManosabaLin.Characters.Ema.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;

namespace ManosabaLin.Commands;

public sealed class BondConsoleCommand : AbstractConsoleCmd
{
    public override string CmdName => "bond";

    public override string Args => "<name:string> <delta:int>";

    public override string Description => "Increase/Decrease affinity/estrangement of Ema's Bond power";

    public override bool IsNetworked => true;

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        if (!CombatManager.Instance.IsInProgress)
            return new CmdResult(false, "This doesn't appear to be a combat!");
        if (args.Length < 2)
            return new CmdResult(false, "There must be 2 args.");
        if (!int.TryParse(args[1], out var delta))
            return new CmdResult(false, "Arg 2 must be the amount to change.");

        var bond = issuingPlayer!.Creature.GetPower<BondPower>();
        if (bond is null) return new CmdResult(false, "The issuing player doesn't have the Bond power!");

        switch (args[0].ToLowerInvariant())
        {
            case "a":
            case "affinity":
                bond.Affinity += delta;
                return new CmdResult(true, $"Changed affinity by {delta}.");
            case "e":
            case "estrangement":
                bond.Estrangement += delta;
                return new CmdResult(true, $"Changed estrangement by {delta}.");
            default:
                return new CmdResult(false, "Arg 1 must be either 'affinity' or 'estrangement' (or just 'a' or 'e').");
        }
    }

    public override CompletionResult GetArgumentCompletions(Player? player, string[] args)
    {
        if (args.Length <= 1)
        {
            return CompleteArgument(
                ["affinity", "estrangement"],
                [],
                args.Length == 0 ? "" : args[0]
            );
        }

        return new CompletionResult
        {
            Type = CompletionType.Argument,
            ArgumentContext = this.CmdName
        };
    }
}
