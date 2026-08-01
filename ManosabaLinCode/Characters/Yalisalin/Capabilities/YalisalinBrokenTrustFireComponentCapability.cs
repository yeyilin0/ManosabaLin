using ManosabaLin.Characters.Yalisalin.Components;
using STS2RitsuLib.Models.Capabilities;

namespace ManosabaLin.Characters.Yalisalin.Capabilities;

[RegisterModelCapability]
public sealed class YalisalinBrokenTrustFireComponentCapability : ManosabaCardCapability,
    IYalisalinFireComponentModifier
{
    public void ModifyFireComponentConnectionPool(YalisalinFireComponentContext context)
    {
        if (context.SourceCard != Owner)
            return;

        var attacks = new[] { PileType.Draw, PileType.Discard }
            .SelectMany(pile => pile.GetPile(context.Owner).Cards)
            .Where(card => card != context.SourceCard)
            .Where(card => card.Type == CardType.Attack)
            .ToArray();

        if (attacks.Length > 0)
            context.ReplaceConnectionPool(attacks);
    }

    public async Task AfterFireComponentResolved(
        PlayerChoiceContext choiceContext,
        YalisalinFireComponentContext context)
    {
        if (context.SourceCard == Owner && context.ChosenCard?.Type == CardType.Attack)
            await PlayerCmd.GainEnergy(1, context.Owner);
    }
}
