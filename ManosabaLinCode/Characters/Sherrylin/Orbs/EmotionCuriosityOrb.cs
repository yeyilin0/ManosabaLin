using Godot;
using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;
using ManosabaLin.Characters.Sherrylin.Relics;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Sherrylin.Orbs;

/// <summary>
/// 好奇球体：本回合触发翻案时，获得等于弃牌堆进消耗堆数量的魔法，抽魔法/4张卡，获得消耗堆/4能量。
/// </summary>
[RegisterOrb]
public sealed class EmotionCuriosityOrb : EmotionOrb<EmotionCuriosity>
{
    protected override Color OrbColor => new(0.6f, 0.9f, 0.6f);

    private bool _bonusApplied;

    public override async Task AfterTurnStartOrbTrigger(PlayerChoiceContext ctx)
    {
        // 消散前检查是否触发过翻案
        var relic = Owner.Relics.OfType<MagnifyingGlass>().FirstOrDefault();
        if (relic != null && relic.CaseReversalDiscardToExhaustCount > 0 && !_bonusApplied)
        {
            _bonusApplied = true;
            var count = relic.CaseReversalDiscardToExhaustCount;
            relic.CaseReversalDiscardToExhaustCount = 0;

            // 获得等于弃牌堆进消耗堆数量的魔法
            await PowerCmd.Apply<XlmPower>(
                ctx, Owner.Creature, count, Owner.Creature, null, false);

            // 抽魔法/4张卡（仅1次）
            var xlmPower = Owner.Creature.GetPower<XlmPower>();
            if (xlmPower != null)
            {
                var drawCount = xlmPower.Amount / 4;
                if (drawCount > 0)
                    await CardPileCmd.Draw(ctx, drawCount, Owner);
            }

            // 获得消耗堆/4能量（仅1次）
            var exhaustCount = PileType.Exhaust.GetPile(Owner).Cards.Count;
            var energyGain = exhaustCount / 4;
            if (energyGain > 0)
                await PlayerCmd.GainEnergy(energyGain, Owner);
        }

        await base.AfterTurnStartOrbTrigger(ctx);
    }

    public override Task Passive(PlayerChoiceContext ctx, Creature? target) => Task.CompletedTask;
}
