using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Emalin;
using ManosabaLin.Characters.Emalin.Components;
using ManosabaLin.Characters.Emalin.Enchantments;
using ManosabaLin.Characters.Ema.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Rooms;

namespace ManosabaLin.Characters.Ema.Relics;

[RegisterRelic(typeof(EmalinRelicPool))]
[RegisterCharacterStarterRelic(typeof(Emalin.Emalin))]
[RegisterTouchOfOrobasRefinement(typeof(Withema))]
public class EmaTrialBadge : ManosabaRelicTemplate
{
    private int _agreeCount;
    private int _doubtCount;
    private int _rebuttalCount;
    private int _lastResetRound;
    private bool _enchantedThisCombat;

    public override RelicRarity Rarity => RelicRarity.Starter;
    public override bool ShouldFlashOnPlayer => false;

    public int AgreeCount => _agreeCount;
    public int DoubtCount => _doubtCount;
    public int RebuttalCount => _rebuttalCount;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            foreach (var tip in HoverTipFactory.FromEnchantment<Agreement>())
                yield return tip;
            foreach (var tip in HoverTipFactory.FromEnchantment<Rebuttal>())
                yield return tip;
            foreach (var tip in HoverTipFactory.FromEnchantment<Doubt>())
                yield return tip;
        }
    }

    [SavedProperty]
    public int LastResetRound
    {
        get => _lastResetRound;
        set { AssertMutable(); _lastResetRound = value; }
    }

    /// <summary>
    ///     ⚠️ 三个审判计数必须走 <c>[SavedProperty]</c>：它们决定 ×2/×3/×4/×5 的阈值效果
    ///     （含「随机 1 张手牌免费」这类直接改<b>卡牌费用</b>的效果），而费用进校验和 ——
    ///     计数一旦在两端不一致，就会在某个 ×5 的回合让一端改牌、另一端不改 ⇒ StateDivergence（踢人）。
    ///     <para>
    ///         计数器还有个天然放大器：<see cref="SyncCountersToEnchantments" /> 会把本地计数写进
    ///         <c>Enchantment.Amount</c>（那是**同步**字段）—— 计数不同步会直接把差别刻进同步状态里。
    ///     </para>
    ///     <para>
    ///         普通私有字段在**重连/中途加入**时不会被传递（对端只收到 <c>RelicState</c> 里的 props），
    ///         所以队友掉线重连后计数会被清 0，与本机脱节 —— 这正是 2026-10-15 那次掉线后
    ///         再次分歧的温床。
    ///     </para>
    /// </summary>
    [SavedProperty]
    public int SavedAgreeCount
    {
        get => _agreeCount;
        set { AssertMutable(); _agreeCount = value; }
    }

    /// <summary>【疑问】计数（同步字段，说明见 <see cref="SavedAgreeCount" />）。</summary>
    [SavedProperty]
    public int SavedDoubtCount
    {
        get => _doubtCount;
        set { AssertMutable(); _doubtCount = value; }
    }

    /// <summary>【反驳】计数（同步字段，说明见 <see cref="SavedAgreeCount" />）。</summary>
    [SavedProperty]
    public int SavedRebuttalCount
    {
        get => _rebuttalCount;
        set { AssertMutable(); _rebuttalCount = value; }
    }

    [SavedProperty]
    public bool EnchantedThisCombat
    {
        get => _enchantedThisCombat;
        set { AssertMutable(); _enchantedThisCombat = value; }
    }

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        MegaCrit.Sts2.Core.Entities.Players.Player player)
    {
        if (player != Owner) return;

        var combatState = player.Creature.CombatState;
        var currentRound = combatState.RoundNumber;

        if (!EnchantedThisCombat)
        {
            EnchantedThisCombat = true;
            EnchantAllTrialCards();
        }

        if (currentRound != LastResetRound)
        {
            LastResetRound = currentRound;
            _agreeCount = 0;
            _doubtCount = 0;
            _rebuttalCount = 0;
            SyncCountersToEnchantments();
        }
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        EnchantedThisCombat = false;
        _agreeCount = 0;
        _doubtCount = 0;
        _rebuttalCount = 0;
    }

    public void IncrementCount(EnchantmentModel enchantment)
    {
        switch (enchantment)
        {
            case Agreement:
                _agreeCount++;
                break;
            case Doubt:
                _doubtCount++;
                break;
            case Rebuttal:
                _rebuttalCount++;
                break;
        }
        SyncCountersToEnchantments();
    }

    private void SyncCountersToEnchantments()
    {
        var piles = new[] { PileType.Hand, PileType.Draw, PileType.Discard };
        foreach (var pileType in piles)
        {
            foreach (var card in pileType.GetPile(Owner).Cards)
            {
                switch (card.Enchantment)
                {
                    case Agreement:
                        card.Enchantment.Amount = _agreeCount;
                        break;
                    case Doubt:
                        card.Enchantment.Amount = _doubtCount;
                        break;
                    case Rebuttal:
                        card.Enchantment.Amount = _rebuttalCount;
                        break;
                }
            }
        }
    }

    protected virtual void EnchantAllTrialCards()
    {
        var allCards = PileType.Draw.GetPile(Owner).Cards
            .Concat(PileType.Hand.GetPile(Owner).Cards)
            .Concat(PileType.Discard.GetPile(Owner).Cards)
            .Concat(PileType.Deck.GetPile(Owner).Cards)
            .Distinct()
            .ToList();

        foreach (var card in allCards)
        {
            if (card.Enchantment != null) continue;

            try
            {
                if (EmalinKeywordRules.HasAgreeKeyword(card))
                    CardCmd.Enchant(ModelDb.Enchantment<Agreement>().ToMutable(), card, 1m);
                else if (EmalinKeywordRules.HasRebuttalKeyword(card))
                    CardCmd.Enchant(ModelDb.Enchantment<Rebuttal>().ToMutable(), card, 1m);
                else if (EmalinKeywordRules.HasDoubtKeyword(card))
                    CardCmd.Enchant(ModelDb.Enchantment<Doubt>().ToMutable(), card, 1m);
            }
            catch (Exception ex)
            {
                MainFile.Logger.Info($"[{GetType().Name}] Skip {card.Id.Entry}: {ex.Message}");
            }
        }

        // 给随机卡牌添加审判组件
        var componentPiles = new[] { PileType.Draw, PileType.Hand, PileType.Discard };
        var availableCards = componentPiles
            .SelectMany(p => p.GetPile(Owner).Cards)
            .Where(CanReceiveInitialTrialComponent)
            .Distinct()
            .ToList();

        if (availableCards.Count >= 3)
        {
            var rng = Owner.RunState.Rng.CombatCardSelection;
            var shuffled = availableCards.OrderBy(_ => rng.NextFloat()).ToList();

            shuffled[0].TryAddComponent(new AgreementTrialComponent());
            shuffled[1].TryAddComponent(new RebuttalTrialComponent());
            shuffled[2].TryAddComponent(new DoubtTrialComponent());
        }

        if (Owner?.Creature != null)
            PowerCmd.Apply<BondPower>(null, Owner.Creature, 1m, Owner.Creature, null, false);
    }

    protected virtual bool CanReceiveInitialTrialComponent(CardModel card)
    {
        return card.Enchantment == null;
    }
}
