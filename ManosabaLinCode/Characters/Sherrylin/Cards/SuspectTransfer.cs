using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;

namespace ManosabaLin.Characters.Sherrylin.Cards;

/// <summary>
/// 嫌疑转移：令友方全体失去一层嫌疑，将等量嫌疑转移至任意敌方
/// </summary>
[RegisterCard(typeof(SherrylinCardPool))]
public sealed class SuspectTransfer() : ManosabaCardTemplate(2, CardType.Attack, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<SuspectPower>(); }
    }

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var combatState = source.CombatState;
        if (combatState == null) return;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        var allies = combatState.Allies.Where(a => a is { IsAlive: true }).ToList();
        int totalRemoved = 0;

        foreach (var ally in allies)
        {
            var allySuspect = ally.GetPower<SuspectPower>();
            if (allySuspect != null && allySuspect.Amount > 0)
            {
                await PowerCmd.ModifyAmount(choiceContext, allySuspect, -1,
                    source.Owner.Creature, source, false);
                totalRemoved++;
            }
        }

        if (totalRemoved > 0)
        {
            var enemies = combatState.HittableEnemies.Where(e => e.IsAlive).ToList();
            if (enemies.Count > 0)
            {
                var rng = source.Owner.RunState.Rng.CombatCardSelection;
                var enemy = enemies[rng.NextInt(enemies.Count)];
                await PowerCmd.Apply<SuspectPower>(
                    choiceContext, enemy, totalRemoved,
                    source.Owner.Creature, source, false);
            }
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
