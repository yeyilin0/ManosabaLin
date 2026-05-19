using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Emalin;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class Lyshuyuan : ManosabaEmalinCardTemplate
{
    public Lyshuyuan() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<BondPower>(); }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new DamageVar(8m, ValueProp.Move); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var owner = source.Owner;
        var creature = owner.Creature;
        var combatState = source.CombatState;

        // 造成8点伤害
        if (cardPlay.Target != null)
        {
            await CreatureCmd.Damage(choiceContext, cardPlay.Target, source.DynamicVars.Damage.BaseValue,
                ValueProp.Move, creature, this);
        }

        var bond = creature.GetPower<BondPower>();
        if (bond == null) return;

        // 疏远 +1
        bond.Estrangement++;

        // 获取较高值，消耗掉
        var higherValue = Math.Max(bond.Affinity, bond.Estrangement);
        if (higherValue <= 0) return;

        // 消耗较高值
        if (bond.Affinity >= bond.Estrangement)
            bond.Affinity -= higherValue;
        else
            bond.Estrangement -= higherValue;

        // 从卡组中收集可升级羁绊牌并升级
        var deckPile = PileType.Deck.GetPile(owner);
        var bondCardTypes = new[]
        {
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
        };

        var bondCards = deckPile.Cards
            .Where(c => bondCardTypes.Contains(c.GetType()) && c.IsUpgradable)
            .Take(higherValue)
            .ToList();

        foreach (var card in bondCards)
        {
            CardCmd.Upgrade(card);
        }

        // 疏远 > 亲近时，从卡组选卡变形为随机疏远卡并升级
        if (bond.Estrangement > bond.Affinity)
        {
            var deckCards = deckPile.Cards.ToList();
            var selectCount = Math.Min(higherValue, deckCards.Count);
            if (selectCount > 0)
            {
                var prefs = new CardSelectorPrefs(SelectionScreenPrompt, selectCount, selectCount);
                var selected = await CardSelectCmd.FromSimpleGrid(
                    choiceContext, deckCards.Cast<CardModel>().ToList(), owner, prefs);
                var picked = selected.ToList();

                var estrangementCards = new[]
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
                };

                var rng = owner.RunState.Rng.CombatCardSelection;
                var createCardMethod = typeof(ICombatState).GetMethod("CreateCard", new Type[] { typeof(Player) });

                foreach (var card in picked)
                {
                    var chosenType = rng.NextItem(estrangementCards);
                    var genericMethod = createCardMethod.MakeGenericMethod(chosenType);
                    var newCard = (CardModel)genericMethod.Invoke(combatState, new object[] { owner });
                    CardCmd.Upgrade(newCard);
                    await CardCmd.Transform(card, newCard);
                }
            }
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}