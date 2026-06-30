---
name: manosabalin-abstract-model-hooks
description: Choose and implement common AbstractModel hooks for ManosabaLin cards, relics, powers, monsters, and RitsuLib model capabilities. Use when deciding between Before/After/Modify/TryModify hooks, owner hook capabilities, combat hook participation, clone lifecycle, or command-safe gameplay timing.
---

# ManosabaLin AbstractModel Hooks

## First Rule

Read the actual signatures before editing:

- Base game: `D:\RiderProjects\SlayTheSpire2\src\Core\Models\AbstractModel.cs`
- Cards: `...\CardModel.cs`
- Relics: `...\RelicModel.cs`
- Powers: `...\PowerModel.cs`
- Monsters: `...\MonsterModel.cs`
- RitsuLib capabilities: `D:\RiderProjects\STS2-RitsuLib\Models\Capabilities\`

Do not guess nullability, `PlayerChoiceContext`, or return types.

## Hook Selection

- Use `Before...` hooks to validate, prepare, or mutate state before the game action.
- Use `After...` hooks for side effects after the action succeeds.
- Use `Modify...` hooks for pure value changes; return only the delta or replacement shape the base method expects.
- Use `TryModify...` hooks when the hook needs to report whether it changed an object/list/value.
- Use `AfterModifying...` hooks to perform follow-up side effects after a modifying hook has been selected.

Common hooks:

- Combat lifecycle: `BeforeCombatStart`, `BeforeCombatStartLate`, `AfterCombatEnd`, `AfterCombatVictory`, `AfterCreatureAddedToCombat`.
- Turn lifecycle: `BeforeSideTurnStart`, `AfterSideTurnStart`, `AfterPlayerTurnStart`, `BeforeSideTurnEnd`, `AfterSideTurnEnd`, late/early variants.
- Card flow: `AfterCardEnteredCombat`, `AfterCardGeneratedForCombat`, `AfterCardChangedPiles`, `AfterCardDrawn`, `AfterCardDiscarded`, `AfterCardExhausted`.
- Card play: `BeforeCardPlayed`, `AfterCardPlayed`, `AfterCardPlayedLate`, `ModifyCardPlayCount`, `ModifyCardPlayResultPileTypeAndPosition`.
- Damage/block: `BeforeAttack`, `AfterAttack`, `ModifyAttackHitCount`, `ModifyDamageAdditive`, `ModifyDamageMultiplicative`, `BeforeDamageReceived`, `AfterDamageReceived`, `ModifyBlockAdditive`, `AfterBlockGained`.
- Powers: `BeforePowerAmountChanged`, `AfterPowerAmountChanged`, `ModifyPowerAmountGivenAdditive`, `TryModifyPowerAmountReceived`.
- Economy/rewards/map: `AfterGoldGained`, `ModifyGoldGained`, `TryModifyRewards`, `AfterRewardTaken`, `ModifyGeneratedMap`, `AfterRoomEntered`.

## Model-Specific Notes

- `CardModel` hooks for the card itself include `AfterCreated`, `AfterTransformedFrom`, `AfterTransformedTo`, `OnEnqueuePlayVfx`, plus `OnPlay`/`OnUpgrade` through `ManosabaCardTemplate`.
- `RelicModel` has `AfterObtained`, `AfterRemoved`, `IsAllowed`, `IsAllowedAtNeow`, `ShowCounter`, and `DisplayAmount`.
- `PowerModel` has `BeforeApplied`, `AfterApplied`, `AfterRemoved`, `ShouldPowerBeRemovedAfterOwnerDeath`, `PowerType`, `PowerStackType`, and `AllowNegative`.
- `MonsterModel` has `AfterAddedToRoom`, `BeforeRemovedFromRoom`, `AfterDeath`, and move-state machinery.

## Combat Hook Participation

- `AbstractModel.ShouldReceiveCombatHooks` controls whether combat hooks are called.
- Cards in combat piles, powers, relics, and active combat models normally receive relevant hooks.
- Disconnected canonical models should not be treated as mutable runtime state.
- Use `AssertMutable()` only to guard against misuse; do not paper over canonical/mutable mistakes by cloning at the wrong layer.

## RitsuLib Capability Hooks

- `OwnerHookCapability<TModel>` lets a capability receive the owning model's vanilla hooks.
- `CardCapability` receives card lifecycle helpers such as owner card upgraded/downgraded/transformed.
- `IModelCapabilityHookListener.OwnerHookOrder` controls ordering: negative before owner, zero/positive after owner.
- Gameplay-affecting multiplayer logic should use awaited vanilla hooks or RitsuLib owner-hook capabilities, not fire-and-forget side channels.

## Safety Rules

- Use command APIs for gameplay effects; avoid direct mutation when a command exists.
- Check owner/side/target guards explicitly.
- If adding events or delegate fields to an `AbstractModel`, inspect `AfterCloned`; shallow-copied delegates must not leak to clones.
- Validate with `dotnet build ManosabaLin.sln`.
