using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.HiroKeywords;
using ManosabaLin.Characters.Hiro.Capabilities;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MinionLib.Component.Core;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models.Capabilities;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(LinCardPool))]
public sealed class SamePlacePendingTruth() : ManosabaCardTemplate(-1, CardType.Skill, CardRarity.Ancient, TargetType.Self)
{
    private const string EffectHoverLocEntry = "MANOSABA_LIN_CARD_SAME_PLACE_PENDING_TRUTH_EFFECT";

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return CardEffectHoverTipFactory.FromCard(this, EffectHoverLocEntry);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        if (Owner is not { } player) return;

        await PlayPendingTruthRemovalEffect(choiceContext, player, this);
    }

    /// <summary>
    /// 本场战斗移除所有【旧识疑影】（包括当前打出强化效果的这张本体），
    /// 所有带【轮回】的卡获得【真相】，并获得【真相】能力。
    /// 由【旧识疑影】右键强化打出时额外调用。
    /// </summary>
    internal static async Task PlayPendingTruthRemovalEffect(PlayerChoiceContext choiceContext, Player player, CardModel source)
    {
        // 本场战斗移除所有【旧识疑影】
        foreach (var truth in player.PlayerCombatState.AllCards.OfType<SamePlaceTruth>().ToArray())
        {
            // 当前正在打出的那张不在这里移除：它的打出结果牌堆是 PileType.None，
            // 由引擎 OnPlayWrapper 收尾阶段（RemoveFromCombat(this, skipCardPileVisuals: false)）
            // 负责带视觉移除；若在这里用 skipVisuals 提前移除，卡牌节点会残留在打出位置
            // （屏幕上方）不被清理。
            if (truth.Pile?.Type == PileType.Play)
            {
                continue;
            }

            await CardPileCmd.RemoveFromCombat(truth, skipVisuals: true);
        }

        // 给予当前抽牌堆/弃牌堆/手牌/消耗牌堆中带【轮回】的卡真相组件
        var transmigrationCards = new[]
            {
                PileType.Draw,
                PileType.Discard,
                PileType.Hand,
                PileType.Exhaust
            }
            .SelectMany(pile => pile.GetPile(player).Cards)
            .Where(TransmigrationRules.HasTransmigration)
            .ToArray();

        foreach (var card in transmigrationCards)
        {
            card.GetOrCreateCapability<TruthComponentCapability>();
        }

        // 你获得真相能力
        await PowerCmd.Apply<TruthPower>(choiceContext, player.Creature, 1m, player.Creature, source, false);
    }
}
