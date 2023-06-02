using System;
using System.Diagnostics;
using System.Reflection;
using Aki.Reflection.Patching;
using BepInEx;
using BepInEx.Configuration;
using EFT;
using UnityEngine;
using VersionChecker;

namespace BotDebug
{
    [BepInPlugin("com.dvize.BotDebug", "dvize.BotDebug", "1.0.0")]
    public class BotDebugPlugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> EnableGui;
        public static ConfigEntry<int> debugBoxHeight;
        private void Awake()
        {
            CheckEftVersion();

            EnableGui = Config.Bind(
                "Main Settings",
                "EnableGui",
                false,
                "Turn Off/On");

            debugBoxHeight = Config.Bind(
                "Main Settings",
                "Debug Box Height",
                40,
                "Change to Increase/Decrease GUI Box Size");

            new NewGamePatch().Enable();
        }

        private void CheckEftVersion()
        {
            // Make sure the version of EFT being run is the correct version
            int currentVersion = FileVersionInfo.GetVersionInfo(BepInEx.Paths.ExecutablePath).FilePrivatePart;
            int buildVersion = TarkovVersion.BuildVersion;
            if (currentVersion != buildVersion)
            {
                Logger.LogError($"ERROR: This version of {Info.Metadata.Name} v{Info.Metadata.Version} was built for Tarkov {buildVersion}, but you are running {currentVersion}. Please download the correct plugin version.");
                EFT.UI.ConsoleScreen.LogError($"ERROR: This version of {Info.Metadata.Name} v{Info.Metadata.Version} was built for Tarkov {buildVersion}, but you are running {currentVersion}. Please download the correct plugin version.");
                throw new Exception($"Invalid EFT Version ({currentVersion} != {buildVersion})");
            }
        }
    }

    //re-initializes each new game
    internal class NewGamePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));

        [PatchPrefix]
        public static void PatchPrefix()
        {
            BotDebug.BotDebugComponent.Enable();
        }
    }
}
