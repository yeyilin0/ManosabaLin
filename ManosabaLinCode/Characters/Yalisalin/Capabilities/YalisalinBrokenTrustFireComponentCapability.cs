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

        // 文案（2026-10-18 用户改 zhs）：「这张牌的余火连接抽牌堆中的攻击牌」——
        // 原为「先连接抽牌堆或弃牌堆中的攻击牌」，去掉弃牌堆。
        var attacks = PileType.Draw.GetPile(context.Owner).Cards
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
