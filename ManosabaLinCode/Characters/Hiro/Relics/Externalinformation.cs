using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;

namespace ManosabaLin.Characters.Hiro.Relics;

[RegisterRelic(typeof(HiroRelicPool))]
public sealed class Externalinformation : ManosabaRelicTemplate
{
    public const int MaxHpLoss = 3;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new CardsVar(1); }
    }

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        var relic = this;

        var prefs = new CardSelectorPrefs(
            CardSelectorPrefs.UpgradeSelectionPrompt,
            0,
            relic.DynamicVars.Cards.IntValue);

        var selected = await CardSelectCmd.FromDeckForUpgrade(relic.Owner, prefs);
        var cards = selected.ToList();

        if (cards.Count == 0) return;

        Flash();

        foreach (var card in cards)
            CardCmd.Upgrade(card);

        await CreatureCmd.LoseMaxHp(
            new BlockingPlayerChoiceContext(),
            relic.Owner.Creature,
            MaxHpLoss,
            false);
    }
}
