# Script Map

Made by **Tools > Script Map** in Unity. Don't edit this file by hand, it is rewritten every update.

## Warnings

- CraftingMenu.cs has no /* */ header at the top.
- EnemyAttack.cs has no /* */ header at the top.
- EnemyCombat.cs has no /* */ header at the top.
- EnemyMovement.cs has no /* */ header at the top.
- EnemySpawner.cs has no /* */ header at the top.
- ResourceNode.cs has no /* */ header at the top.
- Wander.cs has no /* */ header at the top.
- PerkChest.cs has no /* */ header at the top.
- PerkData.cs has no /* */ header at the top.
- PerkManager.cs has no /* */ header at the top.
- EquippedWeaponDisplay.cs has no /* */ header at the top.
- FireballProjectile.cs has no /* */ header at the top.
- GiveSwordForTesting.cs has no /* */ header at the top.
- PlayerCoins.cs has no /* */ header at the top.
- PlayerCombat.cs has no /* */ header at the top.
- PlayerFireball.cs has no /* */ header at the top.
- PlayerStats.cs: can't read this Requirement line: "ItemConsumer.cs, BlueprintPlacer.cs, PlayerRespawn.cs"
- TestPlayerHealth.cs has no /* */ header at the top.
- ObjectScatter.cs has no /* */ header at the top.
- PerkSelectionUI.cs has no /* */ header at the top.
- ItemDataEditor.cs uses Consumable.cs in its code, but its Requirement doesn't list it. Add: ItemDataEditor.cs x-> Consumable.cs
- TierToolBuilder.cs uses IconSource.cs in its code, but its Requirement doesn't list it. Add: TierToolBuilder.cs x-> IconSource.cs
- TierToolBuilder.cs uses PlayerInventory.cs in its code, but its Requirement doesn't list it. Add: TierToolBuilder.cs x-> PlayerInventory.cs
- TierToolBuilder.cs uses ToolRules.cs in its code, but its Requirement doesn't list it. Add: TierToolBuilder.cs x-> ToolRules.cs
- ObjectLevitator.cs uses BoundsUtil.cs in its code, but its Requirement doesn't list it. Add: ObjectLevitator.cs x-> BoundsUtil.cs
- ObjectLevitator.cs uses ClickClaim.cs in its code, but its Requirement doesn't list it. Add: ObjectLevitator.cs x-> ClickClaim.cs
- ObjectLevitator.cs uses Crosshair.cs in its code, but its Requirement doesn't list it. Add: ObjectLevitator.cs x-> Crosshair.cs
- ObjectLevitator.cs uses IHand.cs in its code, but its Requirement doesn't list it. Add: ObjectLevitator.cs x-> IHand.cs
- ObjectLevitator.cs uses InteractionGate.cs in its code, but its Requirement doesn't list it. Add: ObjectLevitator.cs x-> InteractionGate.cs
- ObjectLevitator.cs uses KeyBindings.cs in its code, but its Requirement doesn't list it. Add: ObjectLevitator.cs x-> KeyBindings.cs
- ObjectLevitator.cs uses MenuState.cs in its code, but its Requirement doesn't list it. Add: ObjectLevitator.cs x-> MenuState.cs
- PickupEffect.cs uses ItemGrabber.cs in its code, but its Requirement doesn't list it. Add: PickupEffect.cs x-> ItemGrabber.cs
- PlayerStats.cs uses BlueprintPlacer.cs in its code, but its Requirement doesn't list it. Add: PlayerStats.cs x-> BlueprintPlacer.cs
- PlayerStats.cs uses BuildingData.cs in its code, but its Requirement doesn't list it. Add: PlayerStats.cs x-> BuildingData.cs
- PlayerStats.cs uses ItemConsumer.cs in its code, but its Requirement doesn't list it. Add: PlayerStats.cs x-> ItemConsumer.cs
- PlayerStats.cs uses ItemData.cs in its code, but its Requirement doesn't list it. Add: PlayerStats.cs x-> ItemData.cs
- PlayerStats.cs uses PlayerRespawn.cs in its code, but its Requirement doesn't list it. Add: PlayerStats.cs x-> PlayerRespawn.cs
- BuildingButton.cs uses IconSource.cs in its code, but its Requirement doesn't list it. Add: BuildingButton.cs x-> IconSource.cs
- NewGamePanel.cs uses VictoryScreen.cs in its code, but its Requirement doesn't list it. Add: NewGamePanel.cs x-> VictoryScreen.cs

## Scripts

| Script | Folder | Needs | Needed by |
|---|---|---|---|
| [AnimalSpawner.cs](<Assets/Script/Animal/AnimalSpawner.cs>) | Animal | PlayerSight.cs, Terrain (Unity) (optional) | WorldSpawnSetup.cs |
| [WildAnimal.cs](<Assets/Script/Animal/WildAnimal.cs>) | Animal | Animator (Unity) (optional), ResourceNode.cs (optional), PlayerController.cs (optional), PlayerHealth.cs (optional) | DeathEffectBuilder.cs, WildAnimalBuilder.cs, WorldSpawnSetup.cs |
| [Blueprint.cs](<Assets/Script/Building/Blueprint.cs>) | Building | BuildingData.cs, BuiltPiece.cs | BlueprintPlacer.cs |
| [BlueprintPlacer.cs](<Assets/Script/Building/BlueprintPlacer.cs>) | Building | Blueprint.cs, BuildingData.cs, BuildSnap.cs, BuiltPiece.cs, BoundsUtil.cs, ClickClaim.cs, InteractionGate.cs, KeyBindings.cs, MenuState.cs, PlayerInventory.cs | BuildMenu.cs, BuildMenuUIBuilder.cs, BuildModeHUD.cs, PlayerStats.cs |
| [BuildingData.cs](<Assets/Script/Building/BuildingData.cs>) | Building | IconCapture.cs, IconSource.cs, Recipe.cs, PlayerInventory.cs | Blueprint.cs, BlueprintPlacer.cs, BuildingButton.cs, BuildingTools.cs, BuildMenu.cs, BuildMenuAutoList.cs, BuildMenuUIBuilder.cs, BuildSnap.cs, BuiltPiece.cs, ItemTools.cs, PlayerStats.cs |
| [BuildSnap.cs](<Assets/Script/Building/BuildSnap.cs>) | Building | BuiltPiece.cs, BuildingData.cs | BlueprintPlacer.cs |
| [BuiltPiece.cs](<Assets/Script/Building/BuiltPiece.cs>) | Building | BuildingData.cs | Blueprint.cs, BlueprintPlacer.cs, BuildSnap.cs |
| [CameraController.cs](<Assets/Script/Camera/CameraController.cs>) | Camera | KeyBindings.cs, MenuState.cs | FirstPersonHands.cs (optional), FirstPersonHandsBuilder.cs, HeldItemView.cs (optional), PlayerAnimator.cs (optional), ToolInHand.cs (optional) |
| [ShapeColor.cs](<Assets/Script/Color & Material/ShapeColor.cs>) | Color & Material | - | GrasslandTreeBuilder.cs, IconCapture.cs (optional) |
| [ClickClaim.cs](<Assets/Script/Core/ClickClaim.cs>) | Core | - | BlueprintPlacer.cs, HotbarSelector.cs, ItemConsumer.cs, ObjectLevitator.cs, PlayerCombat.cs |
| [GameRules.cs](<Assets/Script/Core/GameRules.cs>) | Core | - | AutoCollector.cs, DaylightCycle.cs, GameRulesEditor.cs, GameSetup.cs, HeldItemUI.cs, ItemDropper.cs, ItemIconRules.cs, NewGameDefaultsSync.cs, NotificationFeed.cs, SlotUI.cs, ToolTestKit.cs |
| [IHand.cs](<Assets/Script/Core/IHand.cs>) | Core | - | ObjectLevitator.cs, PlayerInventory.cs |
| [InteractionGate.cs](<Assets/Script/Core/InteractionGate.cs>) | Core | - | BlueprintPlacer.cs, ItemGrabber.cs, ObjectLevitator.cs |
| [MenuState.cs](<Assets/Script/Core/MenuState.cs>) | Core | - | BlueprintPlacer.cs, BuildMenu.cs, CameraController.cs, CraftingMenu.cs, DeathScreen.cs, HeldItemView.cs, HotbarSelector.cs, InventoryWindow.cs, ItemConsumer.cs, ItemDropper.cs, ItemGrabber.cs, ObjectLevitator.cs, PerkSelectionUI.cs, PlayerCombat.cs, PlayerController.cs, PlayerJump.cs, PlayerRespawn.cs, VictoryScreen.cs |
| [CraftingMenu.cs](<Assets/Script/Crafting/CraftingMenu.cs>) | Crafting | - | InventoryUIBuilder.cs, TierToolBuilder.cs |
| [Crosshair.cs](<Assets/Script/Crosshair/Crosshair.cs>) | Crosshair | ItemGrabber.cs (each other), ItemPickup.cs, ItemData.cs, BoundsUtil.cs, Image (Unity) | HUDBuilder.cs, ItemGrabber.cs (each other), ObjectLevitator.cs |
| [AnimatorBuildHelper.cs](<Assets/Script/Editor/AnimatorBuildHelper.cs>) | Editor | - | EnemyBuilder.cs, FirstPersonHandsBuilder.cs, PlayerAnimationSetup.cs, PlayerCharacterBuilder.cs, WildAnimalBuilder.cs |
| [BuildingTools.cs](<Assets/Script/Editor/BuildingTools.cs>) | Editor | BuildingData.cs, ItemPickup.cs, UIBuildHelper.cs, MenuOrder.cs | - |
| [BuildMenuAutoList.cs](<Assets/Script/Editor/BuildMenuAutoList.cs>) | Editor | BuildMenuUIBuilder.cs, BuildingData.cs | - |
| [BuildMenuUIBuilder.cs](<Assets/Script/Editor/BuildMenuUIBuilder.cs>) | Editor | UIBuildHelper.cs, BuildMenu.cs, BuildingButton.cs, BuildingData.cs, BlueprintPlacer.cs, BuildModeHUD.cs, UIAnimatedImage.cs, MenuOrder.cs | BuildMenuAutoList.cs |
| [BuildModeUIBuilder.cs](<Assets/Script/Editor/BuildModeUIBuilder.cs>) | Editor | UIBuildHelper.cs, BuildModeHUD.cs, UIAnimatedImage.cs, MenuOrder.cs | - |
| [CoinCounterEditor.cs](<Assets/Script/Editor/CoinCounterEditor.cs>) | Editor | CoinCounter.cs | - |
| [DeathEffectBuilder.cs](<Assets/Script/Editor/DeathEffectBuilder.cs>) | Editor | DeathEffect.cs, WildAnimal.cs, EnemyAnimator.cs, UIBuildHelper.cs, MenuOrder.cs | EnemyBuilder.cs, WildAnimalBuilder.cs |
| [EnemyBuilder.cs](<Assets/Script/Editor/EnemyBuilder.cs>) | Editor | UIBuildHelper.cs, AnimatorBuildHelper.cs, MeshBounds.cs, EnemyAnimator.cs, EnemyCombat.cs, EnemyMovement.cs, EnemyAttack.cs, EnemySpawner.cs, DeathEffectBuilder.cs, MenuOrder.cs | - |
| [FirstPersonArmsHelper.cs](<Assets/Script/Editor/FirstPersonArmsHelper.cs>) | Editor | FirstPersonHands.cs, UIBuildHelper.cs | FirstPersonHandsBuilder.cs, PlayerAnimationSetup.cs |
| [FirstPersonHandsBuilder.cs](<Assets/Script/Editor/FirstPersonHandsBuilder.cs>) | Editor | UIBuildHelper.cs, AnimatorBuildHelper.cs, MeshBounds.cs, FirstPersonArmsHelper.cs, FirstPersonHands.cs, PlayerAnimator.cs, CameraController.cs, MenuOrder.cs | - |
| [GameEndUIBuilder.cs](<Assets/Script/Editor/GameEndUIBuilder.cs>) | Editor | UIBuildHelper.cs, VictoryScreen.cs, FitToText.cs, MenuOrder.cs | - |
| [GameRulesEditor.cs](<Assets/Script/Editor/GameRulesEditor.cs>) | Editor | GameRules.cs | - |
| [GrasslandTreeBuilder.cs](<Assets/Script/Editor/GrasslandTreeBuilder.cs>) | Editor | UIBuildHelper.cs, ShapeColor.cs, ResourceNode.cs, ObjectScatter.cs, MenuOrder.cs | - |
| [HUDBuilder.cs](<Assets/Script/Editor/HUDBuilder.cs>) | Editor | UIBuildHelper.cs, StatBar.cs, Crosshair.cs, DayCounter.cs, TimeCounter.cs, CoinCounter.cs, PerkBar.cs, PlayerPerks.cs, MenuOrder.cs | - |
| [IconSourceDrawer.cs](<Assets/Script/Editor/IconSourceDrawer.cs>) | Editor | IconSource.cs | - |
| [InventoryUIBuilder.cs](<Assets/Script/Editor/InventoryUIBuilder.cs>) | Editor | UIBuildHelper.cs, TabGroup.cs, InventoryWindow.cs, SlotUI.cs, SlotGroup.cs, HeldItemUI.cs, HotbarSelector.cs, ItemDropper.cs, CraftingMenu.cs, MenuOrder.cs | - |
| [ItemDataEditor.cs](<Assets/Script/Editor/ItemDataEditor.cs>) | Editor | ItemData.cs | - |
| [ItemTools.cs](<Assets/Script/Editor/ItemTools.cs>) | Editor | ItemData.cs, ItemPickup.cs, BuildingData.cs, MenuOrder.cs | - |
| [MaterialBuildHelper.cs](<Assets/Script/Editor/MaterialBuildHelper.cs>) | Editor | UIBuildHelper.cs | TierToolBuilder.cs, WildAnimalBuilder.cs |
| [MenuOrder.cs](<Assets/Script/Editor/MenuOrder.cs>) | Editor | - | BuildingTools.cs, BuildMenuUIBuilder.cs, BuildModeUIBuilder.cs, DeathEffectBuilder.cs, EnemyBuilder.cs, FirstPersonHandsBuilder.cs, GameEndUIBuilder.cs, GrasslandTreeBuilder.cs, HUDBuilder.cs, InventoryUIBuilder.cs, ItemTools.cs, NotificationUIBuilder.cs, PlayerAnimationSetup.cs, PlayerCharacterBuilder.cs, PunchEffectBuilder.cs, ScriptMapTool.cs, TierToolBuilder.cs, TitleSceneBuilder.cs, WildAnimalBuilder.cs, WorldSpawnSetup.cs |
| [MeshBounds.cs](<Assets/Script/Editor/MeshBounds.cs>) | Editor | - | EnemyBuilder.cs, FirstPersonHandsBuilder.cs, PlayerCharacterBuilder.cs, TierToolBuilder.cs, WildAnimalBuilder.cs |
| [NewGameDefaultsSync.cs](<Assets/Script/Editor/NewGameDefaultsSync.cs>) | Editor | NewGameDefaults.cs, NewGamePanel.cs, VictoryScreen.cs, DaylightCycle.cs, EnemySpawner.cs, PlayerHunger.cs, GameRules.cs, KeyBindings.cs | TitleSceneBuilder.cs |
| [NotificationUIBuilder.cs](<Assets/Script/Editor/NotificationUIBuilder.cs>) | Editor | UIBuildHelper.cs, NotificationFeed.cs, GainNotifier.cs, MenuOrder.cs | - |
| [PlayerAnimationSetup.cs](<Assets/Script/Editor/PlayerAnimationSetup.cs>) | Editor | AnimatorBuildHelper.cs, FirstPersonArmsHelper.cs, PunchEffectBuilder.cs, PlayerAnimator.cs, PunchEffect.cs, PunchCombo.cs, PlayerInventory.cs, UIBuildHelper.cs, MenuOrder.cs | - |
| [PlayerCharacterBuilder.cs](<Assets/Script/Editor/PlayerCharacterBuilder.cs>) | Editor | UIBuildHelper.cs, AnimatorBuildHelper.cs, MeshBounds.cs, PlayerAnimator.cs, PlayerController.cs, ToolInHand.cs, MenuOrder.cs | - |
| [PunchEffectBuilder.cs](<Assets/Script/Editor/PunchEffectBuilder.cs>) | Editor | UIBuildHelper.cs, PunchEffect.cs, PlayerAnimator.cs, MenuOrder.cs | PlayerAnimationSetup.cs, TierToolBuilder.cs |
| [ScriptMapTool.cs](<Assets/Script/Editor/ScriptMapTool.cs>) | Editor | MenuOrder.cs | - |
| [TierToolBuilder.cs](<Assets/Script/Editor/TierToolBuilder.cs>) | Editor | UIBuildHelper.cs, MaterialBuildHelper.cs, MeshBounds.cs, PunchEffectBuilder.cs, ItemData.cs, ItemPickup.cs, ToolStats.cs, ToolInHand.cs, ToolTestKit.cs, PunchEffect.cs, FirstPersonHands.cs, PlayerAnimator.cs, CraftingMenu.cs, MenuOrder.cs | - |
| [TitleSceneBuilder.cs](<Assets/Script/Editor/TitleSceneBuilder.cs>) | Editor | UIBuildHelper.cs, MenuOrder.cs, NewGameDefaultsSync.cs, NewGameDefaults.cs, UIFXSprites.cs, UITween.cs, TitleMenu.cs, TitleBackground.cs, TitleTextFX.cs, ButtonFX.cs, CascadeIn.cs, PulseFX.cs, TabFX.cs, TabGroup.cs, ChoiceRow.cs, StepperRow.cs, TogglePop.cs, DropdownUnfold.cs, NewGamePanel.cs, KeyBindingMenu.cs, IncomingMessage.cs, KeyBindings.cs | - |
| [UIBuildHelper.cs](<Assets/Script/Editor/UIBuildHelper.cs>) | Editor | PlayerInventory.cs | BuildingTools.cs, BuildMenuUIBuilder.cs, BuildModeUIBuilder.cs, DeathEffectBuilder.cs, EnemyBuilder.cs, FirstPersonArmsHelper.cs, FirstPersonHandsBuilder.cs, GameEndUIBuilder.cs, GrasslandTreeBuilder.cs, HUDBuilder.cs, InventoryUIBuilder.cs, MaterialBuildHelper.cs, NotificationUIBuilder.cs, PlayerAnimationSetup.cs, PlayerCharacterBuilder.cs, PunchEffectBuilder.cs, TierToolBuilder.cs, TitleSceneBuilder.cs, WildAnimalBuilder.cs, WorldSpawnSetup.cs |
| [WildAnimalBuilder.cs](<Assets/Script/Editor/WildAnimalBuilder.cs>) | Editor | UIBuildHelper.cs, AnimatorBuildHelper.cs, MaterialBuildHelper.cs, MeshBounds.cs, WildAnimal.cs, ResourceNode.cs, ObjectScatter.cs, DeathEffectBuilder.cs, MenuOrder.cs | - |
| [WorldSpawnSetup.cs](<Assets/Script/Editor/WorldSpawnSetup.cs>) | Editor | ObjectScatter.cs, AnimalSpawner.cs, PlayerSight.cs, WildAnimal.cs, ResourceNode.cs, UIBuildHelper.cs, MenuOrder.cs | - |
| [DeathEffect.cs](<Assets/Script/Effects/DeathEffect.cs>) | Effects | ResourceNode.cs (optional), EnemyCombat.cs (optional), EnemyAttack.cs (optional) | DeathEffectBuilder.cs, EnemyAnimator.cs (optional) |
| [EnemyAnimator.cs](<Assets/Script/Enemy/EnemyAnimator.cs>) | Enemy | EnemyCombat.cs (optional), EnemyMovement.cs (optional), EnemyAttack.cs (optional), PlayerHealth.cs (optional), DeathEffect.cs (optional), Animator (Unity) (optional), Rigidbody (Unity) (optional) | DeathEffectBuilder.cs, EnemyAttack.cs, EnemyBuilder.cs, EnemyCombat.cs |
| [EnemyAttack.cs](<Assets/Script/Enemy/EnemyAttack.cs>) | Enemy | - | DeathEffect.cs (optional), EnemyAnimator.cs (optional), EnemyBuilder.cs, EnemySpawner.cs |
| [EnemyCombat.cs](<Assets/Script/Enemy/EnemyCombat.cs>) | Enemy | - | DeathEffect.cs (optional), EnemyAnimator.cs (optional), EnemyBuilder.cs, EnemySpawner.cs, FireballProjectile.cs, PlayerCombat.cs |
| [EnemyMovement.cs](<Assets/Script/Enemy/EnemyMovement.cs>) | Enemy | - | EnemyAnimator.cs (optional), EnemyBuilder.cs, EnemySpawner.cs |
| [EnemySpawner.cs](<Assets/Script/Enemy/EnemySpawner.cs>) | Enemy | - | EnemyBuilder.cs, GameSetup.cs, NewGameDefaultsSync.cs |
| [ResourceNode.cs](<Assets/Script/Gathering/ResourceNode.cs>) | Gathering | - | DeathEffect.cs (optional), FireballProjectile.cs, GrasslandTreeBuilder.cs, PlayerCombat.cs, ToolRules.cs, WildAnimal.cs (optional), WildAnimalBuilder.cs, WorldSpawnSetup.cs |
| [Wander.cs](<Assets/Script/Gathering/Wander.cs>) | Gathering | - | - |
| [IconCapture.cs](<Assets/Script/IconCapture/IconCapture.cs>) | IconCapture | ShapeColor.cs (optional) | BuildingData.cs, HeldItemView.cs, ItemData.cs, ItemIconRules.cs, ItemPickup.cs, ToolInHand.cs (optional) |
| [KeyBindings.cs](<Assets/Script/Input/KeyBindings.cs>) | Input | - | BlueprintPlacer.cs, BuildMenu.cs, CameraController.cs, CraftingMenu.cs, HotbarSelector.cs, InventoryWindow.cs, ItemConsumer.cs, ItemGrabber.cs, KeyBindingMenu.cs, NewGameDefaultsSync.cs, ObjectLevitator.cs, PlayerController.cs, PlayerJump.cs, TitleSceneBuilder.cs |
| [GameSetup.cs](<Assets/Script/NewGame/GameSetup.cs>) | NewGame | GameRules.cs, VictoryScreen.cs, DaylightCycle.cs, EnemySpawner.cs, PlayerHunger.cs | NewGameDefaults.cs, NewGamePanel.cs, TitleMenu.cs |
| [NewGameDefaults.cs](<Assets/Script/NewGame/NewGameDefaults.cs>) | NewGame | VictoryScreen.cs, GameSetup.cs | NewGameDefaultsSync.cs, NewGamePanel.cs, TitleSceneBuilder.cs |
| [PerkChest.cs](<Assets/Script/Perks/PerkChest.cs>) | Perks | - | - |
| [PerkData.cs](<Assets/Script/Perks/PerkData.cs>) | Perks | - | PerkChest.cs, PerkManager.cs, PerkSelectionUI.cs |
| [PerkManager.cs](<Assets/Script/Perks/PerkManager.cs>) | Perks | - | PerkChest.cs |
| [AutoCollector.cs](<Assets/Script/Player/AutoCollector.cs>) | Player | PlayerInventory.cs, ItemPickup.cs, ItemData.cs, GameRules.cs, PlayerHealth.cs (optional) | - |
| [EquippedWeaponDisplay.cs](<Assets/Script/Player/EquippedWeaponDisplay.cs>) | Player | - | - |
| [FireballProjectile.cs](<Assets/Script/Player/FireballProjectile.cs>) | Player | - | PlayerFireball.cs |
| [FirstPersonHands.cs](<Assets/Script/Player/FirstPersonHands.cs>) | Player | Animator (Unity), PlayerAnimator.cs (optional), CameraController.cs (optional), PlayerInventory.cs (optional), PlayerHealth.cs (optional), PlayerController.cs (optional), Camera (Unity) (optional) | FirstPersonArmsHelper.cs, FirstPersonHandsBuilder.cs, TierToolBuilder.cs, ToolInHand.cs (optional) |
| [GiveSwordForTesting.cs](<Assets/Script/Player/GiveSwordForTesting.cs>) | Player | - | - |
| [HeldItemView.cs](<Assets/Script/Player/HeldItemView.cs>) | Player | PlayerInventory.cs, IconCapture.cs, MenuState.cs, PlayerController.cs (optional), CameraController.cs (optional) | - |
| [HotbarSelector.cs](<Assets/Script/Player/HotbarSelector.cs>) | Player | PlayerInventory.cs, KeyBindings.cs, MenuState.cs, ClickClaim.cs | InventoryUIBuilder.cs |
| [ItemConsumer.cs](<Assets/Script/Player/ItemConsumer.cs>) | Player | PlayerInventory.cs, ItemData.cs, Consumable.cs, KeyBindings.cs, MenuState.cs, ClickClaim.cs, PlayerHealth.cs (optional), PlayerHunger.cs (optional), PlayerStamina.cs (optional) | PlayerStats.cs |
| [ItemDropper.cs](<Assets/Script/Player/ItemDropper.cs>) | Player | PlayerInventory.cs, DroppedStack.cs, ItemPickup.cs, MenuState.cs, GameRules.cs, PlayerHealth.cs (optional) | InventoryUIBuilder.cs |
| [ItemGrabber.cs](<Assets/Script/Player/ItemGrabber.cs>) | Player | Crosshair.cs (each other), PlayerInventory.cs, ItemPickup.cs, InteractionGate.cs, BoundsUtil.cs, KeyBindings.cs, MenuState.cs | Crosshair.cs (each other), PickupEffect.cs |
| [ObjectLevitator.cs](<Assets/Script/Player/ObjectLevitator.cs>) | Player | - | PlayerHunger.cs (optional), PunchCombo.cs (optional) |
| [PickupEffect.cs](<Assets/Script/Player/PickupEffect.cs>) | Player | - | - |
| [PlayerAnimator.cs](<Assets/Script/Player/PlayerAnimator.cs>) | Player | Animator (Unity), PlayerController.cs (optional), PlayerJump.cs (optional), PlayerHealth.cs (optional), CameraController.cs (optional), Rigidbody (Unity) (optional) | FirstPersonHands.cs (optional), FirstPersonHandsBuilder.cs, PlayerAnimationSetup.cs, PlayerCharacterBuilder.cs, PlayerCombat.cs, PunchCombo.cs, PunchEffect.cs, PunchEffectBuilder.cs, TierToolBuilder.cs, ToolRules.cs |
| [PlayerCoins.cs](<Assets/Script/Player/PlayerCoins.cs>) | Player | - | CoinCounter.cs, EnemyCombat.cs, GainNotifier.cs (optional), PerkChest.cs |
| [PlayerCombat.cs](<Assets/Script/Player/PlayerCombat.cs>) | Player | - | PlayerFireball.cs |
| [PlayerController.cs](<Assets/Script/Player/PlayerController.cs>) | Player | KeyBindings.cs, MenuState.cs, Rigidbody (Unity), PlayerStamina.cs (optional), PlayerJump.cs (optional) | FirstPersonHands.cs (optional), HeldItemView.cs (optional), PerkChest.cs, PerkManager.cs, PlayerAnimator.cs (optional), PlayerCharacterBuilder.cs, PlayerHunger.cs (optional), PlayerJump.cs (optional), PlayerPerks.cs (optional), PlayerStats.cs (optional), WildAnimal.cs (optional) |
| [PlayerFireball.cs](<Assets/Script/Player/PlayerFireball.cs>) | Player | - | PerkChest.cs, PerkManager.cs, PlayerPerks.cs (optional) |
| [PlayerHealth.cs](<Assets/Script/Player/PlayerHealth.cs>) | Player | - | AutoCollector.cs (optional), DaylightCycle.cs (optional), DeathScreen.cs, EnemyAnimator.cs (optional), EnemyAttack.cs, FirstPersonHands.cs (optional), GainNotifier.cs (optional), ItemConsumer.cs (optional), ItemDropper.cs (optional), PerkChest.cs, PerkManager.cs, PlayerAnimator.cs (optional), PlayerCoins.cs, PlayerHunger.cs (optional), PlayerPerks.cs (optional), PlayerRespawn.cs, PlayerSight.cs (optional), PlayerStats.cs (optional), StatBar.cs (optional), WildAnimal.cs (optional) |
| [PlayerHunger.cs](<Assets/Script/Player/PlayerHunger.cs>) | Player | PlayerController.cs (optional), PlayerHealth.cs (optional), ObjectLevitator.cs (optional) | GameSetup.cs, ItemConsumer.cs (optional), NewGameDefaultsSync.cs, PlayerRespawn.cs (optional), PlayerStats.cs (optional), StatBar.cs (optional) |
| [PlayerInventory.cs](<Assets/Script/Player/PlayerInventory.cs>) | Player | IHand.cs, ItemData.cs | AutoCollector.cs, BlueprintPlacer.cs, BuildingButton.cs, BuildingData.cs, CraftingMenu.cs, DroppedStack.cs, EquippedWeaponDisplay.cs, FirstPersonHands.cs (optional), GainNotifier.cs, GiveSwordForTesting.cs, HeldItemUI.cs, HeldItemView.cs, HotbarSelector.cs, ItemConsumer.cs, ItemData.cs, ItemDropper.cs, ItemGrabber.cs, ItemIconRules.cs, PlayerAnimationSetup.cs, PlayerCombat.cs, PlayerFireball.cs, PunchCombo.cs, Recipe.cs, ResourceNode.cs, SlotGroup.cs, SlotUI.cs, TierToolBuilder.cs, ToolInHand.cs, ToolRules.cs, ToolTestKit.cs, UIBuildHelper.cs |
| [PlayerJump.cs](<Assets/Script/Player/PlayerJump.cs>) | Player | KeyBindings.cs, MenuState.cs, Rigidbody (Unity), Collider (Unity), PlayerStamina.cs (optional), PlayerController.cs (optional) | PlayerAnimator.cs (optional), PlayerController.cs (optional), PlayerStats.cs (optional) |
| [PlayerPerks.cs](<Assets/Script/Player/PlayerPerks.cs>) | Player | PlayerHealth.cs (optional), PlayerStamina.cs (optional), PlayerController.cs (optional), PlayerFireball.cs (optional) | HUDBuilder.cs, PerkBar.cs, PerkChest.cs |
| [PlayerRespawn.cs](<Assets/Script/Player/PlayerRespawn.cs>) | Player | PlayerHealth.cs, MenuState.cs, PlayerHunger.cs (optional), PlayerStamina.cs (optional), Rigidbody (Unity) (optional) | DeathScreen.cs, PlayerStats.cs |
| [PlayerSight.cs](<Assets/Script/Player/PlayerSight.cs>) | Player | Camera (Unity) (optional), PlayerHealth.cs (optional) | AnimalSpawner.cs, WorldSpawnSetup.cs |
| [PlayerStamina.cs](<Assets/Script/Player/PlayerStamina.cs>) | Player | - | ItemConsumer.cs (optional), PerkChest.cs, PerkManager.cs, PlayerCombat.cs, PlayerController.cs (optional), PlayerJump.cs (optional), PlayerPerks.cs (optional), PlayerRespawn.cs (optional), PlayerStats.cs (optional), StatBar.cs (optional) |
| [PlayerStats.cs](<Assets/Script/Player/PlayerStats.cs>) | Player | PlayerController.cs (optional), PlayerJump.cs (optional), PlayerHealth.cs (optional), PlayerHunger.cs (optional), PlayerStamina.cs (optional) | DeathScreen.cs (optional), VictoryScreen.cs (optional) |
| [PunchCombo.cs](<Assets/Script/Player/PunchCombo.cs>) | Player | PlayerInventory.cs, PlayerAnimator.cs, ToolRules.cs, ObjectLevitator.cs (optional) | PlayerAnimationSetup.cs, PlayerCombat.cs, PunchEffect.cs |
| [PunchEffect.cs](<Assets/Script/Player/PunchEffect.cs>) | Player | PlayerAnimator.cs, PunchCombo.cs | PlayerAnimationSetup.cs, PlayerCombat.cs, PunchEffectBuilder.cs, TierToolBuilder.cs |
| [RespawnOnFall.cs](<Assets/Script/Player/RespawnOnFall.cs>) | Player | - | - |
| [TestPlayerHealth.cs](<Assets/Script/Player/TestPlayerHealth.cs>) | Player | - | - |
| [ToolInHand.cs](<Assets/Script/Tool/ToolInHand.cs>) | Tool | Animator (Unity), PlayerInventory.cs, ItemData.cs, ToolStats.cs, CameraController.cs (optional), FirstPersonHands.cs (optional), IconCapture.cs (optional) | PlayerCharacterBuilder.cs, TierToolBuilder.cs |
| [ToolRules.cs](<Assets/Script/Tool/ToolRules.cs>) | Tool | ToolStats.cs, ItemData.cs, PlayerInventory.cs, ResourceNode.cs, PlayerAnimator.cs, NotificationFeed.cs | PlayerCombat.cs, PlayerFireball.cs, PunchCombo.cs, TierToolBuilder.cs |
| [ToolTestKit.cs](<Assets/Script/Tool/ToolTestKit.cs>) | Tool | PlayerInventory.cs, ItemData.cs, GameRules.cs | TierToolBuilder.cs |
| [AsepriteFrames.cs](<Assets/Script/UI/AsepriteFrames.cs>) | UI | - | CoinCounter.cs, IconSource.cs, UIAnimatedImage.cs |
| [BuildingButton.cs](<Assets/Script/UI/Building/BuildingButton.cs>) | UI/Building | BuildingData.cs, Recipe.cs, PlayerInventory.cs, UIAnimatedImage.cs, ItemIconRules.cs, Button (Unity) | BuildMenu.cs, BuildMenuUIBuilder.cs |
| [BuildMenu.cs](<Assets/Script/UI/Building/BuildMenu.cs>) | UI/Building | BuildingButton.cs, BuildingData.cs, BlueprintPlacer.cs, KeyBindings.cs, MenuState.cs | BuildMenuUIBuilder.cs |
| [BuildModeHUD.cs](<Assets/Script/UI/Building/BuildModeHUD.cs>) | UI/Building | BlueprintPlacer.cs, Canvas (Unity) | BuildMenuUIBuilder.cs, BuildModeUIBuilder.cs |
| [ChoiceRow.cs](<Assets/Script/UI/Controls/ChoiceRow.cs>) | UI/Controls | UITween.cs | NewGamePanel.cs, TitleSceneBuilder.cs |
| [DropdownUnfold.cs](<Assets/Script/UI/Controls/DropdownUnfold.cs>) | UI/Controls | UITween.cs, TMP_Dropdown (Unity) | TitleSceneBuilder.cs |
| [StepperRow.cs](<Assets/Script/UI/Controls/StepperRow.cs>) | UI/Controls | UITween.cs | NewGamePanel.cs, TitleSceneBuilder.cs |
| [TogglePop.cs](<Assets/Script/UI/Controls/TogglePop.cs>) | UI/Controls | UITween.cs, Toggle (Unity) | TitleSceneBuilder.cs |
| [DeathScreen.cs](<Assets/Script/UI/Death/DeathScreen.cs>) | UI/Death | PlayerHealth.cs, PlayerRespawn.cs, MenuState.cs, Canvas (Unity), PlayerStats.cs (optional) | - |
| [ButtonFX.cs](<Assets/Script/UI/Effects/ButtonFX.cs>) | UI/Effects | UITween.cs, UIFXSprites.cs, UISparks.cs, UISound.cs, Button (Unity) | TitleMenu.cs, TitleSceneBuilder.cs |
| [CascadeIn.cs](<Assets/Script/UI/Effects/CascadeIn.cs>) | UI/Effects | UITween.cs | TitleMenu.cs, TitleSceneBuilder.cs |
| [PulseFX.cs](<Assets/Script/UI/Effects/PulseFX.cs>) | UI/Effects | UITween.cs | TitleSceneBuilder.cs |
| [ScreenFader.cs](<Assets/Script/UI/Effects/ScreenFader.cs>) | UI/Effects | UITween.cs | TitleMenu.cs |
| [TabFX.cs](<Assets/Script/UI/Effects/TabFX.cs>) | UI/Effects | TabGroup.cs, UITween.cs | TitleSceneBuilder.cs |
| [TitleTextFX.cs](<Assets/Script/UI/Effects/TitleTextFX.cs>) | UI/Effects | UITween.cs, TMP_Text (Unity) | TitleMenu.cs, TitleSceneBuilder.cs |
| [UIFXSprites.cs](<Assets/Script/UI/Effects/UIFXSprites.cs>) | UI/Effects | - | ButtonFX.cs, IncomingMessage.cs, TitleBackground.cs, TitleSceneBuilder.cs, UISparks.cs |
| [UISound.cs](<Assets/Script/UI/Effects/UISound.cs>) | UI/Effects | - | ButtonFX.cs, TitleMenu.cs |
| [UISparks.cs](<Assets/Script/UI/Effects/UISparks.cs>) | UI/Effects | UITween.cs, UIFXSprites.cs | ButtonFX.cs, TitleMenu.cs |
| [UITween.cs](<Assets/Script/UI/Effects/UITween.cs>) | UI/Effects | - | ButtonFX.cs, CascadeIn.cs, ChoiceRow.cs, DropdownUnfold.cs, IncomingMessage.cs, KeyBindingMenu.cs, NewGamePanel.cs, PulseFX.cs, ScreenFader.cs, StepperRow.cs, TabFX.cs, TitleBackground.cs, TitleMenu.cs, TitleSceneBuilder.cs, TitleTextFX.cs, TogglePop.cs, UISparks.cs |
| [FitToText.cs](<Assets/Script/UI/FitToText.cs>) | UI | RectTransform (Unity) | GameEndUIBuilder.cs |
| [VictoryScreen.cs](<Assets/Script/UI/GameEnd/VictoryScreen.cs>) | UI/GameEnd | DaylightCycle.cs, MenuState.cs, Canvas (Unity), PlayerStats.cs (optional) | GameEndUIBuilder.cs, GameSetup.cs, NewGameDefaults.cs, NewGameDefaultsSync.cs, NewGamePanel.cs |
| [CoinCounter.cs](<Assets/Script/UI/HUD/CoinCounter.cs>) | UI/HUD | PlayerCoins.cs, AsepriteFrames.cs | CoinCounterEditor.cs, GainNotifier.cs (optional), HUDBuilder.cs |
| [DayCounter.cs](<Assets/Script/UI/HUD/DayCounter.cs>) | UI/HUD | DaylightCycle.cs, TextMeshProUGUI (Unity) | HUDBuilder.cs |
| [PerkBar.cs](<Assets/Script/UI/HUD/PerkBar.cs>) | UI/HUD | PlayerPerks.cs | HUDBuilder.cs |
| [StatBar.cs](<Assets/Script/UI/HUD/StatBar.cs>) | UI/HUD | PlayerHealth.cs (optional), PlayerHunger.cs (optional), PlayerStamina.cs (optional) | HUDBuilder.cs |
| [TimeCounter.cs](<Assets/Script/UI/HUD/TimeCounter.cs>) | UI/HUD | DaylightCycle.cs, TextMeshProUGUI (Unity) | HUDBuilder.cs |
| [IconSource.cs](<Assets/Script/UI/IconSource.cs>) | UI | AsepriteFrames.cs | BuildingButton.cs, BuildingData.cs, IconSourceDrawer.cs, ItemData.cs, ItemIconRules.cs, SlotUI.cs, TierToolBuilder.cs, UIAnimatedImage.cs |
| [HeldItemUI.cs](<Assets/Script/UI/Inventory/HeldItemUI.cs>) | UI/Inventory | PlayerInventory.cs, SlotUI.cs, ItemIconRules.cs, GameRules.cs | InventoryUIBuilder.cs |
| [InventoryWindow.cs](<Assets/Script/UI/Inventory/InventoryWindow.cs>) | UI/Inventory | KeyBindings.cs, MenuState.cs | CraftingMenu.cs, InventoryUIBuilder.cs |
| [SlotGroup.cs](<Assets/Script/UI/Inventory/SlotGroup.cs>) | UI/Inventory | SlotUI.cs, PlayerInventory.cs | InventoryUIBuilder.cs |
| [SlotUI.cs](<Assets/Script/UI/Inventory/SlotUI.cs>) | UI/Inventory | PlayerInventory.cs, UIAnimatedImage.cs, IconSource.cs, ItemIconRules.cs, GameRules.cs, Image (Unity) | HeldItemUI.cs, InventoryUIBuilder.cs, SlotGroup.cs |
| [ItemIconRules.cs](<Assets/Script/UI/ItemIconRules.cs>) | UI | GameRules.cs, ItemData.cs, IconSource.cs, IconCapture.cs, PlayerInventory.cs | BuildingButton.cs, GainNotifier.cs, HeldItemUI.cs, SlotUI.cs |
| [GainNotifier.cs](<Assets/Script/UI/Notifications/GainNotifier.cs>) | UI/Notifications | NotificationFeed.cs, PlayerInventory.cs, ItemIconRules.cs, PlayerCoins.cs (optional), CoinCounter.cs (optional), PlayerHealth.cs (optional) | NotificationUIBuilder.cs |
| [NotificationFeed.cs](<Assets/Script/UI/Notifications/NotificationFeed.cs>) | UI/Notifications | GameRules.cs, VerticalLayoutGroup (Unity) | GainNotifier.cs, NotificationUIBuilder.cs, ToolRules.cs |
| [PerkSelectionUI.cs](<Assets/Script/UI/Perks/PerkSelectionUI.cs>) | UI/Perks | - | PerkChest.cs |
| [IncomingMessage.cs](<Assets/Script/UI/Settings/IncomingMessage.cs>) | UI/Settings | UITween.cs, UIFXSprites.cs | TitleSceneBuilder.cs |
| [KeyBindingMenu.cs](<Assets/Script/UI/Settings/KeyBindingMenu.cs>) | UI/Settings | KeyBindings.cs, UITween.cs | TitleMenu.cs (optional), TitleSceneBuilder.cs |
| [TabGroup.cs](<Assets/Script/UI/Tabs/TabGroup.cs>) | UI/Tabs | - | InventoryUIBuilder.cs, TabFX.cs, TitleSceneBuilder.cs |
| [NewGamePanel.cs](<Assets/Script/UI/Title/NewGamePanel.cs>) | UI/Title | NewGameDefaults.cs, GameSetup.cs, StepperRow.cs, ChoiceRow.cs, UITween.cs, TMP_Dropdown (Unity) | NewGameDefaultsSync.cs, TitleMenu.cs, TitleSceneBuilder.cs |
| [TitleBackground.cs](<Assets/Script/UI/Title/TitleBackground.cs>) | UI/Title | UITween.cs, UIFXSprites.cs | TitleSceneBuilder.cs |
| [TitleMenu.cs](<Assets/Script/UI/Title/TitleMenu.cs>) | UI/Title | UITween.cs, UISparks.cs, UISound.cs, ScreenFader.cs, TitleTextFX.cs, CascadeIn.cs, ButtonFX.cs, NewGamePanel.cs, GameSetup.cs, KeyBindingMenu.cs (optional) | TitleSceneBuilder.cs |
| [UIAnimatedImage.cs](<Assets/Script/UI/UIAnimatedImage.cs>) | UI | AsepriteFrames.cs, IconSource.cs, Image (Unity) | BuildingButton.cs, BuildMenuUIBuilder.cs, BuildModeUIBuilder.cs, SlotUI.cs |
| [BoundsUtil.cs](<Assets/Script/Utility/BoundsUtil.cs>) | Utility | - | BlueprintPlacer.cs, Crosshair.cs, ItemGrabber.cs, ObjectLevitator.cs |
| [DaylightCycle.cs](<Assets/Script/World/DaylightCycle.cs>) | World | GameRules.cs, Light (Unity), PlayerHealth.cs (optional) | DayCounter.cs, EnemySpawner.cs, GameSetup.cs, NewGameDefaultsSync.cs, TimeCounter.cs, VictoryScreen.cs |
| [ObjectScatter.cs](<Assets/Script/World/ObjectScatter.cs>) | World | - | GrasslandTreeBuilder.cs, WildAnimalBuilder.cs, WorldSpawnSetup.cs |
| [Consumable.cs](<Assets/Script/WorldItem/Consumable.cs>) | WorldItem | - | ItemConsumer.cs, ItemData.cs, ItemDataEditor.cs |
| [DroppedStack.cs](<Assets/Script/WorldItem/DroppedStack.cs>) | WorldItem | ItemPickup.cs, PlayerInventory.cs | ItemDropper.cs |
| [ItemData.cs](<Assets/Script/WorldItem/ItemData.cs>) | WorldItem | Recipe.cs (each other), Consumable.cs, ToolStats.cs, IconCapture.cs, IconSource.cs, PlayerInventory.cs | AutoCollector.cs, CraftingMenu.cs, Crosshair.cs, ItemConsumer.cs, ItemDataEditor.cs, ItemIconRules.cs, ItemPickup.cs, ItemTools.cs, PlayerInventory.cs, PlayerStats.cs, Recipe.cs (each other), ResourceNode.cs, TierToolBuilder.cs, ToolInHand.cs, ToolRules.cs, ToolTestKit.cs |
| [ItemPickup.cs](<Assets/Script/WorldItem/ItemPickup.cs>) | WorldItem | ItemData.cs, IconCapture.cs | AutoCollector.cs, BuildingTools.cs, Crosshair.cs, DroppedStack.cs, ItemDropper.cs, ItemGrabber.cs, ItemTools.cs, TierToolBuilder.cs |
| [Recipe.cs](<Assets/Script/WorldItem/Recipe.cs>) | WorldItem | ItemData.cs (each other), PlayerInventory.cs | BuildingButton.cs, BuildingData.cs, CraftingMenu.cs, ItemData.cs (each other) |
| [ToolStats.cs](<Assets/Script/WorldItem/ToolStats.cs>) | WorldItem | - | ItemData.cs, PlayerCombat.cs, TierToolBuilder.cs, ToolInHand.cs, ToolRules.cs |
