using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace ManosabaLin.Characters.Common.HiroKeywords;

/// <summary>
///     Hiro关键词注册
/// </summary>
internal static class HiroKeywordRegistration
{
    [RegisterOwnedCardKeyword("hiro",
        CardDescriptionPlacement = ModKeywordCardDescriptionPlacement.BeforeCardDescription)]
    private sealed class HiroKeyword;
}

/// <summary>
///     Hiro关键词规则
/// </summary>
public static class HiroKeywordRules
{
    public static string HiroKeywordId =>
        ModContentRegistry.GetQualifiedKeywordId(MainFile.ModId, "hiro");

    public static bool HasHiroKeyword(CardModel? card)
    {
        return card != null && card.HasModKeyword(HiroKeywordId);
    }
}

[RegisterSingleton]
public sealed class HiroKeywordSingleton : SingletonModel
{
    public HiroKeywordSingleton()
    {
        ModHelper.SubscribeForCombatStateHooks(Id.Entry, CombatSubModels);
    }

    public override bool ShouldReceiveCombatHooks => true;

    private IEnumerable<AbstractModel> CombatSubModels(CombatState _)
    {
        return [this];
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        if (cardPlay.IsAutoPlay) return;
        if (!HiroKeywordRules.HasHiroKeyword(cardPlay.Card)) return;

        // 抽一张牌
        await CardPileCmd.Draw(context, 1m, cardPlay.Card.Owner);
    }
}