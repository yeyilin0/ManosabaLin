using MinionLib.Component.Core;
﻿using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.Characters.Common.HiroKeywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class JusticeEnforcer() : ManosabaCardTemplate(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<JusticePower>();
            yield return HoverTipFactory.FromPower<PerjuryPower>();
            yield return HoverTipFactory.FromPower<SuspectPower>();
            yield return HoverTipFactory.FromPower<WithPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var owner = source.Owner;

        var perjury = owner.Creature.GetPower<PerjuryPower>();
        var suspect = owner.Creature.GetPower<SuspectPower>();
        var justice = owner.Creature.GetPower<JusticePower>();
        var with = owner.Creature.GetPower<WithPower>();

        var perjuryAmt = perjury?.Amount ?? 0;
        var suspectAmt = suspect?.Amount ?? 0;
        var justiceAmt = justice?.Amount ?? 0;
        var withAmt = with?.Amount ?? 0;

        // ===== 1. 轮回 =====
        var handCards = PileType.Hand.GetPile(owner).Cards.Where(c => c != source).ToList();
        var rebirthCards = handCards.Where(c => c.HasModKeyword(TransmigrationRules.TransmigrationKeywordId)).ToList();

        if (rebirthCards.Count > 0)
        {
            var pool = owner.Character.CardPool.GetUnlockedCards(owner.UnlockState, owner.RunState.CardMultiplayerConstraint)
                .Where(c => c.Type != CardType.Status && c.Type != CardType.Curse)
                .ToList();

            foreach (var card in rebirthCards)
            {
                await CardPileCmd.Add(card, (PileType)4, (CardPilePosition)1, null, false);

                if (pool.Count > 0)
                {
                    var randomId = owner.RunState.Rng.Shuffle.NextItem(pool);
                    var generated = CombatState.CreateCard(randomId, owner);
                    generated.AddModKeyword(TransmigrationRules.TransmigrationKeywordId);
                    await CardPileCmd.AddGeneratedCardToCombat(generated, PileType.Hand, owner, CardPilePosition.Bottom);
                }

                await PowerCmd.Apply<PerjuryPower>(choiceContext, owner.Creature, 1, owner.Creature, source, false);
                await CreatureCmd.Heal(owner.Creature, 1);
            }
        }

        perjury = owner.Creature.GetPower<PerjuryPower>();
        suspect = owner.Creature.GetPower<SuspectPower>();
        perjuryAmt = perjury?.Amount ?? 0;
        suspectAmt = suspect?.Amount ?? 0;

        // ===== 2. 伪证 =====
        if (perjuryAmt > 0)
        {
            var enemies = owner.Creature.CombatState.Creatures
                .Where(c => c.IsEnemy && c.IsAlive).ToList();

            if (enemies.Count > 0)
            {
                var rng = new System.Random();
                for (var i = 0; i < perjuryAmt; i++)
                {
                    await CreatureCmd.Damage(
                        choiceContext, enemies[rng.Next(enemies.Count)],
                        2 + (int)(withAmt / 50),
                        ValueProp.Unpowered, owner.Creature, null
                    );
                }
            }
            await PowerCmd.Apply<JusticePower>(choiceContext, owner.Creature, perjuryAmt, owner.Creature, source, false);
            await PowerCmd.Remove(perjury);
        }

        // ===== 3. 嫌疑 =====
        if (suspectAmt > 0)
        {
            await PowerCmd.Apply<WithPower>(choiceContext, owner.Creature, suspectAmt * 20, owner.Creature, source, false);
            await PlayerCmd.GainEnergy(suspectAmt, owner);
            await PowerCmd.Remove(suspect);
        }

        // ===== 4. 正义 =====
        if (justiceAmt > 0)
        {
            var allies = owner.Creature.CombatState.Creatures
                .Where(c => c.IsAlive && !c.IsEnemy)
                .ToList();

            foreach (var ally in allies)
                await CreatureCmd.Heal(ally, justiceAmt);

            handCards = PileType.Hand.GetPile(owner).Cards.Where(c => c != source).ToList();
            if (justiceAmt >= 10)
            {
                foreach (var card in handCards)
                    card.AddModKeyword(TransmigrationRules.TransmigrationKeywordId);
            }
        }

        // ===== 5. 魔女化 =====
        if (withAmt >= 100)
        {
            await PowerCmd.Apply<OriginalsinjusticePower>(
                choiceContext, owner.Creature, 1, owner.Creature, source, false
            );
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}