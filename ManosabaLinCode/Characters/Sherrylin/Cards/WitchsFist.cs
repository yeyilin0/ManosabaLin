using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ManosabaLin.Characters.Sherrylin.Cards;

/// <summary>
/// 魔女之拳：攻击敌人，可消耗怪力卡或移除怪力能力，获得带保留的愚者。
/// </summary>
[RegisterCard(typeof(SherrylinCardPool))]
public sealed class WitchsFist() : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(10m, ValueProp.Move),
    };

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromCard<SuperStrength>();
            yield return HoverTipFactory.FromPower<SuperStrengthPower>();
            yield return HoverTipFactory.FromCard<TheFool>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var target = cardPlay.Target;
        ArgumentNullException.ThrowIfNull(target);

        // 攻击目标
        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);
        await DamageCmd.Attack(source.DynamicVars.Damage.BaseValue)
            .FromCard(source)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 从牌组中搜索怪力卡牌
        var deckCards = PileType.Deck.GetPile(source.Owner).Cards.Where(c => c is SuperStrength).ToList();

        // 选择 0~1 张怪力移除（可不选）
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
                    var pileCards = pileType.GetPile(source.Owner).Cards.Where(c => c is SuperStrength).ToList();
                    foreach (var pileCard in pileCards)
                        await CardPileCmd.RemoveFromCombat(pileCard);
                }

                cardRemoved = true;
            }
        }

        // 移除怪力能力
        var hasSuperStrengthPower = source.Owner.Creature.GetPower<SuperStrengthPower>() != null;
        if (hasSuperStrengthPower)
            await PowerCmd.Remove<SuperStrengthPower>(source.Owner.Creature);

        // 如果移除了卡牌或移除了能力，加入一张带保留的愚者
        if (cardRemoved || hasSuperStrengthPower)
        {
            var fool = source.CombatState.CreateCard<TheFool>(source.Owner);
            fool.AddKeyword(CardKeyword.Retain);
            CardCmd.PreviewCardPileAdd(
                await CardPileCmd.AddGeneratedCardToCombat(fool, PileType.Hand, source.Owner)
            );
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Damage.UpgradeValueBy(5m);
    }
}
