using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Cards;

/// <summary>
/// 循环心绪：当你情绪额外牌堆有牌的时候打出可以使你抽牌堆一张卡获得重放，升级重放2
/// </summary>
[RegisterCard(typeof(SherrylinCardPool))]
public sealed class CycleMind() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Replay", 1m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<EmotionPower>(); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        var caseFileCards = MainFile.CaseFilePile.GetPile(source.Owner).Cards.ToList();
        if (caseFileCards.Count == 0) return;

        var drawPile = PileType.Draw.GetPile(source.Owner).Cards.ToList();
        if (drawPile.Count == 0) return;

        var rng = source.Owner.RunState.Rng.CombatCardSelection;
        var targetCard = drawPile[rng.NextInt(drawPile.Count)];

        var replayAmount = source.DynamicVars["Replay"].IntValue;
        targetCard.BaseReplayCount += replayAmount;
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars["Replay"].UpgradeValueBy(1m);
    }
}
