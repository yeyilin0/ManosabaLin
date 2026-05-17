using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(LinCardPool))]
public sealed class BulletCard() : ManosabaCardTemplate(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(13m, ValueProp.Move),
        new CardsVar(1)
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var target = cardPlay.Target;
        ArgumentNullException.ThrowIfNull(target);

        await DamageCmd.Attack(source.DynamicVars.Damage.BaseValue)
            .FromCard(source)
            .Targeting(target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        await CardPileCmd.Draw(choiceContext, source.DynamicVars.Cards.BaseValue, source.Owner);

        await PlayerCmd.GainEnergy(1m, source.Owner);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
