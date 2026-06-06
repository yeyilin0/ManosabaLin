using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace ManosabaLin.Characters.Hiro.Events;

[RegisterActEvent(typeof(Hive))]
public sealed class KinmaneyurakuchoEvent : ModEventTemplate
{
    private const string BaseImagePath = "res://ManosabaLin/images/events/kinmaneyurakucho.png";
    private const string OneImagePath = "res://ManosabaLin/images/events/kinmaneyurakucho_one.png";
    private const string TwoImagePath = "res://ManosabaLin/images/events/kinmaneyurakucho_two.png";
    private const string ThreeImagePath = "res://ManosabaLin/images/events/kinmaneyurakucho_three.png";

    private const int GoldCost = 100;
    private const int HpCost = 20;

    public override EventAssetProfile AssetProfile => new(
        InitialPortraitPath: BaseImagePath
    );

    public override bool IsShared => true;

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        var options = new List<EventOption>();

        // 选项1：变形游戏 — 需要金币
        if (Owner?.Gold >= GoldCost)
        {
            options.Add(new EventOption(this, ChooseTransform,
                InitialOptionKey("TRANSFORM")));
        }
        else
        {
            options.Add(new EventOption(this, null,
                InitialOptionKey("TRANSFORM_LOCKED")));
        }

        // 选项2：进化游戏 — 需要药水
        if (Owner?.Potions.Any() == true)
        {
            options.Add(new EventOption(this, ChooseUpgrade,
                InitialOptionKey("UPGRADE")));
        }
        else
        {
            options.Add(new EventOption(this, null,
                InitialOptionKey("UPGRADE_LOCKED")));
        }

        // 选项3：消消乐 — 需要足够HP
        if (Owner?.Creature != null && Owner.Creature.CurrentHp > HpCost)
        {
            options.Add(new EventOption(this, ChooseRemove,
                InitialOptionKey("REMOVE")));
        }
        else
        {
            options.Add(new EventOption(this, null,
                InitialOptionKey("REMOVE_LOCKED")));
        }

        return options;
    }

    private async Task ChooseTransform()
    {
        if (Owner?.Creature == null) return;

        ChangePortrait(OneImagePath);

        Owner.Gold -= GoldCost;

        var prefs = new CardSelectorPrefs(CardSelectorPrefs.TransformSelectionPrompt, 4)
        {
            Cancelable = false,
            RequireManualConfirmation = true
        };

        var selection = await CardSelectCmd.FromDeckForTransformation(Owner, prefs);
        foreach (var card in selection)
        {
            await CardCmd.TransformToRandom(card, Rng, CardPreviewStyle.EventLayout);
        }

        SetEventFinished(PageDescription("OPTION_TRANSFORM_DONE"));
    }

    private async Task ChooseUpgrade()
    {
        if (Owner?.Creature == null) return;

        ChangePortrait(TwoImagePath);

        var randomPotion = Rng.NextItem(Owner.Potions);
        if (randomPotion != null)
            await PotionCmd.Discard(randomPotion);

        var prefs = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 6)
        {
            Cancelable = false,
            RequireManualConfirmation = true
        };

        var selection = await CardSelectCmd.FromDeckForUpgrade(Owner, prefs);
        foreach (var card in selection)
        {
            CardCmd.Upgrade(card, CardPreviewStyle.None);
        }

        SetEventFinished(PageDescription("OPTION_UPGRADE_DONE"));
    }

    private async Task ChooseRemove()
    {
        if (Owner?.Creature == null) return;

        ChangePortrait(ThreeImagePath);

        await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), Owner.Creature,
            HpCost, ValueProp.Unblockable | ValueProp.Unpowered, null, null);

        var prefs = new CardSelectorPrefs(CardSelectorPrefs.RemoveSelectionPrompt, 4)
        {
            Cancelable = false,
            RequireManualConfirmation = true
        };

        var selection = await CardSelectCmd.FromDeckForRemoval(Owner, prefs);
        foreach (var card in selection)
        {
            await CardPileCmd.RemoveFromDeck(card);
        }

        SetEventFinished(PageDescription("OPTION_REMOVE_DONE"));
    }

    private static void ChangePortrait(string imagePath)
    {
        NEventRoom.Instance?.SetPortrait(
            PreloadManager.Cache.GetTexture2D(imagePath));
    }
}
