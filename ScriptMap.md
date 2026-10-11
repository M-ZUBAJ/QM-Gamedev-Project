# Script Map

Made by **Tools > Script Map** in Unity. Don't edit this file by hand, it is rewritten every update.

## Warnings

- ItemDataEditor.cs uses Consumable.cs in its code, but its Requirement doesn't list it. Add: ItemDataEditor.cs x-> Consumable.cs
- ObjectLevitator.cs uses BoundsUtil.cs in its code, but its Requirement doesn't list it. Add: ObjectLevitator.cs x-> BoundsUtil.cs
- ObjectLevitator.cs uses ClickClaim.cs in its code, but its Requirement doesn't list it. Add: ObjectLevitator.cs x-> ClickClaim.cs
- ObjectLevitator.cs uses Crosshair.cs in its code, but its Requirement doesn't list it. Add: ObjectLevitator.cs x-> Crosshair.cs
- ObjectLevitator.cs uses IHand.cs in its code, but its Requirement doesn't list it. Add: ObjectLevitator.cs x-> IHand.cs
- ObjectLevitator.cs uses InteractionGate.cs in its code, but its Requirement doesn't list it. Add: ObjectLevitator.cs x-> InteractionGate.cs
- ObjectLevitator.cs uses KeyBindings.cs in its code, but its Requirement doesn't list it. Add: ObjectLevitator.cs x-> KeyBindings.cs
- ObjectLevitator.cs uses MenuState.cs in its code, but its Requirement doesn't list it. Add: ObjectLevitator.cs x-> MenuState.cs
- PickupEffect.cs uses ItemGrabber.cs in its code, but its Requirement doesn't list it. Add: PickupEffect.cs x-> ItemGrabber.cs

## Scripts

| Script | Folder | Needs | Needed by |
|---|---|---|---|
| [Blueprint.cs](<Assets/Script/Building/Blueprint.cs>) | Building | BuildingData.cs | BlueprintPlacer.cs |
| [BlueprintPlacer.cs](<Assets/Script/Building/BlueprintPlacer.cs>) | Building | Blueprint.cs, BuildingData.cs, BoundsUtil.cs, ClickClaim.cs, InteractionGate.cs, KeyBindings.cs, MenuState.cs, PlayerInventory.cs | BuildMenu.cs, BuildMenuUIBuilder.cs, BuildModeHUD.cs |
| [BuildingData.cs](<Assets/Script/Building/BuildingData.cs>) | Building | IconCapture.cs, IconSource.cs, Recipe.cs, PlayerInventory.cs | Blueprint.cs, BlueprintPlacer.cs, BuildingButton.cs, BuildingTools.cs, BuildMenu.cs, BuildMenuAutoList.cs, BuildMenuUIBuilder.cs, ItemTools.cs |
| [CameraController.cs](<Assets/Script/Camera/CameraController.cs>) | Camera | KeyBindings.cs, MenuState.cs | HeldItemView.cs (optional) |
| [ShapeColor.cs](<Assets/Script/Color & Material/ShapeColor.cs>) | Color & Material | - | IconCapture.cs (optional) |
| [ClickClaim.cs](<Assets/Script/Core/ClickClaim.cs>) | Core | - | BlueprintPlacer.cs, HotbarSelector.cs, ItemConsumer.cs, ObjectLevitator.cs |
| [GameRules.cs](<Assets/Script/Core/GameRules.cs>) | Core | - | DaylightCycle.cs |
| [IHand.cs](<Assets/Script/Core/IHand.cs>) | Core | - | ObjectLevitator.cs, PlayerInventory.cs |
| [InteractionGate.cs](<Assets/Script/Core/InteractionGate.cs>) | Core | - | BlueprintPlacer.cs, ItemGrabber.cs, ObjectLevitator.cs |
| [MenuState.cs](<Assets/Script/Core/MenuState.cs>) | Core | - | BlueprintPlacer.cs, BuildMenu.cs, CameraController.cs, HeldItemView.cs, HotbarSelector.cs, InventoryWindow.cs, ItemConsumer.cs, ItemDropper.cs, ItemGrabber.cs, ObjectLevitator.cs, PlayerController.cs, PlayerJump.cs |
| [Crosshair.cs](<Assets/Script/Crosshair/Crosshair.cs>) | Crosshair | ItemGrabber.cs (each other), ItemPickup.cs, ItemData.cs, BoundsUtil.cs, Image (Unity) | ItemGrabber.cs (each other), ObjectLevitator.cs |
| [BuildingTools.cs](<Assets/Script/Editor/BuildingTools.cs>) | Editor | BuildingData.cs, ItemPickup.cs, UIBuildHelper.cs, MenuOrder.cs | - |
| [BuildMenuAutoList.cs](<Assets/Script/Editor/BuildMenuAutoList.cs>) | Editor | BuildMenuUIBuilder.cs, BuildingData.cs | - |
| [BuildMenuUIBuilder.cs](<Assets/Script/Editor/BuildMenuUIBuilder.cs>) | Editor | UIBuildHelper.cs, BuildMenu.cs, BuildingButton.cs, BuildingData.cs, BlueprintPlacer.cs, BuildModeHUD.cs, UIAnimatedImage.cs, MenuOrder.cs | BuildMenuAutoList.cs |
| [BuildModeUIBuilder.cs](<Assets/Script/Editor/BuildModeUIBuilder.cs>) | Editor | UIBuildHelper.cs, BuildModeHUD.cs, UIAnimatedImage.cs, MenuOrder.cs | - |
| [IconSourceDrawer.cs](<Assets/Script/Editor/IconSourceDrawer.cs>) | Editor | IconSource.cs | - |
| [InventoryUIBuilder.cs](<Assets/Script/Editor/InventoryUIBuilder.cs>) | Editor | UIBuildHelper.cs, TabGroup.cs, InventoryWindow.cs, SlotUI.cs, SlotGroup.cs, HeldItemUI.cs, HotbarSelector.cs, ItemDropper.cs, MenuOrder.cs | - |
| [ItemDataEditor.cs](<Assets/Script/Editor/ItemDataEditor.cs>) | Editor | ItemData.cs | - |
| [ItemTools.cs](<Assets/Script/Editor/ItemTools.cs>) | Editor | ItemData.cs, ItemPickup.cs, BuildingData.cs, MenuOrder.cs | - |
| [MenuOrder.cs](<Assets/Script/Editor/MenuOrder.cs>) | Editor | - | BuildingTools.cs, BuildMenuUIBuilder.cs, BuildModeUIBuilder.cs, InventoryUIBuilder.cs, ItemTools.cs, ScriptMapTool.cs |
| [ScriptMapTool.cs](<Assets/Script/Editor/ScriptMapTool.cs>) | Editor | MenuOrder.cs | - |
| [UIBuildHelper.cs](<Assets/Script/Editor/UIBuildHelper.cs>) | Editor | PlayerInventory.cs | BuildingTools.cs, BuildMenuUIBuilder.cs, BuildModeUIBuilder.cs, InventoryUIBuilder.cs |
| [IconCapture.cs](<Assets/Script/IconCapture/IconCapture.cs>) | IconCapture | ShapeColor.cs (optional) | BuildingData.cs, HeldItemView.cs, ItemData.cs, ItemPickup.cs |
| [KeyBindings.cs](<Assets/Script/Input/KeyBindings.cs>) | Input | - | BlueprintPlacer.cs, BuildMenu.cs, CameraController.cs, HotbarSelector.cs, InventoryWindow.cs, ItemConsumer.cs, ItemGrabber.cs, ObjectLevitator.cs, PlayerController.cs, PlayerJump.cs |
| [AutoCollector.cs](<Assets/Script/Player/AutoCollector.cs>) | Player | PlayerInventory.cs, ItemPickup.cs, ItemData.cs | - |
| [HeldItemView.cs](<Assets/Script/Player/HeldItemView.cs>) | Player | PlayerInventory.cs, IconCapture.cs, MenuState.cs, PlayerController.cs (optional), CameraController.cs (optional) | - |
| [HotbarSelector.cs](<Assets/Script/Player/HotbarSelector.cs>) | Player | PlayerInventory.cs, KeyBindings.cs, MenuState.cs, ClickClaim.cs | InventoryUIBuilder.cs |
| [ItemConsumer.cs](<Assets/Script/Player/ItemConsumer.cs>) | Player | PlayerInventory.cs, ItemData.cs, Consumable.cs, KeyBindings.cs, MenuState.cs, ClickClaim.cs, PlayerHealth.cs (optional), PlayerHunger.cs (optional), PlayerStamina.cs (optional) | - |
| [ItemDropper.cs](<Assets/Script/Player/ItemDropper.cs>) | Player | PlayerInventory.cs, DroppedStack.cs, ItemPickup.cs, MenuState.cs | InventoryUIBuilder.cs |
| [ItemGrabber.cs](<Assets/Script/Player/ItemGrabber.cs>) | Player | Crosshair.cs (each other), PlayerInventory.cs, ItemPickup.cs, InteractionGate.cs, BoundsUtil.cs, KeyBindings.cs, MenuState.cs | Crosshair.cs (each other), PickupEffect.cs |
| [ObjectLevitator.cs](<Assets/Script/Player/ObjectLevitator.cs>) | Player | - | PlayerHunger.cs (optional) |
| [PickupEffect.cs](<Assets/Script/Player/PickupEffect.cs>) | Player | - | - |
| [PlayerController.cs](<Assets/Script/Player/PlayerController.cs>) | Player | KeyBindings.cs, MenuState.cs, Rigidbody (Unity), PlayerStamina.cs (optional) | HeldItemView.cs (optional), PlayerHunger.cs (optional) |
| [PlayerHealth.cs](<Assets/Script/Player/PlayerHealth.cs>) | Player | - | ItemConsumer.cs (optional), PlayerHunger.cs (optional), StatBar.cs (optional) |
| [PlayerHunger.cs](<Assets/Script/Player/PlayerHunger.cs>) | Player | PlayerController.cs (optional), PlayerHealth.cs (optional), ObjectLevitator.cs (optional) | ItemConsumer.cs (optional), StatBar.cs (optional) |
| [PlayerInventory.cs](<Assets/Script/Player/PlayerInventory.cs>) | Player | IHand.cs, ItemData.cs | AutoCollector.cs, BlueprintPlacer.cs, BuildingButton.cs, BuildingData.cs, DroppedStack.cs, HeldItemUI.cs, HeldItemView.cs, HotbarSelector.cs, ItemConsumer.cs, ItemData.cs, ItemDropper.cs, ItemGrabber.cs, Recipe.cs, SlotGroup.cs, SlotUI.cs, UIBuildHelper.cs |
| [PlayerJump.cs](<Assets/Script/Player/PlayerJump.cs>) | Player | KeyBindings.cs, MenuState.cs, Rigidbody (Unity), Collider (Unity), PlayerStamina.cs (optional) | - |
| [PlayerStamina.cs](<Assets/Script/Player/PlayerStamina.cs>) | Player | - | ItemConsumer.cs (optional), PlayerController.cs (optional), PlayerJump.cs (optional), StatBar.cs (optional) |
| [RespawnOnFall.cs](<Assets/Script/Player/RespawnOnFall.cs>) | Player | - | - |
| [AsepriteFrames.cs](<Assets/Script/UI/AsepriteFrames.cs>) | UI | - | IconSource.cs, UIAnimatedImage.cs |
| [BuildingButton.cs](<Assets/Script/UI/Building/BuildingButton.cs>) | UI/Building | BuildingData.cs, Recipe.cs, PlayerInventory.cs, UIAnimatedImage.cs, Button (Unity) | BuildMenu.cs, BuildMenuUIBuilder.cs |
| [BuildMenu.cs](<Assets/Script/UI/Building/BuildMenu.cs>) | UI/Building | BuildingButton.cs, BuildingData.cs, BlueprintPlacer.cs, KeyBindings.cs, MenuState.cs | BuildMenuUIBuilder.cs |
| [BuildModeHUD.cs](<Assets/Script/UI/Building/BuildModeHUD.cs>) | UI/Building | BlueprintPlacer.cs, Canvas (Unity) | BuildMenuUIBuilder.cs, BuildModeUIBuilder.cs |
| [StatBar.cs](<Assets/Script/UI/HUD/StatBar.cs>) | UI/HUD | PlayerHealth.cs (optional), PlayerHunger.cs (optional), PlayerStamina.cs (optional) | - |
| [IconSource.cs](<Assets/Script/UI/IconSource.cs>) | UI | AsepriteFrames.cs | BuildingData.cs, IconSourceDrawer.cs, ItemData.cs, SlotUI.cs, UIAnimatedImage.cs |
| [HeldItemUI.cs](<Assets/Script/UI/Inventory/HeldItemUI.cs>) | UI/Inventory | PlayerInventory.cs, SlotUI.cs | InventoryUIBuilder.cs |
| [InventoryWindow.cs](<Assets/Script/UI/Inventory/InventoryWindow.cs>) | UI/Inventory | KeyBindings.cs, MenuState.cs | InventoryUIBuilder.cs |
| [SlotGroup.cs](<Assets/Script/UI/Inventory/SlotGroup.cs>) | UI/Inventory | SlotUI.cs, PlayerInventory.cs | InventoryUIBuilder.cs |
| [SlotUI.cs](<Assets/Script/UI/Inventory/SlotUI.cs>) | UI/Inventory | PlayerInventory.cs, UIAnimatedImage.cs, IconSource.cs, Image (Unity) | HeldItemUI.cs, InventoryUIBuilder.cs, SlotGroup.cs |
| [TabGroup.cs](<Assets/Script/UI/Tabs/TabGroup.cs>) | UI/Tabs | - | InventoryUIBuilder.cs |
| [UIAnimatedImage.cs](<Assets/Script/UI/UIAnimatedImage.cs>) | UI | AsepriteFrames.cs, IconSource.cs, Image (Unity) | BuildingButton.cs, BuildMenuUIBuilder.cs, BuildModeUIBuilder.cs, SlotUI.cs |
| [BoundsUtil.cs](<Assets/Script/Utility/BoundsUtil.cs>) | Utility | - | BlueprintPlacer.cs, Crosshair.cs, ItemGrabber.cs, ObjectLevitator.cs |
| [DaylightCycle.cs](<Assets/Script/World/DaylightCycle.cs>) | World | GameRules.cs, Light (Unity) | - |
| [Consumable.cs](<Assets/Script/WorldItem/Consumable.cs>) | WorldItem | - | ItemConsumer.cs, ItemData.cs, ItemDataEditor.cs |
| [DroppedStack.cs](<Assets/Script/WorldItem/DroppedStack.cs>) | WorldItem | ItemPickup.cs, PlayerInventory.cs | ItemDropper.cs |
| [ItemData.cs](<Assets/Script/WorldItem/ItemData.cs>) | WorldItem | Recipe.cs (each other), Consumable.cs, IconCapture.cs, IconSource.cs, PlayerInventory.cs | AutoCollector.cs, Crosshair.cs, ItemConsumer.cs, ItemDataEditor.cs, ItemPickup.cs, ItemTools.cs, PlayerInventory.cs, Recipe.cs (each other) |
| [ItemPickup.cs](<Assets/Script/WorldItem/ItemPickup.cs>) | WorldItem | ItemData.cs, IconCapture.cs | AutoCollector.cs, BuildingTools.cs, Crosshair.cs, DroppedStack.cs, ItemDropper.cs, ItemGrabber.cs, ItemTools.cs |
| [Recipe.cs](<Assets/Script/WorldItem/Recipe.cs>) | WorldItem | ItemData.cs (each other), PlayerInventory.cs | BuildingButton.cs, BuildingData.cs, ItemData.cs (each other) |
