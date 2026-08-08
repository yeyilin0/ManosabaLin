using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Emalin;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class BondSettlement : ManosabaCardTemplate
{
    public BondSettlement() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    private static readonly Type[] BondCardTypes =
    [
        typeof(BalloonFragments),
        typeof(StabbingBlade),
        typeof(ShatteredResonance),
        typeof(WitchCleansing),
        typeof(ChainedTrust),
        typeof(PawnRealization),
        typeof(NoahEstrangement),
        typeof(MargaretEstrangement),
        typeof(CocoEstrangement),
        typeof(AnnEstrangement),
        typeof(Hiroshuyuancard),
        typeof(Lyshuyuan),
        typeof(SwapBodySuccess),
        typeof(GuardianOath),
        typeof(Sharedfate),
        typeof(DollGift),
        typeof(TheOnlyClue),
        typeof(SubstituteCost),
        typeof(NoahAffinity),
        typeof(MargaretAffinity),
        typeof(CocoAffinity),
        typeof(AnnAffinity),
        typeof(Lyqinjin),
        typeof(BondSettlement),
    ];

    private static readonly Type[] AffinityTypes =
    [
        typeof(SwapBodySuccess),
        typeof(GuardianOath),
        typeof(Sharedfate),
        typeof(DollGift),
        typeof(TheOnlyClue),
        typeof(SubstituteCost),
        typeof(NoahAffinity),
        typeof(MargaretAffinity),
        typeof(CocoAffinity),
        typeof(AnnAffinity),
        typeof(Lyqinjin),
        typeof(BondSettlement),
    ];

    protected override CardLocation GetResultLocationForCardPlayC()
    {
        return new CardLocation(Owner, PileType.Exhaust, CardPilePosition.Bottom);
    }

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

        // 使用 HashSet 按实例去重
        var seen = new HashSet<CardModel>();
        var allBondCards = new List<CardModel>();

        foreach (var card in drawPile.Cards.Concat(handPile.Cards).Concat(discardPile.Cards))
        {
            if (ReferenceEquals(card, this) || ReferenceEquals(card, cardPlay.Card))
                continue;

            if (BondCardTypes.Contains(card.GetType()) && seen.Add(card))
                allBondCards.Add(card);
        }

        var exhaustedCount = BondCardTypes.Contains(cardPlay.Card.GetType()) ? 1 : 0;
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
            await CreatureCmd.Damage(choiceContext, target, 6m, ValueProp.Unpowered, this, cardPlay);
        }

        // 重新读取亲和
        affinity = bond?.Affinity ?? 0;

        // ===== 按亲近层数生成等量随机亲近卡 =====
        for (int i = 0; i < affinity; i++)
        {
            var chosenType = rng.NextItem(AffinityTypes);
            var newCard = CreateAffinityCard(CombatState, chosenType, owner);
            await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, owner, CardPilePosition.Bottom);
        }

        // ===== 选择疏远层数张手牌减1费（循环 FromHand） =====
        if (estrangement > 0)
        {
            var discountableCards = PileType.Hand.GetPile(owner).Cards
                .Where(c => !ReferenceEquals(c, this) && !ReferenceEquals(c, cardPlay.Card))
                .Distinct()
                .ToHashSet();
            var maxSelect = Math.Min(estrangement, discountableCards.Count);

            if (maxSelect > 0)
            {
                var prefs = new CardSelectorPrefs(SelectionScreenPrompt, maxSelect, maxSelect);
                var selected = await CardSelectCmd.FromHand(
                    choiceContext, owner, prefs, discountableCards.Contains, this);

                foreach (var card in selected.Distinct())
                    card.EnergyCost.UpgradeBy(-1);
            }
        }
    }

    private static CardModel CreateAffinityCard(ICombatState combatState, Type cardType, Player owner)
    {
        if (cardType == typeof(SwapBodySuccess)) return combatState.CreateCard<SwapBodySuccess>(owner);
        if (cardType == typeof(GuardianOath)) return combatState.CreateCard<GuardianOath>(owner);
        if (cardType == typeof(Sharedfate)) return combatState.CreateCard<Sharedfate>(owner);
        if (cardType == typeof(DollGift)) return combatState.CreateCard<DollGift>(owner);
        if (cardType == typeof(TheOnlyClue)) return combatState.CreateCard<TheOnlyClue>(owner);
        if (cardType == typeof(SubstituteCost)) return combatState.CreateCard<SubstituteCost>(owner);
        if (cardType == typeof(NoahAffinity)) return combatState.CreateCard<NoahAffinity>(owner);
        if (cardType == typeof(MargaretAffinity)) return combatState.CreateCard<MargaretAffinity>(owner);
        if (cardType == typeof(CocoAffinity)) return combatState.CreateCard<CocoAffinity>(owner);
        if (cardType == typeof(AnnAffinity)) return combatState.CreateCard<AnnAffinity>(owner);
        if (cardType == typeof(Lyqinjin)) return combatState.CreateCard<Lyqinjin>(owner);
        if (cardType == typeof(BondSettlement)) return combatState.CreateCard<BondSettlement>(owner);

        throw new InvalidOperationException($"Unsupported affinity card type: {cardType.FullName}");
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
