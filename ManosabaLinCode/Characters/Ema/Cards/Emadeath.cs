using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Emalin.Enchantments;
using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ManosabaLin.Characters.Emalin;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(LinCardPool))]
public sealed class Emadeath : ManosabaCardTemplate
{
    public Emadeath() : base(-1, CardType.Skill, CardRarity.Ancient, TargetType.AllAllies)
    {
    }

    public override int MaxUpgradeLevel => 0;

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
        typeof(SharedFate),
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

    private static readonly Type[] EnchantTypes = [typeof(Rebuttal), typeof(Agreement), typeof(Doubt)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var owner = source.Owner;
        var creature = owner.Creature;
        var combatState = source.CombatState;
        var createCardMethod = typeof(ICombatState).GetMethod("CreateCard", [typeof(Player)]);
        var rng = owner.RunState.Rng.CombatCardSelection;

        await CreatureCmd.TriggerAnim(creature, "Cast", owner.Character.CastAnimDelay);

        // 读取羁绊值
        var bond = creature.GetPower<BondPower>();
        var affinity = bond?.Affinity ?? 0;
        var estrangement = bond?.Estrangement ?? 0;
        var higherBondValue = Math.Max(affinity, estrangement);
        var bondCardCount = higherBondValue / 2;

        // 统计审判附魔数量（抽牌堆+弃牌堆+手卡）
        var drawPile = PileType.Draw.GetPile(owner);
        var handPile = PileType.Hand.GetPile(owner);
        var discardPile = PileType.Discard.GetPile(owner);

        var trialEnchantCount = drawPile.Cards.Concat(handPile.Cards).Concat(discardPile.Cards)
            .Count(c => c.Enchantment is Rebuttal or Agreement or Doubt);
        var enchantTargetCount = trialEnchantCount / 2;

        // 对全体队友生效
        var teammates = combatState.GetTeammatesOf(creature)
            .Where(c => c != null && c.IsAlive && c.IsPlayer);

        foreach (var teammate in teammates)
        {
            // 消耗50层魔女化
            var withPower = teammate.GetPower<WithPower>();
            if (withPower != null && withPower.Amount > 0)
            {
                var withToRemove = Math.Min(50, (int)withPower.Amount);
                await PowerCmd.ModifyAmount(choiceContext, withPower, -withToRemove, creature, source, false);
            }

            // 消耗3层嫌疑
            var suspectPower = teammate.GetPower<SuspectPower>();
            if (suspectPower != null && suspectPower.Amount > 0)
            {
                var suspectToRemove = Math.Min(3, (int)suspectPower.Amount);
                await PowerCmd.ModifyAmount(choiceContext, suspectPower, -suspectToRemove, creature, source, false);
            }

            if (teammate.Player == null) continue;

            // 给予等量的羁绊卡并减1费
            for (int i = 0; i < bondCardCount; i++)
            {
                var chosenType = rng.NextItem(BondCardTypes);
                var genericMethod = createCardMethod.MakeGenericMethod(chosenType);
                var newCard = (CardModel)genericMethod.Invoke(combatState, [teammate.Player]);
                newCard.EnergyCost.UpgradeBy(-1);
                await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Draw, teammate.Player, CardPilePosition.Random);
            }

            // 给予等量的审判附魔（跳过已有附魔的卡）
            var teammateDrawPile = PileType.Draw.GetPile(teammate.Player);
            var teammateHandPile = PileType.Hand.GetPile(teammate.Player);
            var teammateDiscardPile = PileType.Discard.GetPile(teammate.Player);

            var unenchantedCards = teammateDrawPile.Cards
                .Concat(teammateHandPile.Cards)
                .Concat(teammateDiscardPile.Cards)
                .Where(c => c.Enchantment == null)
                .Distinct()
                .ToList();

            var rebuttalCanonical = ModelDb.Enchantment<Rebuttal>();
            var agreementCanonical = ModelDb.Enchantment<Agreement>();
            var doubtCanonical = ModelDb.Enchantment<Doubt>();

            var cardsToEnchant = unenchantedCards
                .OrderBy(_ => rng.NextFloat())
                .Take(enchantTargetCount)
                .ToList();

            foreach (var card in cardsToEnchant)
            {
                var chosenEnchant = rng.NextItem(EnchantTypes);
                if (chosenEnchant == typeof(Rebuttal))
                    CardCmd.Enchant(rebuttalCanonical.ToMutable(), card, 1m);
                else if (chosenEnchant == typeof(Agreement))
                    CardCmd.Enchant(agreementCanonical.ToMutable(), card, 1m);
                else
                    CardCmd.Enchant(doubtCanonical.ToMutable(), card, 1m);
            }
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
    }
}
