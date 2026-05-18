using MinionLib.Component.Core;
using ManosabaLin.Audio;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using ManosabaLin.Characters.Emalin;
using ManosabaLin.Characters.Hiro.Powers;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(EmalinCardPool))]
public sealed class Emawichpower : ManosabaEmalinCardTemplate
{
    private const int RequiredWitchFactorAmount = 100;

    public Emawichpower() : base(3, CardType.Skill, CardRarity.Ancient, TargetType.Self)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<RitualCeremonyPower>(2m),
        new PowerVar<IntangiblePower>(1m),
        new PowerVar<WithPower>(100m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<WithPower>();
            yield return HoverTipFactory.FromPower<RitualCeremonyPower>();
            yield return HoverTipFactory.FromPower<IntangiblePower>();
            yield return HoverTipFactory.FromPower<ReaperFormPower>();
            yield return HoverTipFactory.FromKeyword(CardKeyword.Exhaust);
        }
    }

    protected override bool IsPlayableC
    {
        get
        {
            if (!base.IsPlayableC)
                return false;

            var witchFactor = Owner.Creature.GetPower<WithPower>();
            var witchFactorAmount = witchFactor?.Amount ?? 0;

            return witchFactorAmount >= RequiredWitchFactorAmount;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        ManosabaAudio.TryPlayOneShot("ema_witch_form_theme.mp3".BgmAudioPath());

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        // 获得2层魔女仪式
        await PowerCmd.Apply<RitualCeremonyPower>(
            choiceContext, source.Owner.Creature,
            source.DynamicVars["RitualCeremonyPower"].BaseValue,
            source.Owner.Creature,
            source,
            false
        );

        // 获得1层无实体
        await PowerCmd.Apply<IntangiblePower>(
            choiceContext, source.Owner.Creature,
            source.DynamicVars["IntangiblePower"].BaseValue,
            source.Owner.Creature,
            source,
            false
        );

        // 获得 ReaperFormPower
        await PowerCmd.Apply<ReaperFormPower>(
            choiceContext, source.Owner.Creature,
            1m,
            source.Owner.Creature,
            source,
            false
        );
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}