using System;
using System.Reflection;
using Aki.Reflection.Patching;
using BepInEx;
using BepInEx.Configuration;
using DrakiaXYZ.BotDebug.Components;
using DrakiaXYZ.BotDebug.VersionChecker;
using EFT;

namespace DrakiaXYZ.BotDebug
{
    [BepInPlugin("xyz.drakia.botdebug", "DrakiaXYZ-BotDebug", "0.0.1")]
    public class BotDebugPlugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> Enable;
        private void Awake()
        {
            if (!TarkovVersion.CheckEftVersion(Logger, Info, Config))
            {
                throw new Exception($"Invalid EFT Version");
            }

            Enable = Config.Bind(
                "Main Settings",
                "Enable",
                false,
                "Turn Off/On");

            new NewGamePatch().Enable();
        }
    }

    // Add the debug component every time a match starts
    internal class NewGamePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));

        [PatchPrefix]
        public static void PatchPrefix()
        {
            BotDebugComponent.Enable();
        }
    }
}
