# ResoniteHeartRate

This is a Heart Rate mod using [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader) mod for [Resonite](https://resonite.com/) it will work for all heart rate monitor compatible with [Pulsoid](https://pulsoid.net) or [HypeRate](https://www.hyperate.io). This mod is an alternative to using web sockets for sending in heart rate data, I created it as opening a desktop program from VR every day became annoying. This has some benefits over web sockets as it uses value streams like other tracking data, using streamed data instead of sync makes it less likely to become desynced.

![soft copy](https://github.com/HamoCorp/ResoniteHeartRate/assets/43244781/5bd4c8b2-e7dc-4a49-a06f-4e894d54c52b)

If you prefer to use a web socket program instead of modding your game then use [HR2VRC](https://github.com/200Tigersbloxed/HRtoVRChat_OSC). or [LynxVR](https://github.com/lynixfur/LynxVR)

## Installation
1. Install [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader).

2. To be able to edit settings you also need to have this mod installed [Resonite Mod Settings](https://github.com/badhaloninja/ResoniteModSettings).

3. From the latest release there are 2 dll files [websocket-sharp-core.dll](https://github.com/HamoCorp/ResoniteHeartRate/releases/latest/download/websocket-sharp-core.dll) is a library required! to send HeartRate data to Hype Rate put this in `rml_libs` folder. This folder should be at `C:\Program Files (x86)\Steam\steamapps\common\Resonite\rml_libs`.
Then you put [ResoniteHeartRate.dll](https://github.com/HamoCorp/ResoniteHeartRate/releases/latest/download/ResoniteHeartRate.dll) into the `rml_mods` folder. This folder should be at `C:\Program Files (x86)\Steam\steamapps\common\Resonite\rml_mods` You can create these folders if there missing.

![dlls](https://github.com/HamoCorp/ResoniteHeartRate/assets/43244781/ac71ccbd-e31d-4b49-9aa9-d5cae79416b0)

4. Start the game. go to your mod setting and enter your [HypeRate ID](https://www.hyperate.io/webbluetooth) or [Pulsoid key](https://pulsoid.net) and save settings.

## Facets
There is now support for adding HeartRate Facets to your Dash. these can be found in my public folder. Paste this link into resonite: `resrec:///U-HamoCorp/R-e5c234a3-c4d4-4645-af54-7a80d388605c`. With this also adds support for a Reconnect Button. Pressing the button will stop the HeartRate and attempt to restart and reconnect. These include facets displaying the heartrate and also 2 graph facets based on the FPS facet, one them has a small range of values and is meant for the top of the dash to match the fps facet whilst the other records a much longer set of values to be displayed.

![facets 2](https://github.com/HamoCorp/ResoniteHeartRate/assets/43244781/160fe449-d970-442f-8efd-f590135d51f0)

## Reading your HeartRate ingame

Option 1: You can find 2 types of Heart Rate monitors in my public folder. Paste this link into resonite: `resrec:///U-HamoCorp/R-e5c234a3-c4d4-4645-af54-7a80d388605c`. The one on the left, All you need to do is parent it under your avatar and it will automatically link and start beating. The second type of monitor can be placed in the world and shows a heart rate graph Data but requires you to type in your username at the bottom for it to work.

![heart copy](https://github.com/HamoCorp/ResoniteHeartRate/assets/43244781/d85362c3-539b-4243-99cc-96fa1f40ec5f)


Option 2: If you would rather make your own interface or get the int value of heart rate you will find the heart rate slot under your user root with a dynamic variable:

![2024-04-03 19 44 58](https://github.com/HamoCorp/ResoniteHeartRate/assets/43244781/e7d6ca43-ec45-4a89-962d-367484a8f901)

## Problems
If you experience any problems with the mod not working heart rate not updateing then try respawning.
I found Hyperate to be more reliable than Pulsoid which can be sus as sometimes it randomly stops working.
Ill try an fix any bugs although might not be nessasary when this [github issue](https://github.com/Yellow-Dog-Man/Resonite-Issues/issues/1538) gets added to the game
