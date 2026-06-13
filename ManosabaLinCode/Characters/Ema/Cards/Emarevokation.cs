using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Emalin;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System;
using System.Collections.Generic;
using System.Linq;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Hiro.Cards;
using ManosabaLin.Characters.Hiro.Powers;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class Emarevokation() : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    private const int MaxHpLoss = 3;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(10m, ValueProp.Move),
        new CardsVar(1)
    };

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromCard<EmaWitchKillerCard>();
            yield return HoverTipFactory.FromPower<EmaWitchKillerPower>();
            yield return HoverTipFactory.FromCard<Lamort>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var target = cardPlay.Target;
        ArgumentNullException.ThrowIfNull(target);

        // 降低 3 点血量上限
        await CreatureCmd.LoseMaxHp(choiceContext, source.Owner.Creature, MaxHpLoss, true);

        // 攻击目标
        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);
        await DamageCmd.Attack(source.DynamicVars.Damage.BaseValue)
            .FromCard(source)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 从牌组中搜索魔女杀手卡牌
        var deckCards = PileType.Deck.GetPile(source.Owner).Cards.Where(c => c is EmaWitchKillerCard).ToList();

        // 选择 0~1 张魔女杀手移除（可不选）
        var cardRemoved = false;
        if (deckCards.Count > 0)
        {
            var prefs = new CardSelectorPrefs(source.SelectionScreenPrompt, 0, 1);
            var selected = await CardSelectCmd.FromSimpleGrid(
                choiceContext, deckCards, source.Owner, prefs
            );

            var card = selected.FirstOrDefault();
            if (card != null)
            {
                // 从牌组永久移除
                await CardPileCmd.RemoveFromDeck(card);

                // 同时移除局内对应卡牌
                var combatPiles = new[] { PileType.Draw, PileType.Hand, PileType.Discard, PileType.Exhaust };
                foreach (var pileType in combatPiles)
                {
                    var pileCards = pileType.GetPile(source.Owner).Cards.Where(c => c is EmaWitchKillerCard).ToList();
                    foreach (var pileCard in pileCards)
                        await CardPileCmd.RemoveFromCombat(pileCard);
                }

                cardRemoved = true;
            }
        }

        // 移除魔女杀手能力
        var hasWitchKillerPower = source.Owner.Creature.GetPower<EmaWitchKillerPower>() != null;
        if (hasWitchKillerPower)
            await PowerCmd.Remove<EmaWitchKillerPower>(source.Owner.Creature);

        // 如果移除了卡牌或移除了能力，加入一张死神
        if (cardRemoved || hasWitchKillerPower)
        {
            var justice = source.CombatState.CreateCard<Lamort>(source.Owner);
            justice.SetToFreeThisTurn();
            CardCmd.PreviewCardPileAdd(
                await CardPileCmd.AddGeneratedCardToCombat(justice, PileType.Hand, source.Owner)
            );
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Damage.UpgradeValueBy(5m);
    }
}