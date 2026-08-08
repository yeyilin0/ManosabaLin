using MinionLib.Component.Core;
﻿using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Hiro.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Hiro.Cards;

[RegisterCard(typeof(LinCardPool))]
public sealed class LyXl() : ManosabaCardTemplate(-1, CardType.Attack, CardRarity.Ancient, TargetType.AnyPlayer)
{
    private const string EffectHoverLocEntry = "MANOSABA_LIN_CARD_LY_XL_EFFECT";

    public override CardAssetProfile AssetProfile => base.AssetProfile with
    {
        AncientTextBgPath = "ancient_empty_text_bg.png".CardsImagePath()
    };

    public override int MaxUpgradeLevel => 0;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<Powerthreethree>();
            yield return new HoverTip(
                new LocString("cards", $"{EffectHoverLocEntry}.title"),
                new LocString("cards", $"{EffectHoverLocEntry}.description"));
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay, ComponentContext componentContext)
    {
        var source = this;
        var target = cardPlay.Target ?? source.Owner.Creature;

        await PowerCmd.Apply<WithPower>(
            choiceContext, target, 100,
            source.Owner.Creature, source, false
        );

        await PowerCmd.Apply<Powerthreethree>(
            choiceContext, target, 1,
            source.Owner.Creature, source, false
        );

        await PowerCmd.Apply<RitualCeremonyPower>(
            choiceContext, target, 1,
            source.Owner.Creature, source, false
        );

        await CreatureCmd.Damage(
            choiceContext,
            source.Owner.Creature,
            999m,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null, null
        );
    }
}
