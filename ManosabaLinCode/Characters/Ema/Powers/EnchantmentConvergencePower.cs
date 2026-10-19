using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.Powers;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Emalin.Enchantments;
using ManosabaLin.Characters.Ema.Relics;
using ManosabaLin.Characters.Hiro.Cards;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Enchantments;
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
///     审判开庭：由「审判开庭」牌打出时施加，层数恒为 1，<b>持续整场战斗</b>。
///     <para>
///         打出时选择 1 名友方（记录在 <see cref="ChosenAlly" />）。
///         此后每回合内，当你任一【审判】附魔（赞同 / 反驳 / 疑问）计数达到 5 时进行「锚定」，
///         锚定后只跟随该类附魔的计数增长；其额外效果会同步给所选友方。
///     </para>
///     <para>
///         「每回合内」= <b>持续整场 + 每回合重置锚定</b>：能力本身不会随回合结束消失，
///         只有锚定状态（<c>_chosenEnchantmentId</c> / <c>_lastSharedCount</c>）在每回合重新开始
///         （审判附魔计数本身由 <c>EmaTrialBadge</c> 按 Round 清零）。
///     </para>
///     <para>
///         <b>团队型效果不收窄</b>：全体友方 / 全体敌人 / 全体玩家 这类效果按卡面保持团队范围，
///         即使所选友方已阵亡也照常结算。
///     </para>
/// </summary>
[RegisterPower]
public sealed class EnchantmentConvergencePower : ManosabaPowerTemplate
{
    private ModelId? _chosenEnchantmentId;
    private int _lastSharedCount;

    /// <summary>打出「审判开庭」时选定的友方，由卡牌在 OnPlay 里写入。</summary>
    public Creature? ChosenAlly { get; set; }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player?.Creature != Owner) return;

        // 「每回合内」= 持续整场 + 每回合重置锚定：
        // 能力本身不随回合结束消失，只有锚定状态每回合重新开始
        // （审判附魔计数本身由 EmaTrialBadge 按 Round 清零）。
        _chosenEnchantmentId = null;
        _lastSharedCount = 0;

        await Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature != Owner) return;
        if (Owner.Player is not { } owner) return;

        var enchantment = cardPlay.Card.Enchantment;
        if (enchantment is not (Agreement or Rebuttal or Doubt)) return;

        // 获取附魔计数
        var count = GetEnchantmentCount(owner, enchantment);
        if (count < 5) return;

        // 锚定：本回合第一类达到 5 的审判附魔被锁定，之后只跟随该类继续增长。
        if (_chosenEnchantmentId == null)
        {
            _chosenEnchantmentId = enchantment.Id;
            _lastSharedCount = count;
        }
        else if (_chosenEnchantmentId != enchantment.Id || count <= _lastSharedCount)
        {
            return;
        }
        else
        {
            _lastSharedCount = count;
        }

        // 所选友方已阵亡 / 未记录时，只是没有可同步的个人收益；
        // 团队型效果（全体友方 / 全体敌人 / 全体玩家）不受影响，照常结算 —— 不收窄。
        Player? chosen = ChosenAlly is { IsAlive: true } ally ? ally.Player : null;

        if (chosen is not null) Flash();

        if (enchantment is Agreement)
            await ExecuteAgreement(choiceContext, cardPlay, chosen, count);
        else if (enchantment is Rebuttal)
            await ExecuteRebuttal(choiceContext, cardPlay, chosen, count);
        else if (enchantment is Doubt)
            await ExecuteDoubt(choiceContext, chosen, cardPlay.Card, count);
    }

    private static int GetEnchantmentCount(Player player, EnchantmentModel enchantment)
    {
        var badge = player.Relics.OfType<EmaTrialBadge>().FirstOrDefault();

        return badge is null
            ? 0
            : enchantment switch
            {
                Agreement => badge.AgreeCount,
                Rebuttal => badge.RebuttalCount,
                Doubt => badge.DoubtCount,
                _ => 0
            };
    }

    // ==================== 赞同 ====================
    private async Task ExecuteAgreement(PlayerChoiceContext choiceContext, CardPlay? cardPlay, Player? ally, int count)
    {
        var combatState = Owner.CombatState;
        if (combatState is null) return;

        var allies = combatState.Allies.Where(static a => a is { IsAlive: true }).ToList();
        var applier = ally?.Creature ?? Owner;

        // ×1：全体友方 3 护盾（团队型，保持团队范围）
        foreach (var a in allies)
            await CreatureCmd.GainBlock(a, 3m, ValueProp.Move, cardPlay);

        // ×2：自己 3 护盾 → 所选友方也能获得
        if (count % 2 == 0 && ally is not null)
            await CreatureCmd.GainBlock(ally.Creature, 3m, ValueProp.Move, cardPlay);

        // ×3：全体临时迅捷（团队型，保持团队范围）
        if (count % 3 == 0)
            foreach (var a in allies)
                await PowerCmd.Apply<TempDexterity>(choiceContext, a, 1m, applier, null, false);

        // ×4：全体临时力量（团队型，保持团队范围）
        if (count % 4 == 0)
            foreach (var a in allies)
                await PowerCmd.Apply<TempStrength>(choiceContext, a, 2m, applier, null, false);

        // ×5：随机 1 张手牌免费（自己 → 所选友方也能获得）+ 全体友方 1 能量（团队型）
        if (count % 5 == 0)
        {
            if (ally is not null)
            {
                // ⚠️ 不用 CanPlay()：能量是瞬时量，两端候选集可能不同 ⇒ RNG 抽出不同的牌 ⇒ 状态分歧。
                var allyCards = PileType.Hand.GetPile(ally).Cards
                    .Where(EmalinCombatHelper.IsEligibleForCostEffect)
                    .ToList();
                if (allyCards.Count > 0)
                    ally.RunState.Rng.CombatCardSelection.NextItem(allyCards).SetToFreeThisTurn();
            }

            foreach (var p in combatState.Players)
                await PlayerCmd.GainEnergy(1m, p);
        }
    }

    // ==================== 反驳 ====================
    private async Task ExecuteRebuttal(PlayerChoiceContext choiceContext, CardPlay? cardPlay, Player? ally, int count)
    {
        var combatState = Owner.CombatState;
        if (combatState is null) return;

        var applier = ally?.Creature ?? Owner;

        // ×1：对目标造成 1 点伤害 → 所选友方作为来源再结算一次
        if (ally is not null && cardPlay?.Target is { IsAlive: true } target)
            await CreatureCmd.Damage(choiceContext, target, 1m, ValueProp.Unpowered, ally.Creature, null, null);

        // ×2：自己获得 1 点力量 → 所选友方也能获得
        if (count % 2 == 0 && ally is not null)
            await PowerCmd.Apply<StrengthPower>(choiceContext, ally.Creature, 1m, ally.Creature, null, false);

        // ×3：敌方全体 1 层易伤（团队型，保持团队范围）
        if (count % 3 == 0)
            foreach (var enemy in combatState.Enemies.Where(e => e is { IsAlive: true }))
                await PowerCmd.Apply<VulnerablePower>(choiceContext, enemy, 1m, applier, null, false);

        // ×4：随机敌方「计数」次 1 点伤害 → 所选友方作为来源再结算一次
        if (count % 4 == 0 && ally is not null)
        {
            var rng = ally.RunState.Rng.CombatCardSelection;
            for (var i = 0; i < count; i++)
            {
                var enemies = combatState.Enemies.Where(e => e is { IsAlive: true }).ToList();
                if (enemies.Count == 0) break;
                await CreatureCmd.Damage(choiceContext, enemies[rng.NextInt(enemies.Count)], 1m,
                    ValueProp.Unpowered, ally.Creature, null, null);
            }
        }
    }

    // ==================== 疑问 ====================
    private async Task ExecuteDoubt(PlayerChoiceContext choiceContext, Player? ally, CardModel sourceCard, int count)
    {
        // 疑问各档全部是「自己获得」的个人收益，没有团队型分支 —— 没有所选友方就无事发生。
        if (ally is null) return;

        var allyCreature = ally.Creature;

        await CreatureCmd.GainBlock(allyCreature, 1m, ValueProp.Move, null);

        if (count % 2 == 0)
            await CardPileCmd.Draw(choiceContext, 1m, ally);

        if (count % 3 == 0)
            await PlayerCmd.GainEnergy(1m, ally);

        if (count % 4 == 0)
        {
            var discardCards = PileType.Discard.GetPile(ally).Cards
                .Where(static card => !SamePlaceTruth.IsSelectionLocked(card))
                .ToList();
            if (discardCards.Count > 0)
                await CardPileCmd.Add(ally.RunState.Rng.CombatCardSelection.NextItem(discardCards), PileType.Hand);
        }

        if (count % 5 == 0)
        {
            var handCards = PileType.Hand.GetPile(ally).Cards.Where(c => c != sourceCard).ToList();
            if (handCards.Count == 0) return;

            var rng = ally.RunState.Rng.CombatCardSelection;
            var replayCard = rng.NextItem(handCards);
            replayCard.BaseReplayCount++;
            CardCmd.Preview(replayCard);

            var enchantTargets = handCards
                .Where(c => c != replayCard && c.Enchantment == null
                    && c.Rarity != CardRarity.Status && c.Rarity != CardRarity.Curse && c.Rarity != CardRarity.Quest)
                .ToList();
            if (enchantTargets.Count > 0)
            {
                var options = new EnchantmentModel[]
                {
                    ModelDb.Enchantment<Rebuttal>().ToMutable(),
                    ModelDb.Enchantment<Agreement>().ToMutable(),
                    ModelDb.Enchantment<Doubt>().ToMutable()
                };
                CardCmd.Enchant(rng.NextItem(options), rng.NextItem(enchantTargets), 1m);
                CardCmd.Preview(enchantTargets[0]); // 预览被附魔的牌
            }
        }
    }
}
