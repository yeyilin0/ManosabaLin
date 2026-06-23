using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.CardTags;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop.AutoRegistration;

namespace ManosabaLin.Scripts;

[RegisterOwnedCardTag(nameof(Eternal))]
public class MyTags
{
    public static readonly CardTag Eternal = 
        ModContentRegistry.GetQualifiedCardTagId("manosabalin", nameof(Eternal)).GetModCardTag();
}
