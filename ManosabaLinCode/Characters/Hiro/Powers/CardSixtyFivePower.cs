using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Powers;

[RegisterPower]
public sealed class CardSixtyFivePower : ManosabaPowerTemplate
{
    private const int BaseCardsLeft = 10;
    private const string BaseCardsKey = "BaseCards";

    private int _cardsLeft = BaseCardsLeft;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override int DisplayAmount => _cardsLeft;
    public override bool IsInstanced => true;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new DynamicVar(BaseCardsKey, BaseCardsLeft); }
    }

    public override async Task AfterCardDrawn(
        PlayerChoiceContext choiceContext,
        CardModel card,
        bool fromHandDraw)
    {
        var source = this;

        // 只处理持有者抽到的牌
        if (card.Owner != source.Owner.Player)
            return;

        _cardsLeft--;
        InvokeDisplayAmountChanged();

        if (_cardsLeft > 0)
            return;

        source.Flash();
        await PlayerCmd.GainEnergy(source.Amount, source.Owner.Player);
        _cardsLeft = BaseCardsLeft;
        InvokeDisplayAmountChanged();
    }
}