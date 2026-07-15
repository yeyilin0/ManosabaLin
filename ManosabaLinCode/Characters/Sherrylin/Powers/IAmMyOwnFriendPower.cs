using ManosabaLin.Characters.Common;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Characters.Sherrylin.Powers;

/// <summary>
/// 吾即吾友能力：下回合开始随机复制一张基础情绪卡加入手牌，然后移除此能力。
/// </summary>
[RegisterPower]
public sealed class IAmMyOwnFriendPower : ManosabaPowerTemplate
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;

        Flash();

        var rng = Owner.Player.RunState.Rng.CombatCardSelection;
        var roll = rng.NextInt(6);

        CardModel? newCard = roll switch
        {
            0 => Owner.CombatState.CreateCard<EmotionAnger>(Owner.Player),
            1 => Owner.CombatState.CreateCard<EmotionDisgust>(Owner.Player),
            2 => Owner.CombatState.CreateCard<EmotionSadness>(Owner.Player),
            3 => Owner.CombatState.CreateCard<EmotionFear>(Owner.Player),
            4 => Owner.CombatState.CreateCard<EmotionJoy>(Owner.Player),
            5 => Owner.CombatState.CreateCard<EmotionSurprise>(Owner.Player),
            _ => null
        };

        if (newCard != null)
            await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, Owner.Player);

        RemoveInternal();
    }
}
