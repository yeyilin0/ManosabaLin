using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Yalisalin.Powers;

/// <summary>
/// 没被采用的结论：每回合第一次触发火色「连续」时，获得层数点能量并抽层数张牌。
/// 由发夹在凑成同色连续后调用 <see cref="OnContinuousTriggered" />。
/// </summary>
[RegisterPower]
public sealed class MixedConclusionPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private bool _usedThisTurn;

    public async Task OnContinuousTriggered(PlayerChoiceContext choiceContext)
    {
        if (_usedThisTurn || Owner.Player is not { } player)
            return;

        _usedThisTurn = true;
        Flash();
        await PlayerCmd.GainEnergy((int)Amount, player);
        await CardPileCmd.Draw(choiceContext, (int)Amount, player);
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player == Owner.Player)
            _usedThisTurn = false;

        return Task.CompletedTask;
    }
}
