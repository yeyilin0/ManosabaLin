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

[RegisterPower]
public sealed class EnchantmentConvergencePower : ManosabaPowerTemplate
{
    private ModelId? _chosenEnchantmentId;
    private int _lastSharedCount;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature != Owner) return;

        var enchantment = cardPlay.Card.Enchantment;
        if (enchantment is not (Agreement or Rebuttal or Doubt)) return;

        // 获取附魔计数
        var count = GetEnchantmentCount(Owner.Player, enchantment);
        if (count < 5) return;

        // 检查是否应该共享
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

        // 给每个队友复制完整效果
        foreach (var ally in OtherAllyPlayers())
        {
            if (enchantment is Agreement)
                await ExecuteAgreement(choiceContext, cardPlay, ally, count);
            else if (enchantment is Rebuttal)
                await ExecuteRebuttal(choiceContext, cardPlay, ally, count);
            else if (enchantment is Doubt)
                await ExecuteDoubt(choiceContext, ally, cardPlay.Card, count);
        }
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

    public IEnumerable<Player> OtherAllyPlayers()
    {
        return Owner.CombatState.Players
            .Where(p => p != Owner.Player && p.Creature.Side == Owner.Side && p.Creature.IsAlive);
    }

    // ==================== 赞同 ====================
    private async Task ExecuteAgreement(PlayerChoiceContext choiceContext, CardPlay? cardPlay, Player ally, int count)
    {
        var combatState = Owner.CombatState;
        var allyCreature = ally.Creature;

        foreach (var a in combatState.Allies.Where(a => a is { IsAlive: true }))
            await CreatureCmd.GainBlock(a, 3m, ValueProp.Move, cardPlay);

        if (count % 2 == 0)
            await CreatureCmd.GainBlock(allyCreature, 3m, ValueProp.Move, cardPlay);

        if (count % 3 == 0)
            foreach (var a in combatState.Allies.Where(a => a is { IsAlive: true }))
                await PowerCmd.Apply<TempDexterity>(choiceContext, a, 1m, allyCreature, null, false);

        if (count % 4 == 0)
            foreach (var a in combatState.Allies.Where(a => a is { IsAlive: true }))
                await PowerCmd.Apply<TempStrength>(choiceContext, a, 2m, allyCreature, null, false);

        if (count % 5 == 0)
        {
            var allyCards = PileType.Hand.GetPile(ally).Cards.Where(c => c.CanPlay()).ToList();
            if (allyCards.Count > 0)
                ally.RunState.Rng.CombatCardSelection.NextItem(allyCards).SetToFreeThisTurn();

            foreach (var p in combatState.Players)
                await PlayerCmd.GainEnergy(1m, p);
        }
    }

    // ==================== 反驳 ====================
    private async Task ExecuteRebuttal(PlayerChoiceContext choiceContext, CardPlay? cardPlay, Player ally, int count)
    {
        var combatState = Owner.CombatState;
        var allyCreature = ally.Creature;
        var rng = ally.RunState.Rng.CombatCardSelection;

        if (cardPlay?.Target is { IsAlive: true } target)
            await CreatureCmd.Damage(choiceContext, target, 1m, ValueProp.Unpowered, allyCreature, null, null);

        if (count % 2 == 0)
            await PowerCmd.Apply<StrengthPower>(choiceContext, allyCreature, 1m, allyCreature, null, false);

        if (count % 3 == 0)
            foreach (var enemy in combatState.Enemies.Where(e => e is { IsAlive: true }))
                await PowerCmd.Apply<VulnerablePower>(choiceContext, enemy, 1m, allyCreature, null, false);

        if (count % 4 == 0)
        {
            for (var i = 0; i < count; i++)
            {
                var enemies = combatState.Enemies.Where(e => e is { IsAlive: true }).ToList();
                if (enemies.Count == 0) break;
                await CreatureCmd.Damage(choiceContext, enemies[rng.NextInt(enemies.Count)], 1m,
                    ValueProp.Unpowered, allyCreature, null, null);
            }
        }
    }

    // ==================== 疑问 ====================
    private async Task ExecuteDoubt(PlayerChoiceContext choiceContext, Player ally, CardModel sourceCard, int count)
    {
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

    public override async Task BeforeSideTurnEnd(
        PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != Owner.Side) return;
        await PowerCmd.Remove(this);
    }
}
