using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx.Logging;
using Comfort.Common;
using DrakiaXYZ.BotDebug.Helpers;
using EFT;
using HarmonyLib;
using UnityEngine;
using BotDebugStruct = GStruct15;

namespace DrakiaXYZ.BotDebug.Components
{
    internal class BotDebugComponent : MonoBehaviour, IDisposable
    {
        private GameWorld gameWorld;
        private BotSpawnerClass botSpawner;
        private Player localPlayer;

        private GUIStyle guiStyle;

        private Dictionary<string, BotData> botMap = new Dictionary<string, BotData>();
        protected ManualLogSource Logger;

        // Hijack BotInfoDataPanel to create our data
        BotInfoDataPanel botInfoDataPanel = new BotInfoDataPanel();
        FieldInfo botInfoStringBuilderField;

        private BotDebugComponent()
        {
            Logger = BepInEx.Logging.Logger.CreateLogSource(GetType().Name);
            botInfoStringBuilderField = AccessTools.Field(typeof(BotInfoDataPanel), "stringBuilder_0");
        }

        public void Awake()
        {
            botSpawner = (Singleton<IBotGame>.Instance).BotsController.BotSpawner;
            gameWorld = Singleton<GameWorld>.Instance;
            localPlayer = gameWorld.MainPlayer;

            Logger.LogDebug("BotDebugComponent enabled");
        }
        
        public void Dispose()
        {
            Destroy(this);
            Logger.LogDebug("BotDebugComponent disabled");
        }

        public void Update()
        {
            // Make sure we're enabled
            if (!Settings.Enable.Value || gameWorld == null)
            {
                return;
            }

            // Add any missing bots to the dictionary, pulling the debug data from BSG classes
            foreach (Player player in gameWorld.AllPlayers)
            {
                var data = botSpawner.BotDebugData(localPlayer, player.ProfileId);
                if (!botMap.TryGetValue(player.ProfileId, out var botData))
                {
                    Logger.LogInfo($"Adding bot {player.name}");
                    botData = new BotData();
                    botMap.Add(player.ProfileId, botData);
                }

                botData.SetData(data);
            }

            // Check if the user is hitting the Next Mode button
            if (Settings.NextModeKey.Value.IsDown())
            {
                Settings.ActiveMode.Value = Settings.ActiveMode.Value.Next();
            }
            else if (Settings.PrevModeKey.Value.IsDown())
            {
                Settings.ActiveMode.Value = Settings.ActiveMode.Value.Previous();
            }
        }

        private void OnGUI()
        {
            if (Settings.Enable.Value)
            {
                if (guiStyle == null)
                {
                    guiStyle = new GUIStyle(GUI.skin.box);
                    guiStyle.alignment = TextAnchor.MiddleRight;
                    guiStyle.fontSize = 24;
                    guiStyle.margin = new RectOffset(3, 3, 3, 3);
                    guiStyle.richText = true;
                }

                List<string> deadList = new List<string>();

                foreach (var bot in botMap)
                {
                    var botData = bot.Value.Data;
                    if (!botData.InitedBotData) continue;

                    // If the bot hasn't been updated in over 3 seconds, it's dead Jim, remove it
                    if (Time.time - bot.Value.LastUpdate >= 3f)
                    {
                        Logger.LogInfo($"Removing {botData.Name}  {Time.time} - {bot.Value.LastUpdate}");
                        deadList.Add(bot.Key);
                        continue;
                    }

                    // Make sure we have a GuiContent object for this bot
                    if (bot.Value.GuiContent == null)
                    {
                        bot.Value.GuiContent = new GUIContent();
                    }

                    // Only draw the bot data if it's visible on screen
                    if (WorldToScreenPoint(botData.PlayerOwner.Transform.position, Camera.main, out Vector3 screenPos))
                    {
                        int dist = Mathf.RoundToInt((botData.PlayerOwner.Transform.position - localPlayer.Transform.position).magnitude);

                        // Directly utilize the StringBuilder, so we can add data to it without allocating our own memory
                        if (botInfoDataPanel.GetInfoText(botData, Settings.ActiveMode.Value, true) != null)
                        {
                            StringBuilder botInfoStringBuilder = botInfoStringBuilderField.GetValue(botInfoDataPanel) as StringBuilder;

                            // Add distance to the Behaviour state
                            if (Settings.ActiveMode.Value == EBotInfoMode.Behaviour)
                            {
                                botInfoStringBuilder.AppendLabeledValue("Dist", dist.ToString(), Color.white, Color.white, true);
                            }
                            // Otherwise, add the ID/Strategy to any other layer
                            else
                            {
                                botInfoStringBuilder.AppendLabeledValue("Id, Strategy", $"{botData.Id} {botData.StrategyName}", Color.white, Color.white, true);
                            }

                            bot.Value.GuiContent.text = botInfoStringBuilder.ToString();
                            Vector2 guiSize = guiStyle.CalcSize(bot.Value.GuiContent);

                            Rect guiRect = new Rect(
                                screenPos.x - (guiSize.x / 2),
                                Screen.height - screenPos.y - guiSize.y,
                                guiSize.x,
                                guiSize.y);
                            GUI.Box(guiRect, bot.Value.GuiContent, guiStyle);
                        }
                        else
                        {
                            bot.Value.GuiContent.text = "";
                        }
                    }
                }

                // Remove any dead bots, just to save processing later
                foreach (string deadBotKey in deadList)
                {
                    botMap.Remove(deadBotKey);
                }
            }
        }

        bool WorldToScreenPoint(Vector3 worldPoint, Camera camera, out Vector3 screenPoint)
        {
            // calculate view-projection using non-jitter matrices
            Matrix4x4 viewProjMtx = camera.nonJitteredProjectionMatrix * camera.worldToCameraMatrix;

            // multiply world point by view-projection
            Vector4 clipPoint = viewProjMtx * new Vector4(worldPoint.x, worldPoint.y, worldPoint.z, 1f);

            if (clipPoint.w == 0f)
            {
                // point is undefined on camera focus point
                screenPoint = Vector3.zero;
                return false;
            }
            else
            {
                // check if object is in front of the camera
                var heading = worldPoint - camera.transform.position;
                bool inFront = (Vector3.Dot(camera.transform.forward, heading) > 0f);

                // convert x and y from clip space to screen coordinates
                clipPoint.x = (clipPoint.x / clipPoint.w + 1f) * .5f * camera.pixelWidth;
                clipPoint.y = (clipPoint.y / clipPoint.w + 1f) * .5f * camera.pixelHeight;
                screenPoint = new Vector3(clipPoint.x, clipPoint.y, worldPoint.z);
                return inFront;
            }
        }

        public static void Enable()
        {
            if (Singleton<IBotGame>.Instantiated)
            {
                var gameWorld = Singleton<GameWorld>.Instance;
                gameWorld.GetOrAddComponent<BotDebugComponent>();
            }
        }

        public static void Disable()
        {
            if (Singleton<IBotGame>.Instantiated)
            {
                var gameWorld = Singleton<GameWorld>.Instance;
                gameWorld.GetComponent<BotDebugComponent>()?.Dispose();
            }
        }

        internal class BotData
        {
            public void SetData(BotDebugStruct botData)
            {
                LastUpdate = Time.time;
                Data = botData;
            }

            public float LastUpdate;
            public BotDebugStruct Data;
            public GUIContent GuiContent;
        }
    }


}
