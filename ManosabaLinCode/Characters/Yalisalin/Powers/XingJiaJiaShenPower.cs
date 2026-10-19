using ManosabaLin.Characters.Common.Components;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using System;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Yalisalin.Powers;

/// <summary>
/// 刑枷加身：每回合第一张原罪诅咒卡牌打出后，减一费并添加消耗，然后加入手牌。
/// 升级：作用于每回合前两张。
/// </summary>
[RegisterPower]
public sealed class XingJiaJiaShenPower : ManosabaPowerTemplate
{
    private bool _hooked;
    private int _processedThisTurn;

    /// <summary>升级版每回合处理的原罪张数。</summary>
    public int CountPerTurn { get; set; } = 1;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    ///     描述随「来源牌是否升级」切键（升级也是一种达成条件）。两侧通道都切，做法对齐「共犯」/
    ///     被缚的普罗米修斯。
    /// </summary>
    public override LocString Description =>
        new LocString("powers",
            CountPerTurn >= 2 ? $"{Id.Entry}.descriptionEnhanced" : $"{Id.Entry}.description");

    protected override string SmartDescriptionLocKey =>
        CountPerTurn >= 2 ? $"{Id.Entry}.smartDescriptionEnhanced" : $"{Id.Entry}.smartDescription";

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

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player?.Creature == Owner)
            _processedThisTurn = 0;
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
            if (_processedThisTurn >= CountPerTurn) return;

            _processedThisTurn++;

            // 减一费
            punished.EnergyCost.UpgradeBy(-1);

            // 添加消耗
            if (!punished.Keywords.Contains(CardKeyword.Exhaust))
                punished.AddKeyword(CardKeyword.Exhaust);

            // 加入手牌
            await CardPileCmd.Add(punished, PileType.Hand);
        }
        catch (Exception ex)
        {
            Log.Error($"XingJiaJiaShenPower.OnPunishTriggered failed: {ex}");
        }
    }
}