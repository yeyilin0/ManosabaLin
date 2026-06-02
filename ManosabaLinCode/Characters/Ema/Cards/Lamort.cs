using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Emalin;
using ManosabaLin.Characters.Emalin.Enchantments;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Combat;
using System;
using System.Reflection;
using ManosabaLin.Characters.Hiro.Powers;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class Lamort : ManosabaCardTemplate
{
    public Lamort() : base(3, CardType.Power, CardRarity.Ancient, TargetType.Self)
    {
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<WithPower>();
            yield return HoverTipFactory.FromPower<EmaWitchFactorPower>();
            yield return HoverTipFactory.FromPower<RitualCeremonyPower>();
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<WithPower>(100m),
        new PowerVar<RitualCeremonyPower>(1m),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var owner = source.Owner;
        var creature = owner.Creature;
        var combatState = source.CombatState;

        await CreatureCmd.TriggerAnim(creature, "Cast", owner.Character.CastAnimDelay);

        await PowerCmd.Apply<WithPower>(
            choiceContext, creature,
            source.DynamicVars["WithPower"].BaseValue,
            creature, source, false);

        var allCreatures = combatState.Allies
            .Concat(combatState.Enemies)
            .Where(c => c.IsAlive)
            .ToList();

        foreach (var target in allCreatures)
        {
            var hpQuarter = (int)(target.CurrentHp / 4);
            if (hpQuarter > 0)
            {
                await PowerCmd.Apply<EmaWitchFactorPower>(
                    choiceContext, target, hpQuarter,
                    creature, source, false);
            }
        }

        await PowerCmd.Apply<RitualCeremonyPower>(
            choiceContext, creature,
            source.DynamicVars["RitualCeremonyPower"].BaseValue,
            creature, source, false);

        var bond = creature.GetPower<BondPower>();
        var createCardMethod = typeof(ICombatState).GetMethod("CreateCard", new Type[] { typeof(Player) });

        for (int i = 0; i < 3; i++)
        {
            var affinityCard = (CardModel)createCardMethod.MakeGenericMethod(typeof(Emamqinjin)).Invoke(combatState, new object[] { owner });
            var estrangementCard = (CardModel)createCardMethod.MakeGenericMethod(typeof(Emamshuyuan)).Invoke(combatState, new object[] { owner });
            affinityCard.SetToFreeThisTurn();
            estrangementCard.SetToFreeThisTurn();

            var options = new List<CardModel> { affinityCard, estrangementCard };
            var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1, 1);
            var selected = await CardSelectCmd.FromSimpleGrid(choiceContext, options, owner, prefs);
            var picked = selected.FirstOrDefault();
            if (picked == null) continue;

            await CardPileCmd.AddGeneratedCardToCombat(picked, PileType.Hand, owner);
            await Cmd.Wait(0.1f);

            Creature? autoTarget = null;
            if (picked.TargetType == TargetType.AnyEnemy)
            {
                autoTarget = combatState.GetOpponentsOf(creature)
                    .Where(c => c.IsAlive)
                    .FirstOrDefault();
            }

            await CardCmd.AutoPlay(choiceContext, picked, autoTarget);
        }

        bond = creature.GetPower<BondPower>();
        var affinity = bond?.Affinity ?? 0;
        var estrangement = bond?.Estrangement ?? 0;
        var higherBondValue = Math.Max(affinity, estrangement);
        var lowerBondValue = Math.Min(affinity, estrangement);

        var enchantTypes = new Type[] { typeof(Rebuttal), typeof(Agreement), typeof(Doubt) };
        var rng = owner.RunState.Rng.CombatCardSelection;

        var rebuttalCanonical = ModelDb.Enchantment<Rebuttal>();
        var agreementCanonical = ModelDb.Enchantment<Agreement>();
        var doubtCanonical = ModelDb.Enchantment<Doubt>();

        var judgmentCards = new List<CardModel>();
        for (int i = 0; i < higherBondValue; i++)
        {
            var bondCard = CreateRandomBondCard(combatState, owner, createCardMethod);

            var chosenEnchant = rng.NextItem(enchantTypes);
            if (chosenEnchant == typeof(Rebuttal))
                CardCmd.Enchant(rebuttalCanonical.ToMutable(), bondCard, 1m);
            else if (chosenEnchant == typeof(Agreement))
                CardCmd.Enchant(agreementCanonical.ToMutable(), bondCard, 1m);
            else
                CardCmd.Enchant(doubtCanonical.ToMutable(), bondCard, 1m);

            judgmentCards.Add(bondCard);
        }

        var toDiscount = judgmentCards
            .OrderBy(_ => rng.NextFloat())
            .Take(lowerBondValue)
            .ToList();

        foreach (var card in toDiscount)
        {
            card.EnergyCost.UpgradeBy(-1);
        }

        if (judgmentCards.Count > 0)
        {
            var orderPrefs = new CardSelectorPrefs(SelectionScreenPrompt, judgmentCards.Count, judgmentCards.Count)
            {
                PretendCardsCanBePlayed = true
            };
            var orderedCards = await CardSelectCmd.FromSimpleGrid(
                choiceContext, judgmentCards, owner, orderPrefs);

            foreach (var card in orderedCards)
            {
                CardCmd.PreviewCardPileAdd(
                    await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Draw, owner, CardPilePosition.Random));
            }
        }
    }

    private CardModel CreateRandomBondCard(ICombatState combatState, Player owner, MethodInfo createCardMethod)
    {
        var bondCardTypes = new[]
        {
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
        };

        var rng = owner.RunState.Rng.CombatCardSelection;
        var chosenType = rng.NextItem(bondCardTypes);
        var genericMethod = createCardMethod.MakeGenericMethod(chosenType);
        return (CardModel)genericMethod.Invoke(combatState, new object[] { owner });
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}