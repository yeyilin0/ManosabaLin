using MegaCrit.Sts2.Core.Helpers;

namespace ManosabaLin.Characters.Common;

internal static class CardEffectHoverTipFactory
{
    public static IHoverTip FromCard(CardModel card, string locEntry)
    {
        var title = new LocString("cards", $"{locEntry}.title");
        var description = new LocString("cards", $"{locEntry}.description");
        var energyPrefix = EnergyIconHelper.GetPrefix(card);

        title.Add("energyPrefix", energyPrefix);
        description.Add("energyPrefix", energyPrefix);
        card.DynamicVars.AddTo(title);
        card.DynamicVars.AddTo(description);
        ApplyEnergyPrefix(title, energyPrefix);
        ApplyEnergyPrefix(description, energyPrefix);
        return new HoverTip(title, description);
    }

    private static void ApplyEnergyPrefix(LocString locString, string energyPrefix)
    {
        foreach (var value in locString.Variables.Values)
        {
            if (value is EnergyVar energyVar)
                energyVar.ColorPrefix = energyPrefix;
        }
    }
}
