using BepInEx.Configuration;
using Comfort.Common;
using DrakiaXYZ.BotDebug.Components;
using System;
using UnityEngine;

namespace DrakiaXYZ.BotDebug.Helpers
{
    internal class Settings
    {
        public static ConfigEntry<bool> Enable;
        public static ConfigEntry<EBotInfoMode> ActiveMode;
        public static ConfigEntry<KeyboardShortcut> NextModeKey;
        public static ConfigEntry<KeyboardShortcut> PrevModeKey;
        public static ConfigEntry<int> MaxDrawDistance;

        public static void Init(ConfigFile Config)
        {
            Enable = Config.Bind(
                "Main Settings",
                "Enable",
                false,
                "Turn Off/On");
            Enable.SettingChanged += Enable_SettingChanged;

            ActiveMode = Config.Bind(
                "Main Settings",
                "ActiveMode",
                EBotInfoMode.Behaviour,
                "Set the bot monitor mode");

            NextModeKey = Config.Bind(
                "Main Settings",
                "NextModeKey",
                new KeyboardShortcut(KeyCode.F10),
                "Key to switch to the next Monitor Mode");

            PrevModeKey = Config.Bind(
                "Main Settings",
                "PrevModeKey",
                new KeyboardShortcut(KeyCode.F9),
                "Key to switch to the previous Monitor Mode");

            MaxDrawDistance = Config.Bind(
                "Main Settings",
                "MaxDrawDistance",
                1500,
                new ConfigDescription("Max distance to draw a bot's debug box", new AcceptableValueRange<int>(0, 2000)));
        }

        public static void Enable_SettingChanged(object sender, EventArgs e)
        {
            // If no game, do nothing
            if (!Singleton<IBotGame>.Instantiated)
            {
                return;
            }

            if (Enable.Value)
            {
                BotDebugComponent.Enable();
            }
            else
            {
                BotDebugComponent.Disable();
            }
        }
    }
}
