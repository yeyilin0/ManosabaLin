using ManosabaLin.Characters.Common.Components.Abstracts;

namespace ManosabaLin.Characters.Emalin.Components;

public sealed partial class BlackHandComponent : KeywordLikeComponent
{
    public override Task BeforeFlushLatePostfix(PlayerChoiceContext choiceContext, Player player,
        ComponentContext componentContext)
    {
        Card!.GiveSingleTurnRetain();
        return Task.CompletedTask;
    }
}
