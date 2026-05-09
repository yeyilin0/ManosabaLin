using System.Reflection;
using System.Text.RegularExpressions;
using Godot;
using ManosabaLin.Characters.Hiro;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Screens.CardSelection;
using STS2RitsuLib.Scaffolding.Content;

namespace ManosabaLin.Characters.Common;

internal readonly record struct MoveChoice(MoveState Move, string Label, string IntentSummary, string MonsterId);

internal readonly record struct MoveChoiceSelection(MoveState Move, MoveChoiceCard Card);

internal abstract class MoveChoiceCard : CardModel
{
    private const int HiddenEnergyCost = -1;
    private static readonly string AttackPortrait = $"{MainFile.ResPath}/images/cards/HiroAttack.png";
    private static readonly string DefaultPortrait = $"{MainFile.ResPath}/images/cards/HiroDefend.png";

    private CardPoolModel? _cardPool;
    private MoveChoice? _choice;

    protected MoveChoiceCard(CardType cardType = CardType.Skill, CardRarity rarity = CardRarity.Token)
        : base(HiddenEnergyCost, cardType, rarity, TargetType.None, shouldShowInCardLibrary: false)
    {
    }

    public string Label => _choice?.Label ?? string.Empty;
    public MoveState? Move => _choice?.Move;
    internal bool IsConfigured => _choice is not null;
    internal string DisplayTitle => Label;
    internal string StateId => _choice?.Move.StateId ?? string.Empty;
    internal string IntentSummary => _choice?.IntentSummary ?? string.Empty;
    internal string MonsterId => _choice?.MonsterId ?? string.Empty;
    internal LocString TitleOverride => new(RedirectMoveChoiceScreen.LocTable, $"{Id.Entry}.title");
    internal LocString DescriptionOverride => new(RedirectMoveChoiceScreen.LocTable, $"{Id.Entry}.description");
    public override CardPoolModel Pool => _cardPool ?? ModelDb.CardPool<HiroCardPool>();
    public override CardPoolModel VisualCardPool => Pool;
    public override string PortraitPath => GetIntentPortrait();
    public override IEnumerable<string> AllPortraitPaths => new[] { PortraitPath };

    private string GetIntentPortrait()
    {
        if (_choice is not { } choice) return DefaultPortrait;

        foreach (var path in GetPortraitCandidates(choice))
            if (ResourceLoader.Exists(path))
                return path;

        return DefaultPortrait;
    }

    private static IEnumerable<string> GetPortraitCandidates(MoveChoice choice)
    {
        var primary = choice.Move.Intents.FirstOrDefault();
        if (primary is not null)
            foreach (var path in GetIntentPortraitCandidates(primary))
                yield return path;

        yield return primary?.IntentType switch
        {
            IntentType.Attack or IntentType.DeathBlow => AttackPortrait,
            _ => DefaultPortrait
        };
    }

    private static IEnumerable<string> GetIntentPortraitCandidates(AbstractIntent intent)
    {
        if (intent is AttackIntent attack)
            yield return $"res://images/packed/intents/attack/intent_attack_{GetAttackIntentTier(attack)}.png";

        var staticIcon = intent.IntentType switch
        {
            IntentType.Attack => "intent_attack",
            IntentType.Buff => "intent_buff",
            IntentType.CardDebuff => "intent_card_debuff",
            IntentType.DeathBlow => "intent_death_blow",
            IntentType.Debuff or IntentType.DebuffStrong => "intent_debuff",
            IntentType.Defend => "intent_defend",
            IntentType.Escape => "intent_escape",
            IntentType.Heal => "intent_heal",
            IntentType.Hidden => "intent_hidden",
            IntentType.Sleep => "intent_sleep",
            IntentType.StatusCard => "intent_status_card",
            IntentType.Stun => "intent_stun",
            IntentType.Summon => "intent_summon",
            IntentType.Unknown => "intent_unknown",
            _ => null
        };

        if (staticIcon is not null)
            yield return $"res://images/packed/intents/{staticIcon}.png";
    }

    private static int GetAttackIntentTier(AttackIntent attack)
    {
        var totalDamage = (int)Math.Max(0m, GetAttackDamageValue(attack) * Math.Max(1, attack.Repeats));
        return totalDamage switch
        {
            < 5 => 1, < 10 => 2, < 20 => 3, < 40 => 4, _ => 5
        };
    }

    private static decimal GetAttackDamageValue(AttackIntent attack)
    {
        try { return attack.DamageCalc?.Invoke() ?? 0m; }
        catch { return 0m; }
    }

    public override bool CanBeGeneratedInCombat => false;
    public override bool CanBeGeneratedByModifiers => false;
    public override int MaxUpgradeLevel => 0;
    public override bool ShouldReceiveCombatHooks => false;

    internal void Configure(MoveChoice choice, CardPoolModel cardPool, Player player)
    {
        AssertMutable();
        _choice = choice;
        _cardPool = cardPool;
        Owner = player;
    }
}

// Attack type cards
internal abstract class MoveChoiceCardAtk : MoveChoiceCard
{
    protected MoveChoiceCardAtk() : base(CardType.Attack, CardRarity.Uncommon) { }
}
internal abstract class MoveChoiceCardAtkCom : MoveChoiceCard
{
    protected MoveChoiceCardAtkCom() : base(CardType.Attack, CardRarity.Common) { }
}
internal sealed class MoveChoiceCardAtk00 : MoveChoiceCardAtk { public MoveChoiceCardAtk00() { } }
internal sealed class MoveChoiceCardAtk01 : MoveChoiceCardAtk { public MoveChoiceCardAtk01() { } }
internal sealed class MoveChoiceCardAtk02 : MoveChoiceCardAtk { public MoveChoiceCardAtk02() { } }
internal sealed class MoveChoiceCardAtk03 : MoveChoiceCardAtk { public MoveChoiceCardAtk03() { } }
internal sealed class MoveChoiceCardAtk04 : MoveChoiceCardAtk { public MoveChoiceCardAtk04() { } }
internal sealed class MoveChoiceCardAtk05 : MoveChoiceCardAtk { public MoveChoiceCardAtk05() { } }
internal sealed class MoveChoiceCardAtkCom00 : MoveChoiceCardAtkCom { public MoveChoiceCardAtkCom00() { } }
internal sealed class MoveChoiceCardAtkCom01 : MoveChoiceCardAtkCom { public MoveChoiceCardAtkCom01() { } }
internal sealed class MoveChoiceCardAtkCom02 : MoveChoiceCardAtkCom { public MoveChoiceCardAtkCom02() { } }
internal sealed class MoveChoiceCardAtkCom03 : MoveChoiceCardAtkCom { public MoveChoiceCardAtkCom03() { } }
internal sealed class MoveChoiceCardAtkCom04 : MoveChoiceCardAtkCom { public MoveChoiceCardAtkCom04() { } }
internal sealed class MoveChoiceCardAtkCom05 : MoveChoiceCardAtkCom { public MoveChoiceCardAtkCom05() { } }

// Skill type cards
internal abstract class MoveChoiceCardSkl : MoveChoiceCard
{
    protected MoveChoiceCardSkl() : base(CardType.Skill, CardRarity.Uncommon) { }
}
internal abstract class MoveChoiceCardSklCom : MoveChoiceCard
{
    protected MoveChoiceCardSklCom() : base(CardType.Skill, CardRarity.Common) { }
}
internal sealed class MoveChoiceCardSkl00 : MoveChoiceCardSkl { public MoveChoiceCardSkl00() { } }
internal sealed class MoveChoiceCardSkl01 : MoveChoiceCardSkl { public MoveChoiceCardSkl01() { } }
internal sealed class MoveChoiceCardSkl02 : MoveChoiceCardSkl { public MoveChoiceCardSkl02() { } }
internal sealed class MoveChoiceCardSkl03 : MoveChoiceCardSkl { public MoveChoiceCardSkl03() { } }
internal sealed class MoveChoiceCardSkl04 : MoveChoiceCardSkl { public MoveChoiceCardSkl04() { } }
internal sealed class MoveChoiceCardSkl05 : MoveChoiceCardSkl { public MoveChoiceCardSkl05() { } }
internal sealed class MoveChoiceCardSklCom00 : MoveChoiceCardSklCom { public MoveChoiceCardSklCom00() { } }
internal sealed class MoveChoiceCardSklCom01 : MoveChoiceCardSklCom { public MoveChoiceCardSklCom01() { } }
internal sealed class MoveChoiceCardSklCom02 : MoveChoiceCardSklCom { public MoveChoiceCardSklCom02() { } }
internal sealed class MoveChoiceCardSklCom03 : MoveChoiceCardSklCom { public MoveChoiceCardSklCom03() { } }
internal sealed class MoveChoiceCardSklCom04 : MoveChoiceCardSklCom { public MoveChoiceCardSklCom04() { } }
internal sealed class MoveChoiceCardSklCom05 : MoveChoiceCardSklCom { public MoveChoiceCardSklCom05() { } }

// Power type cards
internal abstract class MoveChoiceCardPow : MoveChoiceCard
{
    protected MoveChoiceCardPow() : base(CardType.Power, CardRarity.Uncommon) { }
}
internal abstract class MoveChoiceCardPowRare : MoveChoiceCard
{
    protected MoveChoiceCardPowRare() : base(CardType.Power, CardRarity.Rare) { }
}
internal sealed class MoveChoiceCardPow00 : MoveChoiceCardPow { public MoveChoiceCardPow00() { } }
internal sealed class MoveChoiceCardPow01 : MoveChoiceCardPow { public MoveChoiceCardPow01() { } }
internal sealed class MoveChoiceCardPow02 : MoveChoiceCardPow { public MoveChoiceCardPow02() { } }
internal sealed class MoveChoiceCardPow03 : MoveChoiceCardPow { public MoveChoiceCardPow03() { } }
internal sealed class MoveChoiceCardPow04 : MoveChoiceCardPow { public MoveChoiceCardPow04() { } }
internal sealed class MoveChoiceCardPow05 : MoveChoiceCardPow { public MoveChoiceCardPow05() { } }
internal sealed class MoveChoiceCardPowRare00 : MoveChoiceCardPowRare { public MoveChoiceCardPowRare00() { } }
internal sealed class MoveChoiceCardPowRare01 : MoveChoiceCardPowRare { public MoveChoiceCardPowRare01() { } }
internal sealed class MoveChoiceCardPowRare02 : MoveChoiceCardPowRare { public MoveChoiceCardPowRare02() { } }
internal sealed class MoveChoiceCardPowRare03 : MoveChoiceCardPowRare { public MoveChoiceCardPowRare03() { } }
internal sealed class MoveChoiceCardPowRare04 : MoveChoiceCardPowRare { public MoveChoiceCardPowRare04() { } }
internal sealed class MoveChoiceCardPowRare05 : MoveChoiceCardPowRare { public MoveChoiceCardPowRare05() { } }

internal static class RedirectMoveChoiceScreen
{
    private static readonly Func<MoveChoiceCard>[] AtkFactories =
    [
        static () => (MoveChoiceCard)ModelDb.Card<MoveChoiceCardAtk00>().ToMutable(),
        static () => (MoveChoiceCard)ModelDb.Card<MoveChoiceCardAtk01>().ToMutable(),
        static () => (MoveChoiceCard)ModelDb.Card<MoveChoiceCardAtk02>().ToMutable(),
        static () => (MoveChoiceCard)ModelDb.Card<MoveChoiceCardAtk03>().ToMutable(),
        static () => (MoveChoiceCard)ModelDb.Card<MoveChoiceCardAtk04>().ToMutable(),
        static () => (MoveChoiceCard)ModelDb.Card<MoveChoiceCardAtk05>().ToMutable()
    ];
    private static readonly Func<MoveChoiceCard>[] SklFactories =
    [
        static () => (MoveChoiceCard)ModelDb.Card<MoveChoiceCardSkl00>().ToMutable(),
        static () => (MoveChoiceCard)ModelDb.Card<MoveChoiceCardSkl01>().ToMutable(),
        static () => (MoveChoiceCard)ModelDb.Card<MoveChoiceCardSkl02>().ToMutable(),
        static () => (MoveChoiceCard)ModelDb.Card<MoveChoiceCardSkl03>().ToMutable(),
        static () => (MoveChoiceCard)ModelDb.Card<MoveChoiceCardSkl04>().ToMutable(),
        static () => (MoveChoiceCard)ModelDb.Card<MoveChoiceCardSkl05>().ToMutable()
    ];
    private static readonly Func<MoveChoiceCard>[] PowFactories =
    [
        static () => (MoveChoiceCard)ModelDb.Card<MoveChoiceCardPow00>().ToMutable(),
        static () => (MoveChoiceCard)ModelDb.Card<MoveChoiceCardPow01>().ToMutable(),
        static () => (MoveChoiceCard)ModelDb.Card<MoveChoiceCardPow02>().ToMutable(),
        static () => (MoveChoiceCard)ModelDb.Card<MoveChoiceCardPow03>().ToMutable(),
        static () => (MoveChoiceCard)ModelDb.Card<MoveChoiceCardPow04>().ToMutable(),
        static () => (MoveChoiceCard)ModelDb.Card<MoveChoiceCardPow05>().ToMutable()
    ];
    private static readonly Func<MoveChoiceCard>[] AtkComFactories =
    [
        static () => (MoveChoiceCard)ModelDb.Card<MoveChoiceCardAtkCom00>().ToMutable(),
        static () => (MoveChoiceCard)ModelDb.Card<MoveChoiceCardAtkCom01>().ToMutable(),
        static () => (MoveChoiceCard)ModelDb.Card<MoveChoiceCardAtkCom02>().ToMutable(),
        static () => (MoveChoiceCard)ModelDb.Card<MoveChoiceCardAtkCom03>().ToMutable(),
        static () => (MoveChoiceCard)ModelDb.Card<MoveChoiceCardAtkCom04>().ToMutable(),
        static () => (MoveChoiceCard)ModelDb.Card<MoveChoiceCardAtkCom05>().ToMutable()
    ];
    private static readonly Func<MoveChoiceCard>[] SklComFactories =
    [
        static () => (MoveChoiceCard)ModelDb.Card<MoveChoiceCardSklCom00>().ToMutable(),
        static () => (MoveChoiceCard)ModelDb.Card<MoveChoiceCardSklCom01>().ToMutable(),
        static () => (MoveChoiceCard)ModelDb.Card<MoveChoiceCardSklCom02>().ToMutable(),
        static () => (MoveChoiceCard)ModelDb.Card<MoveChoiceCardSklCom03>().ToMutable(),
        static () => (MoveChoiceCard)ModelDb.Card<MoveChoiceCardSklCom04>().ToMutable(),
        static () => (MoveChoiceCard)ModelDb.Card<MoveChoiceCardSklCom05>().ToMutable()
    ];
    private static readonly Func<MoveChoiceCard>[] PowRareFactories =
    [
        static () => (MoveChoiceCard)ModelDb.Card<MoveChoiceCardPowRare00>().ToMutable(),
        static () => (MoveChoiceCard)ModelDb.Card<MoveChoiceCardPowRare01>().ToMutable(),
        static () => (MoveChoiceCard)ModelDb.Card<MoveChoiceCardPowRare02>().ToMutable(),
        static () => (MoveChoiceCard)ModelDb.Card<MoveChoiceCardPowRare03>().ToMutable(),
        static () => (MoveChoiceCard)ModelDb.Card<MoveChoiceCardPowRare04>().ToMutable(),
        static () => (MoveChoiceCard)ModelDb.Card<MoveChoiceCardPowRare05>().ToMutable()
    ];

    internal const string LocTable = "cards";

    public static async Task<MoveChoiceSelection?> Choose(
        PlayerChoiceContext choiceContext,
        MonsterModel monster,
        IReadOnlyList<MoveState> moves,
        Player player,
        CardPoolModel cardPool)
    {
        if (moves.Count == 0) return null;

        var choices = CreateChoices(monster, moves);
        var cards = choices
            .Select((choice, index) => CreateChoiceCard(choice, cardPool, player, index))
            .OfType<MoveChoiceCard>()
            .ToArray();
        if (cards.Length == 0) return null;

        RegisterChoiceLocalization(cards);

        var screen = NChooseACardSelectionScreen.ShowScreen(cards, canSkip: false);
        var selectedCard = screen is null
            ? null
            : (await screen.CardsSelected()).FirstOrDefault() as MoveChoiceCard;

        if (selectedCard is null) return null;

        var selectedIndex = Array.IndexOf(cards, selectedCard);
        var selectedMove = selectedCard.Move ?? moves.ElementAtOrDefault(selectedIndex);

        return selectedMove is null ? null : new MoveChoiceSelection(selectedMove, selectedCard);
    }

    private static MoveChoiceCard? CreateChoiceCard(MoveChoice choice, CardPoolModel cardPool, Player player, int index)
    {
        var factories = GetFactoriesForMove(choice.Move);
        if (index >= factories.Length) return null;

        var card = factories[index]();
        card.Configure(choice, cardPool, player);
        return card;
    }

    private static Func<MoveChoiceCard>[] GetFactoriesForMove(MoveState move)
    {
        var (cardType, rarity) = GetCardTypeAndRarity(move);
        return (cardType, rarity) switch
        {
            (CardType.Attack, CardRarity.Common) => AtkComFactories,
            (CardType.Attack, _) => AtkFactories,
            (CardType.Skill, CardRarity.Common) => SklComFactories,
            (CardType.Skill, _) => SklFactories,
            (CardType.Power, CardRarity.Rare) => PowRareFactories,
            (CardType.Power, _) => PowFactories,
            _ => SklFactories
        };
    }

    private static (CardType cardType, CardRarity rarity) GetCardTypeAndRarity(MoveState move)
    {
        var primary = move.Intents.FirstOrDefault();
        if (primary is null) return (CardType.Skill, CardRarity.Uncommon);

        return primary.IntentType switch
        {
            IntentType.Attack => (CardType.Attack, CardRarity.Common),
            IntentType.DeathBlow => (CardType.Attack, CardRarity.Rare),
            IntentType.Defend => (CardType.Skill, CardRarity.Common),
            IntentType.StatusCard => (CardType.Skill, CardRarity.Common),
            IntentType.Debuff or IntentType.DebuffStrong => (CardType.Skill, CardRarity.Uncommon),
            IntentType.Escape => (CardType.Skill, CardRarity.Uncommon),
            IntentType.Sleep => (CardType.Skill, CardRarity.Uncommon),
            IntentType.Stun => (CardType.Skill, CardRarity.Uncommon),
            IntentType.CardDebuff => (CardType.Skill, CardRarity.Uncommon),
            IntentType.Buff => (CardType.Power, CardRarity.Rare),
            IntentType.Heal => (CardType.Power, CardRarity.Rare),
            IntentType.Summon => (CardType.Power, CardRarity.Rare),
            _ => (CardType.Skill, CardRarity.Uncommon)
        };
    }

    private static void RegisterChoiceLocalization(IEnumerable<MoveChoiceCard> cards)
    {
        var entries = new Dictionary<string, string>();
        foreach (var card in cards)
        {
            entries[$"{card.Id.Entry}.title"] = card.Label;
            entries[$"{card.Id.Entry}.description"] = card.IntentSummary;
        }
        LocManager.Instance.GetTable(LocTable).MergeWith(entries);
    }

    private static IReadOnlyList<MoveChoice> CreateChoices(MonsterModel monster, IReadOnlyList<MoveState> moves)
    {
        var monsterId = monster.Id.Entry;
        var table = LocManager.Instance.GetTable("monsters");

        var baseChoices = moves
            .Select(move => new MoveChoice(
                move,
                FindMoveName(table, monsterId, move.StateId) ?? GetIntentTitle(move) ?? move.StateId,
                GetIntentSummary(move),
                monsterId))
            .ToArray();

        var labelCounts = baseChoices
            .GroupBy(static c => c.Label)
            .ToDictionary(static g => g.Key, static g => g.Count());

        return baseChoices
            .Select(c => labelCounts[c.Label] > 1
                ? c with { Label = $"{c.Label} ({ShortStateId(c.Move.StateId)})" }
                : c)
            .ToArray();
    }

    private static string? FindMoveName(LocTable table, string monsterId, string rawId)
    {
        foreach (var candidate in GetMoveLocKeyCandidates(rawId))
        {
            var key = $"{monsterId}.moves.{candidate}.title";
            if (table.HasEntry(key)) return table.GetRawText(key);
            key = $"{monsterId}.moves.{candidate}";
            if (table.HasEntry(key)) return table.GetRawText(key);
        }
        return null;
    }

    private static IEnumerable<string> GetMoveLocKeyCandidates(string stateId)
    {
        var candidates = new List<string>();
        var seen = new HashSet<string>();

        void Add(string c) { if (!string.IsNullOrWhiteSpace(c) && seen.Add(c)) candidates.Add(c); }

        if (stateId.EndsWith("_MOVE", StringComparison.Ordinal))
            Add(stateId[..^5]);

        var moveNumberSuffixMatch = Regex.Match(stateId, @"^(.+)_MOVE_\d+$");
        if (moveNumberSuffixMatch.Success)
            Add(moveNumberSuffixMatch.Groups[1].Value);

        var numberBeforeMoveMatch = Regex.Match(stateId, @"^(.+)_\d+_MOVE$");
        if (numberBeforeMoveMatch.Success)
        {
            Add(stateId[..^5]);
            Add(numberBeforeMoveMatch.Groups[1].Value);
        }

        Add(stateId);
        return candidates;
    }

    private static string? GetIntentTitle(MoveState move)
    {
        var primary = move.Intents.FirstOrDefault();
        if (primary is null) return null;
        var prefix = GetIntentLocPrefix(primary.IntentType);
        if (prefix is null) return null;
        var table = LocManager.Instance.GetTable("intents");
        var key = $"{prefix}.title";
        return table.HasEntry(key) ? table.GetRawText(key) : null;
    }

    private static string GetIntentSummary(MoveState move)
    {
        if (move.Intents.Count == 0) return "None";
        var parts = move.Intents
            .Select(GetIntentDescription)
            .Where(static s => !string.IsNullOrEmpty(s))
            .Distinct()
            .ToArray();
        return parts.Length == 0 ? "None" : string.Join("\n", parts);
    }

    private static string GetIntentDescription(AbstractIntent intent)
    {
        var prefix = GetIntentLocPrefix(intent.IntentType);
        if (prefix is null) return string.Empty;
        var table = LocManager.Instance.GetTable("intents");
        var key = $"{prefix}.description";
        if (!table.HasEntry(key)) return intent.IntentType.ToString();
        var template = table.GetRawText(key);

        var damage = 0m;
        var repeat = 1;
        var cardCount = 0;
        if (intent is AttackIntent attack)
        {
            try { damage = attack.DamageCalc?.Invoke() ?? 0m; } catch { }
            repeat = attack.Repeats;
        }
        if (intent is StatusIntent status) cardCount = status.CardCount;

        template = Regex.Replace(template, @"\{Repeat:choose\((\d+)\)(?:[:|]+)((?:[^{}]|\{[^}]*\})*)\}",
            m => int.TryParse(m.Groups[1].Value, out var t) && repeat == t ? "" : m.Groups[2].Value.Replace("{}", repeat.ToString()).TrimStart('|'));
        template = Regex.Replace(template, @"\{Repeat:plural:([^|]*)\|([^}]*)\}", m => repeat == 1 ? m.Groups[1].Value : m.Groups[2].Value);
        template = Regex.Replace(template, @"\{CardCount:plural:([^|]*)\|([^}]*)\}", m => cardCount == 1 ? m.Groups[1].Value : m.Groups[2].Value);
        template = Regex.Replace(template, @"\{IsMultiplayer:[^}]*\|([^}]*)\}", "$1");
        template = template.Replace("{Damage}", damage.ToString()).Replace("{Repeat}", repeat.ToString()).Replace("{CardCount}", cardCount.ToString());
        return template;
    }

    private static string? GetIntentLocPrefix(IntentType intentType)
    {
        return intentType switch
        {
            IntentType.Attack => "ATTACK",
            IntentType.Buff => "BUFF",
            IntentType.Debuff => "DEBUFF",
            IntentType.DebuffStrong => "DEBUFF_STRONG",
            IntentType.Defend => "DEFEND",
            IntentType.Escape => "ESCAPE",
            IntentType.Heal => "HEAL",
            IntentType.Summon => "SUMMON",
            IntentType.Sleep => "SLEEP",
            IntentType.Stun => "STUN",
            IntentType.StatusCard => "STATUS",
            IntentType.CardDebuff => "CARD_DEBUFF",
            IntentType.DeathBlow => "DEATH_BLOW",
            IntentType.Unknown or IntentType.Hidden => "UNKNOWN",
            _ => null
        };
    }

    private static string ShortStateId(string stateId)
    {
        const int maxLength = 12;
        return stateId.Length <= maxLength ? stateId : stateId[..maxLength];
    }

    internal static IReadOnlyList<MoveState> GetChoosableMoves(MonsterModel monster)
    {
        var moveStateMachine = monster.MoveStateMachine;
        if (moveStateMachine is null) return Array.Empty<MoveState>();

        return moveStateMachine.States.Values
            .OfType<MoveState>()
            .Where(static m => m.IsMove && m.ShouldAppearInLogs)
            .GroupBy(static m => m.StateId)
            .Select(static g => g.First())
            .ToArray();
    }
}
