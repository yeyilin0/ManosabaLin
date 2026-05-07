using ManosabaLin.Audio;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using ManosabaLin.ManosabaLinCode.Characters.Hiro.Cards;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class Powerthreethreecard() : ManosabaCardTemplate(1, CardType.Power, CardRarity.Rare, TargetType.AnyPlayer)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromCard<LyXl>();
            yield return HoverTipFactory.FromPower<Powerthreethree>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var source = this;
        var target = cardPlay.Target ?? source.Owner.Creature;

        ManosabaAudio.TryPlayOneShot("power_three_three.wav".CardsAudioPath(), 0.8f);

        var token = CombatState.CreateCard<LyXl>(target.Player);
        await CardPileCmd.AddGeneratedCardToCombat(token, PileType.Hand, target.Player);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}