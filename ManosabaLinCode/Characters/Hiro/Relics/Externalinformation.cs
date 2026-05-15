using ManosabaLin.Characters.Common;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rooms;

namespace ManosabaLin.Characters.Hiro.Relics;

[RegisterRelic(typeof(HiroRelicPool))]
public sealed class Externalinformation : ManosabaRelicTemplate
{
    public const int MaxHpLossPerCard = 2;
    private bool _pendingUpgrade;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new CardsVar(3); }
    }

    [SavedProperty]
    public bool PendingUpgrade
    {
        get => _pendingUpgrade;
        set
        {
            AssertMutable();
            _pendingUpgrade = value;
        }
    }

    // 战斗结束时标记"待升级"
    public override async Task AfterCombatEnd(CombatRoom room)
    {
        PendingUpgrade = true;
        Flash();
    }

    // 下一回合开始时弹窗
    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext,
        Player player)
    {
        if (player != Owner) return;
        if (!PendingUpgrade) return;

        PendingUpgrade = false;

        var prefs = new CardSelectorPrefs(
            CardSelectorPrefs.UpgradeSelectionPrompt,
            0,
            3)
        {
            Cancelable = true,
            RequireManualConfirmation = true
        };

        var selection = await CardSelectCmd.FromDeckForUpgrade(Owner, prefs);
        var cards = selection.ToList();

        if (cards.Count == 0) return;

        foreach (var card in cards)
            CardCmd.Upgrade(card, CardPreviewStyle.None);

        // 每升一张扣 2 点最大生命值
        await CreatureCmd.LoseMaxHp(
            new BlockingPlayerChoiceContext(),
            Owner.Creature,
            MaxHpLossPerCard * cards.Count,
            false);
    }
}