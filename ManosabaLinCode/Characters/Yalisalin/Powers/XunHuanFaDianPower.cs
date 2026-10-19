using ManosabaLin.Characters.Common.Components;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using System;
using System.Linq;
using System.Threading.Tasks;

using MegaCrit.Sts2.Core.Entities.Powers;

namespace ManosabaLin.Characters.Yalisalin.Powers;

/// <summary>
/// 循环法典：每当你宽恕/自惩时，若该原罪卡因此进入弃牌堆/消耗堆，改为放入抽牌堆顶部；
/// 并按照当前牌组内原罪卡数量，随机触发等量次的原罪卡「卡面效果」（每次在宽恕/自惩效果中随机取一条）。
/// </summary>
[RegisterPower]
public sealed class XunHuanFaDianPower : ManosabaPowerTemplate
{
    private bool _hooked;
    private bool _processing;

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
        Originalsin.PunishTriggered += OnOriginalsinTriggered;
        Originalsin.ForgiveTriggered += OnOriginalsinTriggered;
        _hooked = true;
    }

    private void Unhook()
    {
        if (!_hooked) return;
        Originalsin.PunishTriggered -= OnOriginalsinTriggered;
        Originalsin.ForgiveTriggered -= OnOriginalsinTriggered;
        _hooked = false;
    }

    private async void OnOriginalsinTriggered(PlayerChoiceContext choiceContext, CardModel card)
    {
        try
        {
            if (_processing) return;
            if (Owner?.Player is not { } player) return;
            if (card.Owner?.Creature != Owner) return;

            _processing = true;
            try
            {
                // 1. 若该原罪因此进了弃牌堆或消耗堆，改放抽牌堆顶部
                if (card.Pile?.Type is PileType.Discard or PileType.Exhaust)
                {
                    await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Top);
                }

                // 2. 按当前牌组（抽+手+弃）内原罪卡数量，随机触发等量次「卡面效果」
                var pool = PileType.Draw.GetPile(player).Cards
                    .Concat(PileType.Hand.GetPile(player).Cards)
                    .Concat(PileType.Discard.GetPile(player).Cards)
                    .Where(c => c.HasComponent<Originalsin>())
                    .ToList();
                var count = pool.Count;
                if (count <= 0) return;

                var rng = player.RunState.Rng.CombatCardGeneration;
                for (var i = 0; i < count; i++)
                {
                    if (pool.Count == 0) break;
                    var pick = rng.NextItem(pool);
                    if (pick is null) continue;
                    var component = (pick as IComponentsCardModel)?.GetComponent<Originalsin>();
                    if (component is null) continue;

                    // 每次随机触发宽恕或自惩效果之一
                    if (rng.NextInt(2) == 0)
                        await component.Forgive(choiceContext);
                    else
                        await component.Punish(choiceContext);
                }
            }
            finally
            {
                _processing = false;
            }
        }
        catch (Exception ex)
        {
            Log.Error($"XunHuanFaDianPower.OnOriginalsinTriggered failed: {ex}");
        }
    }
}