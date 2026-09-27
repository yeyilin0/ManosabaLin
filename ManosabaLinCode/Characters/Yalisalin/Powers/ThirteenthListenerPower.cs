using ManosabaLin.Characters.Yalisalin.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Yalisalin.Powers;

/// <summary>
/// 第十三格旁听：每当触发火色「连续」，额外消耗同一名敌人层数格火色，并抽层数张牌。
/// 额外消耗照常进入连续链，可能再次凑成连续；量表有限，链条总会停下。
/// </summary>
[RegisterPower]
public sealed class ThirteenthListenerPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public async Task OnContinuousTriggered(
        PlayerChoiceContext choiceContext,
        Creature target,
        CardModel? source = null)
    {
        if (Owner.Player is not { } player
            || !YalisalinFireColorSystem.TryGetHairpin(player, out var hairpin))
            return;

        Flash();
        await CardPileCmd.Draw(choiceContext, (int)Amount, player);
        await hairpin.ConsumeFireColor(choiceContext, target, (int)Amount, source);
    }
}
