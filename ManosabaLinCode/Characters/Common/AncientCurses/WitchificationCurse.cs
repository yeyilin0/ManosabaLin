using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.Components;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MinionLib.Component.Interfaces;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Common.AncientCurses;

/// <summary>
/// 魔女化诅咒卡：白字灰描边（#cccccc）的先古诅咒卡，类型格子与其他原罪诅咒一致。
/// 打出时获得 20 魔女化。
/// <b>本身不带原罪组件</b>：由魔女因子（<c>YalisalinWitchFactor</c>）发放的这张卡会主动挂载
/// <see cref="Originalsin"/>（卡面显示 [color=#cccccc][b]原罪[/b][/color]），
/// 宽恕/自惩的专属效果见 Originalsin 中对应 case。
/// 卡名描边灰色由 LinAncientCurseTitleColors 提供。
/// </summary>
[RegisterCard(typeof(LinCardPool))]
public sealed class WitchificationCurse : LinAncientCurseCard
{
    public WitchificationCurse() : base(1, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new PowerVar<WithPower>("WitchAmount", 20m); }
    }

    // 打出：获得 20 魔女化（宽恕成长后数值会 +10 累加）
    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var source = this;
        if (source.Owner is not { } owner) return;

        await PowerCmd.Apply<WithPower>(
            choiceContext,
            owner.Creature,
            DynamicVars["WitchAmount"].BaseValue,
            owner.Creature,
            source,
            false);
    }
}
