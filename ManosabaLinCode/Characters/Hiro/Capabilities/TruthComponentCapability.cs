using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Capabilities;

/// <summary>
/// 真相组件（真理之证）：标记卡牌"可在弃牌堆被【轮回】自动打出"。
/// 带轮回关键词且拥有本组件的卡被自动打出时，获得 1 点能量。
/// 若持有者拥有【真相能力】，则该 +1 能量被压制（由真相能力统一结算）。
/// 由 TransmigrationRules 在搜索可自动打出的同名卡时检查本组件。
/// </summary>
[RegisterModelCapability]
public sealed class TruthComponentCapability : ManosabaCardCapability
{
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner is not { } ownerCard) return;
        if (cardPlay.Card != ownerCard) return;
        if (!cardPlay.IsAutoPlay) return;
        if (cardPlay.PlayIndex != 0) return;
        if (ownerCard.Owner is not { } player) return;
        if (TruthPower.HasTruthPower(player)) return; // 真相能力压制本组件自身的 +1 能量

        await PlayerCmd.GainEnergy(1m, player);
    }
}
