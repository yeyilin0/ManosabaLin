using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Relics;

[RegisterRelic(typeof(HiroRelicPool))]
[RegisterCharacterStarterRelic(typeof(Hiro))]
[RegisterTouchOfOrobasRefinement(typeof(Withhiro))]
public sealed class Pen : ManosabaRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            return new[]
            {
                new PowerVar<JusticePower>(3m)
            };
        }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<JusticePower>(); }
    }

    public override async Task
        AfterSideTurnStart(CombatSide side, ICombatState combatState) // CombatState → ICombatState
    {
        var relic = this;

        if (side == relic.Owner.Creature.Side && combatState.RoundNumber == 1)
        {
            relic.Flash();

            await PowerCmd.Apply<JusticePower>(
                new ThrowingPlayerChoiceContext(), // ★ 第一个参数
                relic.Owner.Creature,
                relic.DynamicVars["JusticePower"].BaseValue,
                relic.Owner.Creature,
                null
            );
        }
    }
}