using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using ManosabaLin.Characters.Emalin;

namespace ManosabaLin.Characters.Ema.Cards;

/// <summary>共犯的沉默 - 2费技能, 1能量, 打过赞同和反驳额外全体1能量, 升级1费</summary>
[RegisterCard(typeof(EmalinCardPool))]
public sealed class AccompliceSilence : ManosabaCardTemplate
{
    public AccompliceSilence() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        await PlayerCmd.GainEnergy(1m, Owner);

        if (EmalinCombatHelper.HasPlayedBothAgreementAndRebuttal(Owner.Creature, CombatState))
        {
            foreach (var player in CombatState.Players)
                await PlayerCmd.GainEnergy(1m, player);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
