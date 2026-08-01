using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System;
using System.Collections.Generic;
using System.Linq;
using ManosabaLin.Characters.Common.HiroKeywords;
using STS2RitsuLib.Keywords;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(LinCardPool))]
public sealed class Hirodeath : ManosabaCardTemplate
{
    private const int CycleThreshold = 20;
    private const int CopiesToAdd = 2;

    public Hirodeath() : base(-1, CardType.Skill, CardRarity.Ancient, TargetType.AllAllies)
    {
    }

    public override int MaxUpgradeLevel => 0;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DynamicVar("CycleThreshold", CycleThreshold);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        // 获取当前正义层数
        var justicePower = source.Owner.Creature.GetPower<JusticePower>();
        var justiceAmount = (int)(justicePower?.Amount ?? 0);
        var justiceToGive = justiceAmount / 2;

        // 统计自己的轮回关键词个数
        var rebirthId = TransmigrationRules.TransmigrationCardKeyword;
        var cycleCount = source.Owner.PlayerCombatState.AllCards
            .Count(c => c.HasModKeyword(rebirthId));

        // 计算影响卡牌数：每20张轮回卡影响1张
        var cardsToAffect = Math.Max(1, cycleCount / CycleThreshold + 1);

        // 对全体队友生效
        var teammates = source.CombatState.GetTeammatesOf(source.Owner.Creature)
            .Where(c => c != null && c.IsAlive && c.IsPlayer);

        foreach (var teammate in teammates)
        {
            // 消耗50层魔女化
            var withPower = teammate.GetPower<WithPower>();
            if (withPower != null && withPower.Amount > 0)
            {
                var withToRemove = Math.Min(50, (int)withPower.Amount);
                await PowerCmd.ModifyAmount(choiceContext, withPower, -withToRemove, source.Owner.Creature, source, false);
            }

            // 消耗3层嫌疑
            var suspectPower = teammate.GetPower<SuspectPower>();
            if (suspectPower != null && suspectPower.Amount > 0)
            {
                var suspectToRemove = Math.Min(3, (int)suspectPower.Amount);
                await PowerCmd.ModifyAmount(choiceContext, suspectPower, -suspectToRemove, source.Owner.Creature, source, false);
            }

            // 给予正义能力
            if (justiceToGive > 0)
            {
                await PowerCmd.Apply<JusticePower>(
                    choiceContext,
                    teammate,
                    justiceToGive,
                    source.Owner.Creature,
                    source,
                    false
                );
            }

            // 从抽牌堆随机选卡添加轮回关键词并复制
            if (teammate.Player == null) continue;
            var drawPile = PileType.Draw.GetPile(teammate.Player);
            var eligibleCards = drawPile.Cards
                .Where(c => !c.HasModKeyword(rebirthId))
                .ToList();

            if (eligibleCards.Count == 0) continue;

            var rng = teammate.Player.RunState.Rng.CombatCardSelection;
            var selectedCards = eligibleCards
                .OrderBy(_ => rng.NextFloat())
                .Take(cardsToAffect)
                .ToList();

            foreach (var card in selectedCards)
            {
                // 给原卡添加轮回关键词
                card.AddModKeyword(rebirthId);

                // 添加2张相同卡进入抽牌堆
                for (int i = 0; i < CopiesToAdd; i++)
                {
                    var clone = CombatState.CreateCard(card.CanonicalInstance, teammate.Player);
                    clone.AddModKeyword(rebirthId);
                    await CardPileCmd.AddGeneratedCardToCombat(clone, PileType.Draw, teammate.Player, CardPilePosition.Random);
                }
            }
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
    }
}
