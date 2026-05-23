using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Emalin;
using ManosabaLin.Characters.Emalin.Enchantments;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using ManosabaLin.Characters.Emalin.Components;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class FinalJudgment : ManosabaCardTemplate
{
    public FinalJudgment() : base(3, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
   
            yield return Hatedperson.HoverTip;
        }
    }


    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var owner = Owner;
        var creature = owner.Creature;
        var rng = owner.RunState.Rng.CombatTargets;

        // ===== 读取所有状态 =====
        var bond = creature.GetPower<BondPower>();
        var affinity = bond?.Affinity ?? 0;
        var estrangement = bond?.Estrangement ?? 0;
        var bondTotal = affinity + estrangement;

        // 数遍审判附魔
        var allPiles = new[] { PileType.Draw, PileType.Hand, PileType.Discard };
        var trialCards = new List<(CardModel card, PileType pile)>();
        foreach (var pileType in allPiles)
        {
            var pile = pileType.GetPile(owner);
            foreach (var card in pile.Cards)
            {
                if (card.Enchantment is Rebuttal or Agreement or Doubt)
                    trialCards.Add((card, pileType));
            }
        }
        var trialCount = trialCards.Count;

        // 读取魔女化
        var withPower = creature.GetPower<WithPower>();
        var witchAmount = (int)(withPower?.Amount ?? 0);

        // 读取嫌疑
        var enemies = CombatState.Enemies.Where(e => e.IsAlive).ToList();
        var totalSuspect = 0;
        foreach (var enemy in enemies)
        {
            var suspect = enemy.GetPower<SuspectPower>();
            if (suspect != null && suspect.Amount > 0)
                totalSuspect += (int)suspect.Amount;
        }

        await CreatureCmd.TriggerAnim(creature, "Cast", owner.Character.CastAnimDelay);

        // ===== 第一幕：羁绊 =====
        // 羁绊伤害 + 审判联动
        var bondDamage = (bondTotal + trialCount) * 2;
        if (bondDamage > 0)
        {
            foreach (var enemy in enemies)
                await CreatureCmd.Damage(choiceContext, enemy, bondDamage,
                    ValueProp.Unpowered | ValueProp.Move, creature, this);
        }

        if (affinity > estrangement && affinity > 0)
        {
            foreach (var ally in CombatState.Allies.Where(a => a.IsAlive))
                await CreatureCmd.GainBlock(ally, affinity * 3, ValueProp.Move, cardPlay);
        }
        else if (estrangement > affinity && estrangement > 0)
        {
            foreach (var enemy in enemies)
                await PowerCmd.Apply<VulnerablePower>(choiceContext, enemy, estrangement, creature, this, false);
        }

        if (bond != null)
        {
            bond.Affinity = 0;
            bond.Estrangement = 0;
        }

        // ===== 第二幕：审判 =====
        // 触发每张附魔卡的×1效果
        foreach (var (card, _) in trialCards)
        {
            if (card.Enchantment is Rebuttal)
            {
                if (enemies.Count > 0)
                {
                    var target = rng.NextItem(enemies);
                    await CreatureCmd.Damage(choiceContext, target, 1m,
                        ValueProp.Unpowered, creature, null);
                }
            }
            else if (card.Enchantment is Agreement)
            {
                foreach (var ally in CombatState.Allies.Where(a => a.IsAlive))
                    await CreatureCmd.GainBlock(ally, 3m, ValueProp.Move, cardPlay);
            }
            else if (card.Enchantment is Doubt)
            {
                await CreatureCmd.GainBlock(creature, 1m, ValueProp.Move, cardPlay);
            }
        }

        // 抽牌: N + 魔女化联动
        var drawCount = trialCount + witchAmount / 50;
        if (drawCount > 0)
            await CardPileCmd.Draw(choiceContext, drawCount, owner);

        // 移除所有审判附魔
        foreach (var (card, pileType) in trialCards)
        {
            var template = card.CanonicalInstance;
            var upgradeLevel = card.CurrentUpgradeLevel;
            await CardPileCmd.RemoveFromCombat(card);
            var newCard = CombatState.CreateCard(template, owner);
            for (int i = 0; i < upgradeLevel; i++)
                CardCmd.Upgrade(newCard);
            await CardPileCmd.AddGeneratedCardToCombat(newCard, pileType, owner);
        }

        // ===== 第三幕：魔女化 =====
        var baseEnergy = witchAmount / 50;
        var bonusEnergy = (witchAmount / 100) * 2;
        var totalEnergy = baseEnergy + bonusEnergy;

        // 嫌疑联动: ×(1 + totalSuspect × 5%)
        var suspectMultiplier = 1m + totalSuspect * 0.05m;
        totalEnergy = (int)(totalEnergy * suspectMultiplier);

        if (totalEnergy > 0)
            await PlayerCmd.GainEnergy(totalEnergy, owner);

        if (withPower != null)
            await PowerCmd.Remove(withPower);

        // ===== 第四幕：嫌疑 =====
        foreach (var enemy in enemies)
        {
            var suspect = enemy.GetPower<SuspectPower>();
            if (suspect == null || suspect.Amount <= 0) continue;

            // 嫌疑伤害 + 羁绊联动
            var suspectDamage = (int)suspect.Amount * 3 + bondTotal;
            if (suspectDamage > 0)
                await CreatureCmd.Damage(choiceContext, enemy, suspectDamage,
                    ValueProp.Unpowered | ValueProp.Move, creature, this);

            await PowerCmd.Remove(suspect);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
