using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Linq;
using ManosabaLin.Characters.Ema.Powers;
using ManosabaLin.Characters.Emalin;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ManosabaLin.Characters.Ema.Cards;

/// <summary>监牢的设计图 - 2费能力, 赞同附魔, 每打出赞同牌获得1格挡, 同伴获得2格挡</summary>
[RegisterCard(typeof(EmalinCardPool))]
public sealed class PrisonBlueprint : ManosabaEmalinCardTemplate
{
    public PrisonBlueprint() : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<string> RegisteredKeywordIds =>
        new[] { EmalinKeywordRules.AgreeKeywordId };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<PrisonBlueprintPower>(
            choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}