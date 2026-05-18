using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.HiroKeywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public class CardSixtyTwo() : ManosabaCardTemplate(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{
    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Strike };

    // 固定基础伤害，不再用 IsUpgraded 判断
    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new DamageVar(4m, ValueProp.Move)
    };

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [TransmigrationRules.TransmigrationKeywordId.GetModCardKeyword()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target);

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        // 升级：伤害 +3
        DynamicVars.Damage.BaseValue += 3; // 4 → 7
    }
}
