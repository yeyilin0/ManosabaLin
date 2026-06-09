using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using System.Threading.Tasks;
using ManosabaLin.Characters.Sherrylin.Cards.Emotions;

namespace ManosabaLin.Characters.Sherrylin.Orbs;

/// <summary>
/// 惊讶球体：回合结束记录手卡数量，下回合开始抽取等量卡牌。
/// </summary>
[RegisterOrb]
public sealed class EmotionSurpriseOrb : EmotionOrb<EmotionSurprise>
{
    private int _recordedHandSize;

    protected override Color OrbColor => new(0.2f, 0.9f, 0.9f);

    public override async Task BeforeTurnEndOrbTrigger(PlayerChoiceContext ctx)
    {
        if (Owner != null)
        {
            var hand = PileType.Hand.GetPile(Owner);
            _recordedHandSize = hand.Cards.Count;
        }
    }

    public override async Task AfterTurnStartOrbTrigger(PlayerChoiceContext ctx)
    {
        if (_recordedHandSize > 0 && Owner != null)
        {
            await CardPileCmd.Draw(ctx, _recordedHandSize, Owner);
        }

        await OrbCmd.EvokeNext(ctx, Owner);
    }
}
