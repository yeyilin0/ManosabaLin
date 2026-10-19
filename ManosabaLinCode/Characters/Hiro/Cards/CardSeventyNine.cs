using MinionLib.Component.Core;
﻿using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.HiroKeywords;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Models.Capabilities;
using ManosabaLin.Characters.Hiro.Capabilities;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HirolinCardPool))]
public sealed class CardSeventyNine() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    private const string RemoveCountKey = "RemoveCount";
    private const string GrantCountKey = "GrantCount";

    // 固定基础值：失去 1，获得 1
    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new IntVar(RemoveCountKey, 1),
        new IntVar(GrantCountKey, 1)
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new[] { TransmigrationRules.TransmigrationCardKeyword };

    /// <summary>
    ///     打出条件：**当前手牌里有带【轮回】的卡**（＝此刻有卡可以「失去轮回」）才允许打出。
    ///     <para>
    ///         ⭐ **不排除自己** —— 这张卡自身也带【轮回】（见 <see cref="CanonicalKeywords" />），
    ///         所以只要它在手牌里，条件就成立。
    ///     </para>
    ///     <para>
    ///         ⚠️ 非战斗场合（卡库 / 牌组预览）直接放行：<c>PileType.GetPile</c> 在战斗外会抛
    ///         <c>InvalidOperationException</c>（"Tried to get Hand pile while out of combat."）。
    ///     </para>
    /// </summary>
    protected override bool IsPlayableC
    {
        get
        {
            if (!base.IsPlayableC) return false;
            if (Owner is not { } player) return base.IsPlayableC;
            if (player.Creature?.CombatState is null) return base.IsPlayableC;

            return PileType.Hand.GetPile(player).Cards
                .Any(c => c.HasModKeyword(TransmigrationRules.TransmigrationCardKeyword));
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        var handPile = PileType.Hand.GetPile(source.Owner);
        if (handPile.Cards.Count == 0) return;

        var removeCount = source.DynamicVars[RemoveCountKey].IntValue;
        var grantCount = source.DynamicVars[GrantCountKey].IntValue;
        var rebirthId = TransmigrationRules.TransmigrationCardKeyword;

        // 1. 第 1 次选择：选择要失去「轮回」的牌
        //    ⚠️ 打出条件（IsPlayableC）不排除自己，所以这里要先看有没有可失去的候选（`c != this`），
        //       没有就整段跳过 —— 免得弹出空的选择屏。
        var removableCandidates = handPile.Cards.Any(c => c != this && c.HasModKeyword(rebirthId));
        if (removableCandidates)
        {
            var cardsToRemove = (await CardSelectCmd.FromHand(
                choiceContext,
                Owner,
                new CardSelectorPrefs(source.SelectionScreenPrompt, removeCount),
                c => c != this && c.HasModKeyword(rebirthId),
                this)).ToList();

            foreach (var card in cardsToRemove)
            {
                card.RemoveModKeyword(rebirthId);
                RefreshCardVisuals(card);
            }
        }

        // 2. 「获得轮回」是**逐张选择**：第 2 次选择选 1 张、第 3 次再选 1 张（连第 1 步共 **三次选择**），
        //    不是一次选中 2 张。每选出一张就立刻给它加上【轮回】（并刷新卡面）。
        //    ⭐ 升级后的隐藏配对（只作用于**获得轮回**的卡；第 1 步那张完全不参与、不挂任何东西）：
        //       · 第 2 次选择那张 → 加关键词 + 挂隐藏能力；它同时是这一对的**卡名来源**，本身**不改名**。
        //       · 第 3 次选择那张 → 加关键词 + 挂同一个隐藏能力 + **改名**（改成第 2 次那张的卡名）。
        string? pairName = null;

        for (var grantIndex = 0; grantIndex < grantCount; grantIndex++)
        {
            if (!handPile.Cards.Any(c => c != this && !c.HasModKeyword(rebirthId))) break;

            var picked = (await CardSelectCmd.FromHand(
                choiceContext,
                Owner,
                new CardSelectorPrefs(source.SelectionScreenPrompt, 1),
                c => c != this && !c.HasModKeyword(rebirthId),
                this)).ToList();

            if (picked.Count == 0) break;

            foreach (var card in picked)
            {
                card.AddModKeyword(rebirthId);

                if (IsUpgraded)
                {
                    if (grantIndex == 0)
                    {
                        // 第 2 次选择：这一对的**卡名来源**（自己不改名），只挂隐藏配对能力。
                        pairName = CardRename.EffectiveName(card);
                        if (!string.IsNullOrEmpty(pairName))
                            AttachPairCapability(card, pairName);
                    }
                    else if (!string.IsNullOrEmpty(pairName))
                    {
                        // 第 3 次选择：挂同一个隐藏配对能力 + 改名成第 2 次那张的卡名（CardRename，纯视觉）。
                        AttachPairCapability(card, pairName);
                        CardRename.Set(card, pairName);
                    }
                }

                RefreshCardVisuals(card);
            }
        }
    }

    /// <summary>
    ///     给一张手牌挂上隐藏的「轮回配对」能力（已存在则只往里追加配对名）。
    /// </summary>
    private static void AttachPairCapability(CardModel card, string pairName)
    {
        card.GetOrCreateCapability<TransmigrationPairCapability>().AddPairName(pairName);
    }

    private static void RefreshCardVisuals(CardModel card)
    {
        var node = NCard.FindOnTable(card);
        if (node != null) node.UpdateVisuals(card.Pile?.Type ?? PileType.Hand, CardPreviewMode.Normal);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        // 只有获得轮回的牌数 +1（1 → 2），失去轮回保持 1
        DynamicVars[GrantCountKey].UpgradeValueBy(1);
    }
}
