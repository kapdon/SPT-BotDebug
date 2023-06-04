using BepInEx.Configuration;
using Comfort.Common;
using DrakiaXYZ.BotDebug.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DrakiaXYZ.BotDebug.Helpers
{
    internal class Settings
    {
        public static ConfigEntry<bool> Enable;
        public static ConfigEntry<EBotInfoMode> ActiveMode;
        public static ConfigEntry<KeyboardShortcut> NextModeKey;
        public static ConfigEntry<KeyboardShortcut> PrevModeKey;

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
