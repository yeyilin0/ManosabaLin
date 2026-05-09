// WitchSlayerPower.cs
using STS2RitsuLib.Interop.AutoRegistration;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using System.Threading.Tasks;
using ManosabaLin.Characters.Common;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public class AmmPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner.Creature != Owner) return;
        if (cardPlay.Card.Type != CardType.Attack) return;
        if (cardPlay.Target == null || !cardPlay.Target.IsEnemy || !cardPlay.Target.IsAlive) return;

        var with = Owner.GetPower<WithPower>();
        var withAmount = with?.Amount ?? 0;

        var threshold = (int)(withAmount / 4);

        if (cardPlay.Target.CurrentHp <= threshold && cardPlay.Target.CurrentHp > 0)
        {
            Flash();

            await CreatureCmd.Damage(
                choiceContext,
                cardPlay.Target,
                99999m,
                ValueProp.Unblockable | ValueProp.Unpowered,
                Owner,
                cardPlay.Card
            );

            await PowerCmd.Apply<WithPower>(
                choiceContext, Owner, 10,
                Owner, null, false
            );
        }
    }
}