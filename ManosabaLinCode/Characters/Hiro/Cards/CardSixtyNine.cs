using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Common.HiroKeywords;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class CardSixtyNine() : ManosabaCardTemplate(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    // 固定基础值
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1),
        new EnergyVar(1)
    ];

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [TransmigrationRules.TransmigrationKeywordId.GetModCardKeyword()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        // 抽牌
        await CardPileCmd.Draw(choiceContext, source.DynamicVars.Cards.BaseValue, source.Owner);

        // 回复能量
        await PlayerCmd.GainEnergy(source.DynamicVars.Energy.BaseValue, source.Owner);

        // 失去轮回关键词
        source.RemoveModKeyword(TransmigrationRules.TransmigrationKeywordId);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
        DynamicVars.Energy.UpgradeValueBy(1m);
    }
}
