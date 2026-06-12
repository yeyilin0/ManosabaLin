using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Cards;

/// <summary>
/// 案卷调取：选择额外牌堆1张卡加入手牌，升级减一费
/// </summary>
[RegisterCard(typeof(SherrylinCardPool))]
public sealed class CaseFileRetrieval() : ManosabaCardTemplate(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;

        await CreatureCmd.TriggerAnim(source.Owner.Creature, "Cast", source.Owner.Character.CastAnimDelay);

        var caseFilePile = MainFile.CaseFilePile.GetPile(source.Owner);
        if (caseFilePile.Cards.Count == 0) return;

        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1);
        var selected = await CardSelectCmd.FromCombatPile(choiceContext, caseFilePile, source.Owner, prefs);
        var selectedList = selected.ToList();
        if (selectedList.Count > 0)
        {
            var card = selectedList[0];
            await CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Top, source);
        }
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        EnergyCost.UpgradeBy(-1);
    }
}
