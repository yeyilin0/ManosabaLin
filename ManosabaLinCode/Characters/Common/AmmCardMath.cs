namespace ManosabaLin.Characters.Common;

internal static class AmmCardMath
{
    internal const string SuspectBonusKey = "SuspectBonus";
    internal const string WitchificationBonusKey = "WitchificationBonus";

    internal static IEnumerable<DynamicVar> CreateVars()
    {
        yield return new CalculationBaseVar(8m);
        yield return new ExtraDamageVar(1m);
        yield return new CalculatedDamageVar(DamageProps.card)
            .WithMultiplier(static (card, _) => GetDamageBonus(card));
        yield return new LiveCardValueVar(SuspectBonusKey, static card => GetSuspectBonus(card));
        yield return new LiveCardValueVar(WitchificationBonusKey, static card => GetWitchificationBonus(card));
        yield return new PowerVar<AmmPower>(1m);
        yield return new PowerVar<SuspectPower>(1m);
        yield return new PowerVar<WithPower>(10m);
    }

    internal static decimal GetDamageBonus(CardModel card)
    {
        return GetSuspectBonus(card) + GetWitchificationBonus(card);
    }

    private static decimal GetSuspectBonus(CardModel card)
    {
        return Math.Max(0m, card.Owner?.Creature.GetPower<SuspectPower>()?.Amount ?? 0m);
    }

    private static decimal GetWitchificationBonus(CardModel card)
    {
        var amount = Math.Max(0m, card.Owner?.Creature.GetPower<WithPower>()?.Amount ?? 0m);
        return Math.Floor(amount / 20m);
    }

    private sealed class LiveCardValueVar(
        string name,
        Func<CardModel, decimal> valueFactory)
        : DynamicVar(name, 0m)
    {
        public override void UpdateCardPreview(
            CardModel card,
            CardPreviewMode previewMode,
            Creature? target,
            bool runGlobalHooks)
        {
            var value = valueFactory(card);
            PreviewValue = value;
            EnchantedValue = value;
        }
    }
}
