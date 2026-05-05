using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class CardTwentyTwo() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            return new[]
            {
                CardKeyword.Innate,
                CardKeyword.Exhaust
            };
        }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<JusticePower>(); }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<JusticePower>(1m),
        new CardsVar(1),
        new EnergyVar(1)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        // 获得正义
        await PowerCmd.Apply<JusticePower>(
            choiceContext, source.Owner.Creature,
            source.DynamicVars["JusticePower"].BaseValue,
            source.Owner.Creature,
            source,
            false
        );

        // 抽牌
        await CardPileCmd.Draw(choiceContext, source.DynamicVars.Cards.BaseValue, source.Owner);

        // 回复能量
        await PlayerCmd.GainEnergy(source.DynamicVars.Energy.BaseValue, source.Owner);
    }

    protected override void OnUpgrade()
    {
        base.OnUpgrade();
        DynamicVars["JusticePower"].BaseValue = 3m;
        DynamicVars.Cards.BaseValue = 3;
        DynamicVars.Energy.BaseValue = 3;
    }
}