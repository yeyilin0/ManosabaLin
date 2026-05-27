using ManosabaLin.Characters.Common.Components.Abstracts;
using ManosabaLin.Characters.Emalin.Enchantments;
using ManosabaLin.Characters.Ema.Relics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Component.Core;
using System.Linq;

namespace ManosabaLin.Characters.Emalin.Components;

public sealed partial class AgreementTrialComponent : KeywordLikeComponent
{
    public override async Task OnPlayPostfix(PlayerChoiceContext choiceContext, CardPlay cardPlay,
        ComponentContext componentContext)
    {
        var owner = Card?.Owner;
        if (owner == null) return;

        var badge = owner.Relics.OfType<EmaTrialBadge>().FirstOrDefault();
        if (badge == null) return;

        badge.IncrementCount(ModelDb.Enchantment<Agreement>());
    }
}
