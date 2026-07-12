using ManosabaLin.Characters.Ananlin.Relics;

namespace ManosabaLin.Characters.Ananlin.Powers;

[RegisterPower]
public sealed class AnanlinMoviePromisePower : ManosabaPowerTemplate
{
    private readonly List<PromiseEntry> _promises = [];

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    internal void AddPromise(CardModel card)
    {
        _promises.Add(new PromiseEntry(card.CanonicalInstance, card.Pool, card.CurrentUpgradeLevel));
        Amount = _promises.Count;
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner) return;

        if (_promises.Count == 0)
        {
            await PowerCmd.Remove(this);
            return;
        }

        var promises = _promises.ToArray();
        _promises.Clear();

        foreach (var promise in promises)
        {
            var returned = CombatState.CreateCard(promise.CanonicalCard, player);
            CopyUpgradeLevel(promise.UpgradeLevel, returned);
            returned.SetToFreeThisTurn();

            await CardPileCmd.AddGeneratedCardToCombat(returned, PileType.Hand, player);

            var extra = RollExtraFromPool(player, promise.Pool);
            if (extra is null) continue;

            extra.SetToFreeThisTurn();
            extra.ExhaustOnNextPlay = true;
            extra.AddKeyword(CardKeyword.Ethereal);

            await CardPileCmd.AddGeneratedCardToCombat(extra, PileType.Hand, player);
        }

        await PowerCmd.Remove(this);
    }

    private CardModel? RollExtraFromPool(Player player, CardPoolModel pool)
    {
        var candidates = pool
            .GetUnlockedCards(player.UnlockState, player.RunState.CardMultiplayerConstraint)
            .Where(AnansSketchbook.CanSketchbookGenerate)
            .ToArray();
        if (candidates.Length == 0) return null;

        var canonical = player.RunState.Rng.CombatCardGeneration.NextItem(candidates);
        return canonical is null ? null : CombatState.CreateCard(canonical, player);
    }

    private static void CopyUpgradeLevel(int upgradeLevel, CardModel card)
    {
        for (var i = 0; i < upgradeLevel; i++)
            CardCmd.Upgrade(card);
    }

    private sealed record PromiseEntry(CardModel CanonicalCard, CardPoolModel Pool, int UpgradeLevel);
}
