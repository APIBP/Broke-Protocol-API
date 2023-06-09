## 1.36
?> The UI Update adds events related to changing ChatMode and ChatChannel so the server can override or block that behavior. Also, some old client packets were removed to prevent misuse (by me). But the gap in the ClPacket enum changed a bunch of packet IDs so any plugin sending packets directly (SvPlayer.Send(...)) will likely need rebuilding.

### API
* Removed old SerializedAttachments/Wearables client packets
* Modded Underbarrel Attachments can adjust default 'Setting'
* SetChatChannel and SetChatMode events added to GameSource

## 1.35
?> The Destruction Update adds a new class of events for Voxels. Also there's a public API on the new ShVoxel class if you want to manipulate them at runtime. Chat handling is also changed to account for a new ChatMode set for each player that decides whether their voice/message will go Public, Job, or Private Channel when using LocalChat.

### API
* New Voxel event class added (More events coming later)
* Fixed some missing namespaces
* Use HashSet for command lookups
* Mount event implementation moved to GameSource
* Some ShGun parameters/functions moved up to ShWeapon base class
* Global/LocalChatMessage events & packets renamed to ChatGlobal/Local
* New ChatVoice event added
* New handling for ChatLocal messages depending on SvPlayer.chatMode
* SvPlayer.ResetZoom renamed to ResetMode
* Net serializer now supports ushort, int3, and Color32 for new features
* PlaceItemCount moved to SceneManager
* Heightmaps data update ('pngBytes'->'heightmapData' to update old maps)
* New 'Always Visible' Entity parameter (entities always loaded on clients)
* SvEntity.Despawn() renamed to Deactivate() for consistency
* GameSource fix for dead NPCs stuck in vehicles

## 1.3
?> The War Update splits the game up into 3 different plugins. GameSource now contains only core logic related to connections, AI, damage, inventories, etc. LifeSource contains everything related to crimes, jails, RP Jobs, and random AI traffic spawning. The new WarSource plugin adds a new territory capture game mode, new login flow, and AI that can battle in team-based combat. Each plugin uses it's own Extended Player class but all its methods are virtual. You can extend these classes in your own plugin or override their existing behavior. If you want to change behavior or add your own hooks to those methods, make sure you Override the PlayerInitialize event and insert your own class into the pluginPlayer dictionary.
?? Also new are the 'Events' abstract classes. These are ManagerEvents, EntityEvents, MountableEvents, DestroyableEvents, PhysicalEvents, MovableEvents, and PlayerEvents. Override methods in each class to add your own behavior instead of using the old [Target] attributes. This should help you see what's available for modding and their exact parameters. You can use the new [Execution] attribute to change how the methods are called.

### API
* All core game logic moved to GameSource
* All crime, jail, and police handling moved to LifeSource
* Dropping items sets the 'spawner' field on entities (for modding)
* All object types have a Data field now (use in World Builder and in API)
* Data string field restored after entity respawns (Thanks @Olivrrr)
* Player Maintenance loop moved to LifeSource
* New CustomPacket event for pre-login UI handling
* API: [Target] attribute deprecated, Override new Events classes for custom handling
* All Managers and World classes are now Singletons with Instance accessors
* Manager reference no longer passed to Manager events (use Singleton classes)
* Null strings are now handled by the network serializer as empty strings
* BPAPI changed to static class
* GroupManager -> GroupHandler for consistency with other static classes
* ChatHandler -> InterfaceHandler
* BrokeProtocol.API.Types namespace removed
* Added SetMaxSpeed() to ShMovable class (for modding)
* Added ExecutionMode.Additive (similar to Override except overridden events are still executed)
* Removed ExecutionMode.Final (easy to abuse/misuse)
* Added customData field to ConnectionData for pre-join modding
* Removed GroupIndex.Gangster (just use 'Criminal' now)
* API: Must manually call SvDestroyMenu(id) (except when showing a new menu with the same ID)
* All events can now return a boolean to stop execution further execution on that same event chain
* Connection related data like deviceID and passwordHash moved to svPlayer.connectData
* Stop all entity coroutines immediately on destruction (prevent some race conditions)

### Mapping
* Use the new Territory entity to replace the old 3 Territory types
    * Set Owner Index field to jobIndex that you want to give initial ownership
    * Any Owner Index less than 0 or greater that job Count will be set to unowned
    * Job indices for the 3 gangs in LifeSource are 6, 7, 8
    * Scale of territories in Default are 200x200x200
* Use new ServerTriggers to replace ThrowableTarget, RestrictedArea, AreaWarning, Repair, Rearm, etc
    * Add the event name in Enter/Exit Event field in the World Builder ("RestrictedArea" etc. in GameSource)
    * 1.3 backwards compatible objects will keep your old Trigger entity locations/transforms
      * But they will be missing the event names, so don't forget to add them
* To make a War map
    * You just need to place Territories (these are used for spawning and Spawn objects are ignored)
    * You can move, scale, or rotate territoroes, just be mindful of spawning and bot navigation
    * It helps the AI if you place simple Boat and Aircraft waypoints around the map (see DefaultWar)

## 1.25
?> The Locked On Update overhauls how thrown items are handled for projectiles and vehicles. Now they offer multiple weapon sets that can be cycled through so old mods will have to be updated for their weapons to be usable again. Also some hideInterior references for hiding tank turrets, etc is now per-seat instead of per-vehicle. For code changes, GameSource.dll has been renamed to load first and ExecutionModes have been altered slightly though it shouldn't break things in the majority of cases.

### API
- New Plugin ExectionModes
    - ExecutionMode.Override events will replace previous override events
    - Added ExecutionMode.Final (Doesn't allow any more overrides, same as Override behavior prior to 1.25)
- ShowAlert packet added to API
    - Use helper functions for SvShowTime and SvShowAlert now instead of sending packets manually
- New svManager.Add/RemoveInventoryAction() for custom inventory item actions/events
- Added ShEntity.CenterBounds/Mass properties for aiming/looking/buoyancy
- svEntity.thrower -> svEntity.instigator
    - Since it's used for fires, exited vehicles, more than just thrown items

### MODDING
- Boats now have a stabilityFactor moddable property that was hard-coded before
- Physics updates -> Mods likely need values updated:
    - Aircraft mods should multiply old values by 25x : uprightStrength, stabilityStrength
    - Boat mods should mutltiply old values by 25x : engineFactor, turnFactor
- Steerable class removed
    - Modders delete Steerable scripts in Unity project if updating
- HideInterior is now a per-seat property
    - Mods using this to hide player models need to update
- ThrownEntities for Projectiles and Vehicles must be redone under new Weapon Sets property
- New Target Type property on Thrown entities makes the projectile locking/tracking any entity of that type
- New 'Flare' tag will make any Entity act as countermeasures for guided weapons

## 1.22
?> The Cracking Update has a few important breaking changes but mostly extends the API with additional functionality around the lockpicking minigame and almos mounting and dismounting vehicles

### API
- GameSource New Events
    - PlayerCrackStart, PlayerMount, PlayerDismount
- GameSource OnDamage Events now receive more useful parameters about the source
    - New parameters Vector3 source, Vector3 hitPoint
    - Replaces old hitY parameter
    - Used for damage calculations and also for direction markers
- New 'inventoryType' Enum
    - Replaces hasInventory, shop, lockable fields
    - Removed 'Safe' Class of items (use Entity with Locked inventoryType)
- New entity methods
    - HasInventory
    - Shop
    - InApartment
    - CanView
    - CanCrack
- Removed svManager.fixedPlaces (due to cleaner InApartment implementation)
- Removed svManager.payScale (handled fully in GameSource now)
- Removed BrokeProtocol.Prefabs namespace (just use BrokeProtocol.Entities)
- New LineGraphic, QuadGraphic, CircleGraphic UI Classes
    - Available for ExecuteCS but will have proper API later

### MODDING
- All Transports have some air control now
    - Adjust with new per-axis orientStrength Property
- Thrown objects now support a customDestroyEffect
    - Old mods need reconfiguration
- Hitscan items now support a customFireEffect
- Added vanilla game Destructibles to BPResources
    - Working and properly modable now

## 1.2
?> The Multimedia Update has sweeping changes across jobs, animations, vehicle mounting, UI API, and adds some useful new stuff too. Plugins and even some assets are likely to break, and I'll outline the main issues here.

### API
- SvPlayer.ShowTextPanel(text) -> SvPlayer.SendTextPanel(text, menuID, options)
- SvPlayer.HideTextPanel() -> SvPlayer.DestroyTextPanel(menuID)
- Removed SvPlayer.SvMountPrimary()
- Removed SvPlayer.SvMountBack()
- Added SvPlayer.SvTryMount() -> Used to find and mount to the closest seat
    - Or use SvMount() to pick a specific seat
- New Dynamic Action Menu Methods:
    - ShEntity.SvAdd/RemoveDynamicAction(eventName, label)
    - ShPlayer.SvAdd/RemoveSelfAction(eventName, label)
    - ShPlayer.SvAdd/RemoveTypeAction(eventName, type, label)
- New Video Methods:
    - SvEntity.SvStartDefaultVideo(index)
    - SvEntity.SvStartCustomVideo(url)
    - SvEntity.SvStopVideo()
- Added GameSource Event: PlayerSetEquipable
- Jobs Modding Overhaul
    - Can accept JSON metadata now
    - Use new DynamicAction menu methods
    - Can support multiple plugins with Plugin.JobsAdditive and Plugin.JobsOverride
    - Too many changes, see Jobs.cs and Core.cs in GameSourse to see how everything works

### MODDING
- Animator property moved from (Cl)ient class to (Sh)ared class
    - All player/character mods must have this assigned and re-exported with latest BPResources
    - If you want to sync mod Animators automatically, check the 'Sync Animator' property in Unity
- See existing VideoPlayer examples in BPResourses to see how they're set up and assigned

### Misc
- New Video Permissions
    - "bp.videoDefault"
    - "bp.videoCustom"
    - "bp.videoStop"
- Added ConnectionData.deviceID for checking hardware bans or alts during Login/Registration

## 1.14
?> The Animals Update adds a couple new useful GameSource events and a new character type called Mob (used only for basic animals currently). Most existing mods should be forward compatible with this version although Player character mods and custom Player animations will likely need to be redone and exported with the new BPResources.

### API
- Additional entity lookup method: 'bool EntityCollections.TryFindEntity(int id, out ShEntity e)'
- Use new Player.Hands property instead of manager.hands since each characterType can have a different default 'Hands' item
- svPlayer.NodeNear() returns the actual NavGraph Node instead of just a bool
- New Animal related crimes:
    - CrimeIndex.AnimalCruelty
    - CrimeIndex.AnimalKilling
- Added GameSource Events:
    - PlayerHandsUp
    - PlayerMenuClosed

### MODDING
- Re-organized Player Character controller (functionality is 99% the same, just a minor jump fix)
- Player hip origin is different than before for most Sit animations (Player character mods will need to updated for Sit animations)
    - Vehicles don't need to be updated (automatically handled)
- Clothing/Wearables don't need colliders set anymore
    - It's fine if old mods have them though they will be duplicated
