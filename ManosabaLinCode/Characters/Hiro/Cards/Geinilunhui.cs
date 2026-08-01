using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.HiroKeywords;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class Geinilunhui : ManosabaCardTemplate
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    private const string RemoveCountKey = "RemoveCount";
    private const string GrantCountKey = "GrantCount";

    public Geinilunhui() : base(3, CardType.Skill, CardRarity.Rare, TargetType.AnyAlly)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new IntVar(RemoveCountKey, 1),
        new IntVar(GrantCountKey, 1)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        var rebirthKeyword = TransmigrationRules.TransmigrationCardKeyword;
        var targetPlayer = cardPlay.Target?.Player ?? Owner;

        // 1. 从自己的手牌、抽牌堆、弃牌堆中选 1 张有轮回的牌
        var myRebirthCards = new[] { PileType.Hand, PileType.Draw, PileType.Discard }
            .SelectMany(p => p.GetPile(Owner).Cards)
            .Where(c => c.HasModKeyword(rebirthKeyword))
            .ToList();
        if (myRebirthCards.Count == 0) return;

        var removeCount = source.DynamicVars[RemoveCountKey].IntValue;
        var cardsToRemove = await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            myRebirthCards,
            Owner,
            new CardSelectorPrefs(source.SelectionScreenPrompt, removeCount, removeCount));

        // 2. 自动选择至多 2 张同名卡
        var removedEntryIds = cardsToRemove.Select(c => c.Id.Entry).ToHashSet();
        var autoMatchingCards = GetAllCards(Owner)
            .Where(c => removedEntryIds.Contains(c.Id.Entry) && !cardsToRemove.Contains(c))
            .Take(2)
            .ToList();

        // 3. 移除轮回
        var allToRemove = cardsToRemove.Concat(autoMatchingCards).Distinct().ToList();
        int totalRemoved = 0;

        foreach (var card in allToRemove)
        {
            card.RemoveModKeyword(rebirthKeyword);
            RefreshCardVisuals(card);
            totalRemoved++;
        }

        // 4. 目标队友抽牌堆随机 1 张获得轮回
        var targetDrawPile = PileType.Draw.GetPile(targetPlayer).Cards;
        if (targetDrawPile.Count > 0)
        {
            var rng = Owner.RunState.Rng.CombatTargets;
            var targetCard = targetDrawPile[rng.NextInt(targetDrawPile.Count)];
            targetCard.AddModKeyword(rebirthKeyword);
            RefreshCardVisuals(targetCard);

            // 5. 每移除 1 次生成 1 张复制品加入队友抽牌堆，至多 2 张
            var copiesToGenerate = Math.Min(totalRemoved, 2);
            for (int i = 0; i < copiesToGenerate; i++)
            {
                var copy = CombatState.CreateCard(targetCard.CanonicalInstance, targetPlayer);
                copy.AddModKeyword(rebirthKeyword);
                await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Draw, targetPlayer);
            }
        }
    }

    private static List<CardModel> GetAllCards(Player player)
    {
        return new[] { PileType.Hand, PileType.Draw, PileType.Discard }
            .SelectMany(pile => pile.GetPile(player).Cards)
            .ToList();
    }

    private static void RefreshCardVisuals(CardModel card)
    {
        var node = NCard.FindOnTable(card);
        if (node != null) node.UpdateVisuals(card.Pile?.Type ?? PileType.Hand, CardPreviewMode.Normal);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
