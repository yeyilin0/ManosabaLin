using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Sherrylin.Powers;

/// <summary>
/// 悲伤能力：每打出一张卡获得1点护盾，回复1点生命。
/// </summary>
[RegisterPower]
public sealed class EmotionSadnessPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterCardPlayed(
        PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner?.Creature != Owner) return;

        await CreatureCmd.GainBlock(Owner, 1m, ValueProp.Unpowered, null);

        if (Owner.CurrentHp < Owner.MaxHp)
            await CreatureCmd.Heal(Owner, 1m);
    }
}
