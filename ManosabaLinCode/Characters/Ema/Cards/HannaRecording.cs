using MinionLib.Component.Core;
using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Emalin;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using STS2RitsuLib.Keywords;

namespace ManosabaLin.Characters.Ema.Cards;

/// <summary>汉娜的录音数据 - 1费技能, 疑问附魔, 获得1能量, 选择弃牌堆一张再次打出, 升级获得3能量</summary>
[RegisterCard(typeof(EmalinCardPool))]
public sealed class HannaRecording : ManosabaCardTemplate
{
    public HannaRecording() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        new[] { EmalinKeywordRules.DoubtKeywordId.GetModCardKeyword() };

    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);

        var discardPile = PileType.Discard.GetPile(Owner);
        if (discardPile.Cards.Count == 0) return;

        var prefs = new CardSelectorPrefs(SelectionScreenPrompt, 1, 1);
        var selected = await CardSelectCmd.FromSimpleGrid(choiceContext, discardPile.Cards, Owner, prefs);
        var card = selected.FirstOrDefault();
        if (card != null)
            await CardCmd.AutoPlay(choiceContext, card, null);
    }

    protected override void OnUpgrade(ComponentContext componentContext)
    {
        DynamicVars.Energy.UpgradeValueBy(2);
    }
}
