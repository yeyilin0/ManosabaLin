using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ManosabaLin.Characters.Common.HiroKeywords;
using ManosabaLin.Characters.Hiro.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Common.LinRelics;

/// <summary>
///     联动遗物 4：打出【轮回】卡时，**改为**自动打出抽牌堆里**任意两张**【轮回】卡。
///     <para>
///         原版逻辑（<c>TransmigrationRules.TriggerTransmigrationEffect</c>）打的是**同名**卡，且会退化到弃牌堆找。
///         本遗物把整条结算换成「抽牌堆里任意两张」；由 <c>HextechTransmigrationEchoPatch</c> 以 prefix
///         拦掉 <c>TransmigrationSingleton.AfterCardPlayed</c> 来接管。
///     </para>
///     <para>
///         随机源一律走 <c>Owner.RunState.Rng.CombatCardSelection</c>（联机确定性要求）。
///     </para>
/// </summary>
[RegisterRelic(typeof(LinRelicPool))]
public sealed class HextechTransmigrationEcho : ManosabaRelicTemplate
{
    private const int CopiesToPlay = 2;

    public override RelicRarity Rarity => RelicRarity.Starter;

    /// <summary>被 patch 调用来替代原版轮回结算。</summary>
    internal async Task PlayRandomTransmigrationCards(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var source = cardPlay.Card;

        var candidates = PileType.Draw.GetPile(Owner).Cards
            .Where(c => c != source
                        && TransmigrationRules.HasTransmigration(c)
                        && c is not SamePlaceTrace { JustAddedToDrawPile: true })
            .ToList();

        if (candidates.Count == 0) return;

        Flash();

        var rng = Owner.RunState.Rng.CombatCardSelection;
        var isFirst = true;

        for (var i = 0; i < CopiesToPlay && candidates.Count > 0; i++)
        {
            var pick = rng.NextItem(candidates);
            if (pick is null) break;

            candidates.Remove(pick);

            // 候选是触发开始时一次性快照的，可能已被前面的结算移出战斗；
            // 此时再 AutoPlay 会让引擎在 OnPlayWrapper 里「PushModel 之后提前 return」→ 栈残留。
            // 详见 TransmigrationRules.CanStillAutoPlay 的注释。
            if (!TransmigrationRules.CanStillAutoPlay(pick))
            {
                continue;
            }

            pick.SetToFreeThisTurn();

            await CardCmd.AutoPlay(choiceContext, pick, cardPlay.Target, skipCardPileVisuals: !isFirst);

            isFirst = false;
            await Cmd.Wait(0.1f);
        }
    }
}
