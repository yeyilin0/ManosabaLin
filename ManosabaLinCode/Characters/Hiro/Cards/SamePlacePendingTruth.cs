using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.HiroKeywords;
using ManosabaLin.Characters.Hiro.Capabilities;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Component.Core;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models.Capabilities;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class SamePlacePendingTruth() : ManosabaCardTemplate(0, CardType.Skill, CardRarity.Ancient, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        if (Owner is not { } player) return;

        // 本场战斗移除所有【旧识疑影】
        foreach (var truth in player.PlayerCombatState.AllCards.OfType<SamePlaceTruth>().ToArray())
        {
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
        await PowerCmd.Apply<TruthPower>(choiceContext, player.Creature, 1m, player.Creature, this, false);
    }
}
