using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Relics;

[RegisterRelic(typeof(HiroRelicPool))]
public sealed class Withhiro : ManosabaRelicTemplate
{
    private bool _hasTriggeredSuspectReduction;

    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<JusticePower>(6m),
        new PowerVar<SuspectPower>(2m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<JusticePower>();
            yield return HoverTipFactory.FromPower<SuspectPower>();
        }
    }

    // ★ 战斗开始时获得 6 层正义
    public override async Task AfterSideTurnStart(CombatSide side, ICombatState combatState)
    {
        var relic = this;

        if (side == relic.Owner.Creature.Side && combatState.RoundNumber == 1)
        {
            relic.Flash();

            await PowerCmd.Apply<JusticePower>(
                new ThrowingPlayerChoiceContext(),
                relic.Owner.Creature,
                relic.DynamicVars["JusticePower"].BaseValue,
                relic.Owner.Creature,
                (CardModel?)null,
                false
            );

            _hasTriggeredSuspectReduction = false;
        }
    }

    // ★ 回合开始：选择一张手牌返回抽牌堆
    // ★ 回合开始：选择一张手牌返回抽牌堆（可不选，CardEleven 风格）
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        var relic = this;
        if (player != relic.Owner) return;

        var handCards = PileType.Hand.GetPile(relic.Owner).Cards.ToList();
        if (handCards.Count == 0) return;

        var prefs = new CardSelectorPrefs(relic.SelectionScreenPrompt, 0, 1); // 最少0，最多1，可选可不选
        var selected = await CardSelectCmd.FromHand(
            choiceContext,
            relic.Owner,
            prefs,
            null,
            null
        );

        foreach (var card in selected) await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Random, null, false);
    }

    // ★ 当获得嫌疑时，第一次减少 2 层
    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amountChanged,
        Creature? applier,
        CardModel? cardSource)
    {
        var relic = this;

        if (_hasTriggeredSuspectReduction) return;
        if (power is not SuspectPower) return;
        if (power.Owner != relic.Owner.Creature) return;
        if (amountChanged <= 0) return;

        _hasTriggeredSuspectReduction = true;
        relic.Flash();

        var reduction = Math.Min((int)relic.DynamicVars["SuspectPower"].BaseValue, (int)power.Amount);
        if (reduction > 0)
            await PowerCmd.ModifyAmount(
                new ThrowingPlayerChoiceContext(), // ★ 第一个参数
                power,
                -reduction,
                relic.Owner.Creature,
                (CardModel?)null,
                false
            );
    }
}