using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Emalin;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class BondSettlement : ManosabaCardTemplate
{
    public BondSettlement() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    private static readonly Type[] BondCardTypes =
    [
        typeof(BalloonFragments), typeof(StabbingBlade), typeof(ShatteredResonance),
        typeof(WitchCleansing), typeof(ChainedTrust), typeof(PawnRealization),
        typeof(NoahEstrangement), typeof(MargaretEstrangement),
        typeof(CocoEstrangement), typeof(AnnEstrangement),
        typeof(SwapBodySuccess), typeof(GuardianOath), typeof(SharedFate),
        typeof(DollGift), typeof(TheOnlyClue), typeof(SubstituteCost),
        typeof(NoahAffinity), typeof(MargaretAffinity),
        typeof(CocoAffinity), typeof(AnnAffinity)
    ];

    private static readonly Type[] AffinityTypes =
    [
        typeof(SwapBodySuccess), typeof(GuardianOath), typeof(SharedFate),
        typeof(DollGift), typeof(TheOnlyClue), typeof(SubstituteCost),
        typeof(NoahAffinity), typeof(MargaretAffinity),
        typeof(CocoAffinity), typeof(AnnAffinity)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var owner = Owner;
        var creature = owner.Creature;
        var rng = owner.RunState.Rng.CombatCardSelection;
        var enemies = CombatState.Enemies.Where(e => e.IsAlive).ToList();

        // 读取羁绊值
        var bond = creature.GetPower<BondPower>();
        var affinity = bond?.Affinity ?? 0;
        var estrangement = bond?.Estrangement ?? 0;

        // 亲近 +1
        if (bond != null)
            bond.Affinity++;

        // ===== 消耗抽牌堆、手牌、弃牌堆中所有羁绊卡 =====
        var drawPile = PileType.Draw.GetPile(owner);
        var handPile = PileType.Hand.GetPile(owner);
        var discardPile = PileType.Discard.GetPile(owner);

        var allBondCards = drawPile.Cards
            .Concat(handPile.Cards)
            .Concat(discardPile.Cards)
            .Where(c => BondCardTypes.Contains(c.GetType()))
            .Distinct()
            .ToList();

        var exhaustedCount = 0;
        foreach (var card in allBondCards)
        {
            await CardCmd.Exhaust(choiceContext, card);
            exhaustedCount++;
        }

        // ===== 每张对随机敌人造成6点伤害 =====
        for (int i = 0; i < exhaustedCount; i++)
        {
            if (enemies.Count == 0) break;
            var target = rng.NextItem(enemies);
            await CreatureCmd.Damage(choiceContext, target, 6m,
                ValueProp.Unpowered, creature, this);
        }

        // 重新读取亲和
        affinity = bond?.Affinity ?? 0;

        // ===== 按亲近层数生成等量随机亲近卡 =====
        var createCardMethod = typeof(ICombatState).GetMethod("CreateCard", [typeof(Player)]);
        for (int i = 0; i < affinity; i++)
        {
            var chosenType = rng.NextItem(AffinityTypes);
            var genericMethod = createCardMethod.MakeGenericMethod(chosenType);
            var newCard = (CardModel)genericMethod.Invoke(CombatState, [owner]);
            await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, owner, CardPilePosition.Bottom);
        }

        // ===== 选择疏远层数张手牌减1费 =====
        if (estrangement > 0)
        {
            var handCards = handPile.Cards;
            if (handCards.Count > 0)
            {
                var maxSelect = Math.Min(estrangement, handCards.Count);
                var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 0, maxSelect);
                var selected = await CardSelectCmd.FromHand(choiceContext, owner, prefs, null, this);

                foreach (var card in selected)
                {
                    card.EnergyCost.UpgradeBy(-1);
                }
            }
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}