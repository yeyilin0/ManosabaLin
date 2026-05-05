using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public sealed class WithPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    // ===== 增伤（不影响 Unpowered 的伤害）=====
    public override decimal ModifyDamageMultiplicative(Creature? target, decimal amount, ValueProp props,
        Creature? dealer, CardModel? cardSource)
    {
        if (dealer != Owner) return 1m;
        if (props.HasFlag(ValueProp.Unpowered)) return 1m;
        return 1m + Amount / 100m;
    }

    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer,
        CardModel? cardSource)
    {
        if (dealer != Owner) return 0m;
        if (props.HasFlag(ValueProp.Unpowered)) return 0m;
        return Amount / 25;
    }

    // ===== 300层+：技能牌打出后进入消耗堆 =====
    public override (PileType, CardPilePosition) ModifyCardPlayResultPileTypeAndPosition(
        CardModel card, bool isAutoPlay, ResourceInfo resources, PileType pileType, CardPilePosition position)
    {
        if (Amount < 300 || card.Owner.Creature != Owner || card.Type != CardType.Skill)
            return (pileType, position);
        return (PileType.Exhaust, position);
    }

    // ===== 打出卡牌时 =====
    public override async Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
    {
        var source = this;
        if (cardPlay.Card.Owner.Creature != source.Owner) return;

        // [200层+] 打出技能/能力牌失去血量
        if (source.Amount >= 200)
            if (cardPlay.Card.Type == CardType.Skill || cardPlay.Card.Type == CardType.Power)
            {
                var hpLoss = 1m;
                if (source.Amount >= 300)
                    hpLoss += source.Amount / 100m;

                await CreatureCmd.Damage(
                    context,
                    source.Owner,
                    hpLoss,
                    ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
                    cardPlay.Card
                );
            }

        // [300层+] 打出攻击牌时获得 3 血
        if (source.Amount >= 300 && cardPlay.Card.Type == CardType.Attack) await CreatureCmd.Heal(source.Owner, 3m);
    }

    // ===== 回合结束扣血 =====
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Side) return;
        var source = this;
        if (source.Amount >= 200)
            await CreatureCmd.Damage(
                choiceContext,
                source.Owner,
                13m,
                ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
                source.Owner
            );
    }

    // ===== DeathRewind 获取 =====
    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext, // ★ 新增
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power != this) return;
        await CheckAndGiveDeathRewind();
    }

    private async Task CheckAndGiveDeathRewind()
    {
        if (Amount < 100) return;
        if (Owner?.Player == null) return;

        if (Owner.Player.Character.GetType() != typeof(Hiro)) return;

        var deck = Owner.Player.Deck;
        if (deck.Cards.Any(c => c is DeathRewind)) return;

        var cardModel = ModelDb.GetById<CardModel>(ModelDb.GetId<DeathRewind>());
        if (cardModel == null) return;

        var permanentCard = Owner.Player.RunState.CreateCard(cardModel, Owner.Player);
        await CardPileCmd.Add(permanentCard, PileType.Deck);
        CardCmd.PreviewCardPileAdd(new CardPileAddResult { success = true, cardAdded = permanentCard });

        if (Owner.Player.Deck.Cards.Any(c => c is DeathRewind))
            if (Owner.CombatState != null)
            {
                var tempCard = Owner.CombatState.CreateCard(cardModel, Owner.Player);
                await CardPileCmd.AddGeneratedCardToCombat(tempCard, PileType.Hand, Owner.Player);
            }
    }
}