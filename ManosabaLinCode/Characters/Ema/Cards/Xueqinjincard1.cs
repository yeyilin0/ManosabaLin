using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Emalin;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Component.Core;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class Xueqinjincard1 : ManosabaCardTemplate
{
    public Xueqinjincard1() : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy) { }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<BondPower>(); }
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
            var newCard = source.CombatState.CreateCard<Xueqinjincard2>(owner);
            await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, owner);
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

        // 随机生成一张疏远卡并加入手牌
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

        var method = typeof(ICombatState).GetMethod("CreateCard", new Type[] { typeof(Player) })
            ?.MakeGenericMethod(chosenType);
        var generatedCard = (CardModel)method?.Invoke(source.CombatState, new object[] { owner });

        generatedCard.AddKeyword(CardKeyword.Retain);
        await CardPileCmd.AddGeneratedCardToCombat(generatedCard, PileType.Hand, owner);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
