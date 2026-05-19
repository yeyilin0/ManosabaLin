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
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using System;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class Xueqinjincard1 : ManosabaEmalinCardTemplate
{
    public Xueqinjincard1() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<BondPower>();
            yield return HoverTipFactory.FromCard<Xueqinjincard2>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var owner = source.Owner;
        var creature = owner.Creature;
        var bond = creature.GetPower<BondPower>();

        // 如果亲近 > 7，本卡变形为 Xueqinjincard2 并加入手牌，跳过所有效果
        if (bond != null && bond.Affinity > 7)
        {
            var transformedCard = source.CombatState.CreateCard<Xueqinjincard2>(owner);
            await CardPileCmd.AddGeneratedCardToCombat(transformedCard, PileType.Hand, owner);
            return;
        }

        // 疏远 +1
        if (bond != null) bond.Estrangement++;

        // 造成 3 点伤害
        if (cardPlay.Target != null)
        {
            await CreatureCmd.Damage(choiceContext, cardPlay.Target, 3m,
                ValueProp.Move, creature, this);
        }

        // 选择一张手牌变成随机疏远牌
        var handPile = PileType.Hand.GetPile(owner);
        var handCards = handPile.Cards.ToList();
        if (handCards.Count == 0) return;

        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1, 1);
        var selected = await CardSelectCmd.FromHand(
            choiceContext, owner, prefs, null, this);
        var picked = selected.FirstOrDefault();
        if (picked == null) return;

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
        var chosenType = rng.NextItem(estrangementCards);

        var createCardMethod = typeof(ICombatState).GetMethod("CreateCard", new Type[] { typeof(Player) });
        var genericMethod = createCardMethod.MakeGenericMethod(chosenType);
        var estrangementCard = (CardModel)genericMethod.Invoke(source.CombatState, new object[] { owner });
        estrangementCard.AddKeyword(CardKeyword.Retain);

        await CardCmd.Transform(picked, estrangementCard);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}