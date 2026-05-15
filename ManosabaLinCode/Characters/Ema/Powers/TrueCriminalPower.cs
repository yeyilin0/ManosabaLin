using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Emalin.Enchantments;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;

namespace ManosabaLin.Characters.Ema.Powers;

[RegisterPower]
public class TrueCriminalPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private int _consecutiveCount;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature != Owner) return;

        if (cardPlay.Card.Enchantment is Rebuttal or Agreement or Doubt)
        {
            _consecutiveCount++;
            if (_consecutiveCount % 5 == 0 && _consecutiveCount > 0)
            {
                Flash();
                // 对生命值最低的敌人造成15点直接伤害（无视护盾）
                var lowestHpEnemy = Owner.CombatState.HittableEnemies
                    .Where(e => e.IsAlive)
                    .OrderBy(e => e.CurrentHp)
                    .FirstOrDefault();
                if (lowestHpEnemy != null)
                {
                    await CreatureCmd.Damage(choiceContext, lowestHpEnemy, 15m,
                        ValueProp.Unblockable | ValueProp.Unpowered, Owner, null);
                }
            }
        }
        else
        {
            _consecutiveCount = 0;
        }
    }
}
