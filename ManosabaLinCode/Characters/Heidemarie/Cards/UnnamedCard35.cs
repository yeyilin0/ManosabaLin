using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Heidemarie.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MinionLib.Component.Core;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Heidemarie.Cards;

[RegisterCard(typeof(HeidemarieCardPool))]
public sealed class UnnamedCard35() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
{
    public const string MarkVar = nameof(MarkPower);

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<MarkPower>(1m)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var owner = Owner.Creature;
        if (owner.GetPower<MarkPower>() == null)
            return;

        await PowerCmd.Apply<MarkPower>(choiceContext, owner, DynamicVars[MarkVar].BaseValue, owner, this);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars[MarkVar].UpgradeValueBy(1m);
    }
}
