﻿using System;
using System.Reflection;
using Aki.Reflection.Patching;
using BepInEx;
using DrakiaXYZ.BotDebug.Components;
using DrakiaXYZ.BotDebug.Helpers;
using DrakiaXYZ.BotDebug.VersionChecker;
using EFT;
using HarmonyLib;
using UnityEngine;

namespace DrakiaXYZ.BotDebug
{
    [BepInPlugin("xyz.drakia.botdebug", "DrakiaXYZ-BotDebug", "1.3.0")]
#if !STANDALONE
 //   [BepInDependency("com.spt-aki.core", "3.8.0")]
    [BepInDependency("xyz.drakia.bigbrain", "0.4.0")]
#endif
    public class BotDebugPlugin : BaseUnityPlugin
    {
        private void Awake()
        {
#if !STANDALONE
            if (!TarkovVersion.CheckEftVersion(Logger, Info, Config))
            {
                throw new Exception($"Invalid EFT Version");
            }
#endif

            Settings.Init(Config);

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
