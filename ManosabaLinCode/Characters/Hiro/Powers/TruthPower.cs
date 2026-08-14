using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Capabilities;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models.Capabilities;

namespace ManosabaLin.Characters.Hiro.Powers;

/// <summary>
/// 真相能力：
/// 1. 当卡牌获得【轮回】关键词时，自动追加真相组件（见 SamePlaceTrace.GrantTransmigrationToHandCard）。
/// 2. 压制真相组件的"自动打出时获得 1 点能量"，使其无效。
/// 3. 每自动打出 3 张带真相组件的卡：获得 1 点能量，抽 2 张卡。
/// </summary>
[RegisterPower]
public sealed class TruthPower : ManosabaPowerTemplate
{
    private const string AutoPlayCountKey = "AutoPlayCount";
    private const int AutoPlayRequirement = 3;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new IntVar(AutoPlayCountKey, 0)
    ];

    public static bool HasTruthPower(Player player)
    {
        return player != null && player.Creature.GetPower<TruthPower>() != null;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner is not { } creature) return;
        if (cardPlay.Card is not { Owner: { } ownerPlayer } card) return;
        if (ownerPlayer.Creature != creature) return;
        if (!cardPlay.IsAutoPlay) return;
        if (cardPlay.PlayIndex != 0) return;
        if (!card.HasModKeyword(TransmigrationRules.TransmigrationCardKeyword)) return;
        if (!card.TryGetCapability<TruthComponentCapability>(out _)) return;

        var count = (int)(DynamicVars[AutoPlayCountKey].BaseValue + 1);
        if (count < AutoPlayRequirement)
        {
            DynamicVars[AutoPlayCountKey].BaseValue = count;
            return;
        }

        DynamicVars[AutoPlayCountKey].BaseValue = 0;
        Flash();

        await PlayerCmd.GainEnergy(1m, ownerPlayer);
        await CardPileCmd.Draw(choiceContext, 2m, ownerPlayer);
    }
}
