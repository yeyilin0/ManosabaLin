using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Relics;

[RegisterCharacterStarterRelic(typeof(Sherrylin))]
[RegisterRelic(typeof(SherrylinRelicPool))]
public sealed class WitchificationEmotionRelic : ManosabaRelicTemplate
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    private bool _givenThisCombat;

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        _givenThisCombat = false;
    }

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amountChanged,
        Creature? applier,
        CardModel? cardSource)
    {
        if (_givenThisCombat) return;
        if (power.Owner != Owner.Creature) return;
        if (power is not WithPower) return;
        if (power.Amount < 100) return;

        _givenThisCombat = true;
        Flash();

        var combatState = Owner.Creature?.CombatState;
        if (combatState == null) return;

        var newCard = combatState.CreateCard<WitchificationEmotion>(Owner);
        await CardPileCmd.Add(newCard, MainFile.CaseFilePile, CardPilePosition.Top);
    }
}
