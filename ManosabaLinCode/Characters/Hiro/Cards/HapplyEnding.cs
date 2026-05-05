using ManosabaLin.Audio;
using ManosabaLin.Characters.Common;
using ManosabaLin.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(HiroCardPool))]
public sealed class HappyEnding : ManosabaCardTemplate
{
    public HappyEnding() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override HashSet<CardTag> CanonicalTags => new();

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromCard<Error>(); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var source = this;

        ManosabaAudio.TryPlayOneShot("happy_ending_theme.mp3".BgmAudioPath());

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        var errorCard = source.CombatState.CreateCard<Error>(source.Owner);
        await CardPileCmd.AddGeneratedCardToCombat(errorCard, PileType.Hand, source.Owner);

        await Cmd.Wait(0.2f);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}