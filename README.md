# ResoniteHeartRate

This is a Heart Rate mod using [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader) mod for [Resonite](https://resonite.com/) it will work for all heart rate monitor compatible with [Pulsoid](https://pulsoid.net). This mod is an alternative to using web sockets for sending in heart rate data, I created it as open a program each time got annoying. This has some benefits over web sockets as it uses value streams like other tracking data, using streamed data instead of sync makes it less likely to become desynced.

If you prefer to use a web socket program instead of modding your game then use [HR2VRC](https://github.com/200Tigersbloxed/HRtoVRChat_OSC).

## Installation
1. Install [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader).

2. To be able to edit the UI settings you also need to have this mod installed [Resonite Mod Settings](https://github.com/badhaloninja/ResoniteModSettings).

3. Place [ResoniteHeartRate.dll](https://github.com/HamoCorp/CustomLegacyUI/releases/latest/download/ResoniteHeartRate.dll) into your `rml_mods` folder. This folder should be at `C:\Program Files (x86)\Steam\steamapps\common\Resonite\rml_mods` for a default install. You can create it if it's missing, or if you launch the game once with ResoniteModLoader installed it will create the folder for you.

4. Start the game. go to you mod setting and enter your [Pulsoid key](https://pulsoid.net) and save settings.

## Reading your HeartRate

Option 1: You can find a basic Heart Rate monitor in my public folder. All you need to do is parent it under your avatar and it will automatically link the variable and start beating.
resrec:///U-HamoCorp/R-e5c234a3-c4d4-4645-af54-7a80d388605c

![2024-04-03 19 41 57](https://github.com/HamoCorp/ResoniteHeartRate/assets/43244781/d0837159-fb1c-4354-b9c0-2364ddbf3013)


Option 2: If you would rather make your own interface or get the int value of heart rate you will find the heart rate slot under your user root with a dynamic variable:

![2024-04-03 19 44 58](https://github.com/HamoCorp/ResoniteHeartRate/assets/43244781/e7d6ca43-ec45-4a89-962d-367484a8f901)
