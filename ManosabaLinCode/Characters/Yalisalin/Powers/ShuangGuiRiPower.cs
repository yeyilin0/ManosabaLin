using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Hiro.Cards;

using MegaCrit.Sts2.Core.Entities.Powers;

namespace ManosabaLin.Characters.Yalisalin.Powers;

/// <summary>
/// 双轨日：任意原罪触发「自惩」后，可对所有牌（抽牌堆 / 手牌 / 弃牌堆）
/// 里的一张原罪立刻宽恕。
/// </summary>
[RegisterPower]
public sealed class ShuangGuiRiPower : ManosabaPowerTemplate
{
    private bool _hooked;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        Hook();
        return Task.CompletedTask;
    }

    public override Task AfterRemoved(Creature oldOwner)
    {
        Unhook();
        return Task.CompletedTask;
    }

    private void Hook()
    {
        if (_hooked) return;
        Originalsin.PunishTriggered += OnPunishTriggered;
        _hooked = true;
    }

    private void Unhook()
    {
        if (!_hooked) return;
        Originalsin.PunishTriggered -= OnPunishTriggered;
        _hooked = false;
    }

    private async void OnPunishTriggered(PlayerChoiceContext choiceContext, CardModel punished)
    {
        try
        {
            if (Owner?.Player is not { } player) return;
            if (punished.Owner?.Creature != Owner) return;

            var candidates = CollectForgivableCards(player, punished);
            if (candidates.Count == 0) return;

            // min 0 = 可以选择跳过；候选可能来自抽牌堆 / 弃牌堆，故用网格选择而非手牌选择。
            var selected = (await CardSelectCmd.FromSimpleGrid(
                choiceContext,
                candidates,
                player,
                new CardSelectorPrefs(new LocString("powers", $"{Id.Entry}.selectionScreenPrompt"), 0, 1)))
                .FirstOrDefault();

            if ((selected as IComponentsCardModel)?.GetComponent<Originalsin>() is { } sin)
                await sin.Forgive(choiceContext);
        }
        catch (Exception ex)
        {
            Log.Error($"ShuangGuiRiPower.OnPunishTriggered failed: {ex}");
        }
    }

    /// <summary>
    /// 「所有牌」= 抽牌堆 + 手牌 + 弃牌堆（不含消耗堆）。
    /// </summary>
    private static List<CardModel> CollectForgivableCards(Player player, CardModel punished)
    {
        var result = new List<CardModel>();

        foreach (var pileType in new[] { PileType.Hand, PileType.Draw, PileType.Discard })
        {
            foreach (var card in pileType.GetPile(player).Cards)
            {
                if (ReferenceEquals(card, punished)) continue;
                if (SamePlaceTruth.IsSelectionLocked(card)) continue;
                if (!card.HasComponent<Originalsin>()) continue;

                result.Add(card);
            }
        }

        return result;
    }
}
