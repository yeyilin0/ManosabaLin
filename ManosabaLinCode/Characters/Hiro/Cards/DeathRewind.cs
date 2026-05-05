using ManosabaLin.Audio;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public class DeathRewind() : ManosabaCardTemplate(3, CardType.Power, CardRarity.Ancient, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<DeathRewindPower>();
            yield return HoverTipFactory.FromPower<WithPower>();
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<WithPower>(50m)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ManosabaAudio.TryPlayOneShot("death_rewind_theme.mp3".BgmAudioPath());

        await PowerCmd.Apply<DeathRewindPower>(choiceContext, Owner.Creature, 1m, Owner.Creature,
            (CardModel)this, false);
        await PowerCmd.Apply<WithPower>(choiceContext, Owner.Creature, DynamicVars["WithPower"].BaseValue,
            Owner.Creature, (CardModel)this, false);
    }

    public static CardModel ToCardModel(DeathRewind v)
    {
        throw new NotImplementedException();
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}