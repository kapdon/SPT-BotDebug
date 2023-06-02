using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Aki.Reflection.Patching;
using BepInEx.Logging;
using Comfort.Common;
using BotDebug;
using EFT;
using EFT.Ballistics;
using EFT.Interactive;
using EFT.InventoryLogic;
using HarmonyLib;
using HarmonyLib.Tools;
using UnityEngine;

namespace BotDebug
{
    internal class BotDebugComponent : MonoBehaviour
    {
        private static GameWorld gameWorld;
        private static Dictionary<Player, string> botNumbers = new Dictionary<Player, string>();
        private static Dictionary<Player, string> currentBrainLayers = new Dictionary<Player, string>();
        private static Vector3 offset = new Vector3(0, 2.5f, 0);
        private static Color textColor = Color.red;
        protected static ManualLogSource Logger
        {
            get; private set;
        }

        private BotDebugComponent()
        {
            if (Logger == null)
            {
                Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(BotDebugComponent));
            }
        }

        public void Update()
        {
            // Update the bot information based on gameWorld.RegisteredPlayers or any other source of bot data
            foreach (Player player in gameWorld.RegisteredPlayers)
            {
                if (!player.IsYourPlayer)
                {
                    botNumbers[player] = GetBotNumber(player);
                    currentBrainLayers[player] = player.AIData.BotOwner.Brain.GetStateName;
                }
            }

        }

        public static void Enable()
        {
            if (Singleton<IBotGame>.Instantiated)
            {
                gameWorld = Singleton<GameWorld>.Instance;
                gameWorld.GetOrAddComponent<BotDebugComponent>();

                Logger.LogDebug("BotDebugComponent enabled");
            }
        }

        private void OnGUI()
        {
            GUIStyle textStyle = new GUIStyle(GUI.skin.box);
            textStyle.normal.textColor = textColor;

            foreach (var kvp in botNumbers)
            {
                Player player = kvp.Key;

                //draw only if the bot is still alive
                if (!player.AIData.BotOwner.IsDead)
                {
                    string botNumber = kvp.Value;
                    string currentBrainLayer = currentBrainLayers[player];

                    Vector3 position = player.gameObject.transform.position + Vector3.up * 2.5f;
                    Vector3 screenPos = Camera.main.WorldToScreenPoint(position);

                    GUI.Box(new Rect(screenPos.x - 50, Screen.height - screenPos.y, 100, BotDebug.BotDebugPlugin.debugBoxHeight.Value),
                        $"Bot: {botNumber}\n" +
                        $"Layer: {currentBrainLayer}\n");
                }
                

            }
        }

        private static string GetBotNumber(Player player)
        {
            // its the players gameobject name
            return player.AIData.BotOwner.gameObject.name;
        }


    }


}
