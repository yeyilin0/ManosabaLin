using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Ema.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;

namespace ManosabaLin.Characters.Ema.Cards;

[RegisterCard(typeof(LinCardPool))]
public sealed class Emamonvqinjin : ManosabaEmalinCardTemplate
{
    public Emamonvqinjin() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self) { }

    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Minion };

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            yield return CardKeyword.Exhaust;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var bond = Owner.Creature.GetPower<BondPower>();
        if (bond != null) bond.Affinity++;
    }

    protected override void OnUpgrade()
    {
    }
}