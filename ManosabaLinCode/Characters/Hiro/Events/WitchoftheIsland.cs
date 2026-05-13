// using Godot;
// using ManosabaLin.Characters.Hiro.Relics;
// using MegaCrit.Sts2.Core.Events;
// using MegaCrit.Sts2.Core.Models.Acts;
// using STS2RitsuLib.Interop.AutoRegistration;
// using STS2RitsuLib.Scaffolding.Content;
// using STS2RitsuLib.Utils;
// using System.Collections.Generic;
//
// namespace ManosabaLin.Characters.Hiro.Events;
//
// [RegisterActAncient(typeof(Overgrowth))]
// public class WitchoftheIsland : ModAncientEventTemplate
// {
//     public override Color ButtonColor => new(0.6f, 0.1f, 0.2f, 0.5f);
//     public override Color DialogueColor => new("CC3344");
//
//     public override EventAssetProfile AssetProfile => new(
//         BackgroundScenePath: "res://ManosabaLin/scenes/events/witchoftheisland.tscn"
//     );
//
//     public override AncientEventPresentationAssetProfile AncientPresentationAssetProfile => new(
//         MapIconPath: "res://ManosabaLin/images/events/witchoftheisland.png",
//         MapIconOutlinePath: "res://ManosabaLin/images/events/witchoftheisland_outline.png",
//         RunHistoryIconPath: "res://ManosabaLin/images/events/witchoftheisland.png",
//         RunHistoryIconOutlinePath: "res://ManosabaLin/images/events/witchoftheisland_outline.png"
//     );
//
//     private IReadOnlyList<EventOption> Pool1 => [
//         CreateModRelicOption<Safe>(),
//         CreateModRelicOption<Bloodiedclothing>(),
//         CreateModRelicOption<Fruitplate>(),
//     ];
//
//     private IReadOnlyList<EventOption> Pool2 => [
//         CreateModRelicOption<Ritualsword>(),
//         CreateModRelicOption<Magicsniperrifle>(),
//         CreateModRelicOption<Dismantledparts>(),
//     ];
//
//     private IReadOnlyList<EventOption> Pool3 => [
//         CreateModRelicOption<Externalinformation>(),
//         CreateModRelicOption<Magicalsketchbook>(),
//         CreateModRelicOption<Witchgrimoire>(),
//     ];
//
//     public override IEnumerable<EventOption> AllPossibleOptions => [.. Pool1, .. Pool2, .. Pool3];
//
//     protected override IReadOnlyList<EventOption> GenerateInitialOptions()
//     {
//         return
//         [
//             Rng.NextItem(Pool1)!,
//             Rng.NextItem(Pool2)!,
//             Rng.NextItem(Pool3)!,
//         ];
//     }
// }
