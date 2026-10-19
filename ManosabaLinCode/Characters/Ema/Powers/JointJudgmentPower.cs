using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.Powers;
using ManosabaLin.Characters.Hiro.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Ema.Powers;

/// <summary>
///     共同审判 - 施加在艾玛自己身上的计数器。
///     <para>
///         场上<b>所有玩家</b>每累计打出 5 张牌，就让<b>每个玩家各自</b>随机生效一次
///         【审判】附魔的额外效果（赞同 / 疑问 / 反驳 各 5 档，共 15 种里随机 1 种）。
///     </para>
///     <para>
///         与附魔自身的「计数取模」不同，这里抽中的是「第几档」，所以每档只结算它自己那一条，
///         不再叠加低档效果。反驳 ×5「本牌额外多打出 1 次」在当前没有具体牌可指，
///         改为「随机 1 张手牌获得 1 层重放」。
///     </para>
/// </summary>
[RegisterPower]
public sealed class JointJudgmentPower : ManosabaPowerTemplate
{
    /// <summary>每累计多少张牌触发一轮。</summary>
    private const int PlaysPerTrigger = 5;

    /// <summary>15 种额外效果的总数（3 类附魔 × 5 档）。</summary>
    private const int TotalEffects = 15;

    private int _playsInCycle;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 自动打出（含本能力自己引发的回响）不计数，避免递归
        if (cardPlay.IsAutoPlay) return;
        if (cardPlay.Card.Owner is null) return;

        _playsInCycle++;
        if (_playsInCycle < PlaysPerTrigger) return;

        _playsInCycle = 0;

        var combatState = Owner.CombatState;
        if (combatState is null) return;

        Flash();

        foreach (var player in combatState.Players.Where(static p => p.Creature.IsAlive).ToList())
            await TriggerRandomJudgmentEffect(choiceContext, player);
    }

    private async Task TriggerRandomJudgmentEffect(PlayerChoiceContext choiceContext, Player player)
    {
        var runState = Owner.Player?.RunState;
        if (runState is null) return;

        var rng = runState.Rng.CombatCardSelection;
        var index = rng.NextInt(TotalEffects);

        // 0..4 = 赞同 ×1..×5 ；5..9 = 疑问 ×1..×5 ；10..14 = 反驳 ×1..×5
        var family = index / 5;
        var tier = index % 5 + 1;

        switch (family)
        {
            case 0:
                await ExecuteAgreementEffect(choiceContext, player, tier);
                break;
            case 1:
                await ExecuteDoubtEffect(choiceContext, player, tier);
                break;
            default:
                await ExecuteRebuttalEffect(choiceContext, player, tier);
                break;
        }
    }

    // ==================== 赞同（Agreement）====================
    private async Task ExecuteAgreementEffect(PlayerChoiceContext choiceContext, Player player, int tier)
    {
        var combatState = Owner.CombatState;
        if (combatState is null) return;

        var allies = combatState.Allies.Where(static a => a is { IsAlive: true }).ToList();

        switch (tier)
        {
            case 1: // ×1：全体友方 3 护盾
                foreach (var ally in allies)
                    await CreatureCmd.GainBlock(ally, 3m, ValueProp.Move, null);
                break;

            case 2: // ×2：自身 3 护盾
                await CreatureCmd.GainBlock(player.Creature, 3m, ValueProp.Move, null);
                break;

            case 3: // ×3：全体友方 1 层临时迅捷
                foreach (var ally in allies)
                    await PowerCmd.Apply<TempDexterity>(choiceContext, ally, 1m, player.Creature, null, false);
                break;

            case 4: // ×4：全体友方 2 层临时力量
                foreach (var ally in allies)
                    await PowerCmd.Apply<TempStrength>(choiceContext, ally, 2m, player.Creature, null, false);
                break;

            case 5: // ×5：随机 1 张手牌本回合免费 + 全体友方 1 能量
                // ⚠️ 不用 CanPlay()：能量是瞬时量，两端候选集可能不同 ⇒ RNG 抽出不同的牌 ⇒ 状态分歧。
                var freeTargets = PileType.Hand.GetPile(player).Cards
                    .Where(EmalinCombatHelper.IsEligibleForCostEffect)
                    .ToList();
                if (freeTargets.Count > 0)
                    Owner.Player!.RunState.Rng.CombatCardSelection.NextItem(freeTargets)?.SetToFreeThisTurn();

                foreach (var p in combatState.Players)
                    await PlayerCmd.GainEnergy(1m, p);
                break;
        }
    }

    // ==================== 疑问（Doubt）====================
    private async Task ExecuteDoubtEffect(PlayerChoiceContext choiceContext, Player player, int tier)
    {
        switch (tier)
        {
            case 1: // ×1：自身 1 护盾
                await CreatureCmd.GainBlock(player.Creature, 1m, ValueProp.Move, null);
                break;

            case 2: // ×2：抽 1 张
                await CardPileCmd.Draw(choiceContext, 1m, player);
                break;

            case 3: // ×3：自身 +1 能量
                await PlayerCmd.GainEnergy(1m, player);
                break;

            case 4: // ×4：从弃牌堆随机回收 1 张
                var discardCards = PileType.Discard.GetPile(player).Cards
                    .Where(static c => !SamePlaceTruth.IsSelectionLocked(c))
                    .ToList();
                if (discardCards.Count > 0)
                {
                    var retrieved = Owner.Player!.RunState.Rng.CombatCardSelection.NextItem(discardCards);
                    if (retrieved is not null)
                        await CardPileCmd.Add(retrieved, PileType.Hand);
                }

                break;

            case 5: // ×5：1 张手牌获得重放 1，另 1 张手牌获得随机【审判】附魔
                var handCards = PileType.Hand.GetPile(player).Cards.ToList();
                if (handCards.Count == 0) break;

                var rng = Owner.Player!.RunState.Rng.CombatCardSelection;

                var replayCard = rng.NextItem(handCards);
                if (replayCard is not null)
                {
                    replayCard.BaseReplayCount++;
                    CardCmd.Preview(replayCard);
                }

                var enchantTargets = handCards
                    .Where(c => c != replayCard
                                && c.Enchantment == null
                                && c.Rarity != CardRarity.Status
                                && c.Rarity != CardRarity.Curse
                                && c.Rarity != CardRarity.Quest
                                && c.Rarity != CardRarity.Ancient)
                    .ToList();
                if (enchantTargets.Count > 0)
                {
                    var enchantCard = rng.NextItem(enchantTargets);
                    var options = new EnchantmentModel[]
                    {
                        ModelDb.Enchantment<Rebuttal>().ToMutable(),
                        ModelDb.Enchantment<Agreement>().ToMutable(),
                        ModelDb.Enchantment<Doubt>().ToMutable()
                    };
                    if (enchantCard is not null)
                    {
                        CardCmd.Enchant(rng.NextItem(options), enchantCard, 1m);
                        CardCmd.Preview(enchantCard);
                    }
                }

                break;
        }
    }

    // ==================== 反驳（Rebuttal）====================
    private async Task ExecuteRebuttalEffect(PlayerChoiceContext choiceContext, Player player, int tier)
    {
        var combatState = Owner.CombatState;
        if (combatState is null) return;

        var rng = Owner.Player!.RunState.Rng.CombatCardSelection;
        var enemies = combatState.Enemies.Where(static e => e is { IsAlive: true }).ToList();

        switch (tier)
        {
            case 1: // ×1：随机造成 1 点伤害
                if (enemies.Count > 0)
                {
                    var target = rng.NextItem(enemies);
                    if (target is not null)
                        await CreatureCmd.Damage(choiceContext, target, 1m, ValueProp.Unpowered, player.Creature, null, null);
                }

                break;

            case 2: // ×2：自身获得 1 点力量
                await PowerCmd.Apply<StrengthPower>(choiceContext, player.Creature, 1m, player.Creature, null, false);
                break;

            case 3: // ×3：敌方全体 1 层易伤
                foreach (var enemy in enemies)
                    await PowerCmd.Apply<VulnerablePower>(choiceContext, enemy, 1m, player.Creature, null, false);
                break;

            case 4: // ×4：随机造成「本档档位」次数的 1 点伤害
                for (var i = 0; i < tier; i++)
                {
                    var live = combatState.Enemies.Where(static e => e is { IsAlive: true }).ToList();
                    if (live.Count == 0) break;

                    var target = rng.NextItem(live);
                    if (target is null) break;

                    await CreatureCmd.Damage(choiceContext, target, 1m, ValueProp.Unpowered, player.Creature, null, null);
                }

                break;

            case 5: // ×5：原效果是「本牌额外多打出 1 次」，此处无具体牌 → 随机 1 张手牌获得 1 层重放
                var handCards = PileType.Hand.GetPile(player).Cards.ToList();
                if (handCards.Count == 0) break;

                var replayCard = rng.NextItem(handCards);
                if (replayCard is not null)
                {
                    replayCard.BaseReplayCount++;
                    CardCmd.Preview(replayCard);
                }

                break;
        }
    }
}
