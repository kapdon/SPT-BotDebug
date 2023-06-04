using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Logging;
using Comfort.Common;
using DrakiaXYZ.BotDebug.Helpers;
using EFT;
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
        private float lastBotUpdate;
        private bool updateGuiPending = false;

        private Dictionary<string, BotData> botMap = new Dictionary<string, BotData>();
        private List<string> deadList = new List<string>();
        protected ManualLogSource Logger;

        long memAllocUpdate = 0;
        long memAllocGui = 0;
        float lastMemOutUpdate = 0;
        float lastMemOutGui = 0;
        float memTimeframe = 5.0f;

        private BotDebugComponent()
        {
            Logger = BepInEx.Logging.Logger.CreateLogSource(GetType().Name);
        }

        public void Awake()
        {
            botSpawner = (Singleton<IBotGame>.Instance).BotsController.BotSpawner;
            gameWorld = Singleton<GameWorld>.Instance;
            localPlayer = gameWorld.MainPlayer;

            Logger.LogInfo("BotDebugComponent enabled");
        }
        
        public void Dispose()
        {
            Logger.LogInfo("BotDebugComponent disabled");
            Destroy(this);
        }

        public void Update()
        {
            // Make sure we're enabled
            if (!Settings.Enable.Value || gameWorld == null)
            {
                return;
            }

            // Check if the user is hitting the Next Mode button
            if (Settings.NextModeKey.Value.IsDown())
            {
                Settings.ActiveMode.Value = Settings.ActiveMode.Value.Next();
                updateGuiPending = true;
            }
            else if (Settings.PrevModeKey.Value.IsDown())
            {
                Settings.ActiveMode.Value = Settings.ActiveMode.Value.Previous();
                updateGuiPending = true;
            }

            // Only update bot data once every second
            if (Time.time - lastBotUpdate < 1.0f)
            {
                return;
            }
            lastBotUpdate = Time.time;

#if DEBUG
            long startMem = GC.GetTotalMemory(false);
#endif

            // Add any missing bots to the dictionary, pulling the debug data from BSG classes
            foreach (Player player in gameWorld.AllPlayers)
            {
                var data = botSpawner.BotDebugData(localPlayer, player.ProfileId);
                if (!botMap.TryGetValue(player.ProfileId, out var botData))
                {
                    botData = new BotData();
                    botMap.Add(player.ProfileId, botData);
                }

                botData.SetData(data);
            }

            // Flag that the GUI needs to update its text
            updateGuiPending = true;

#if DEBUG
            memAllocUpdate += GC.GetTotalMemory(false) - startMem;
            if (Time.time - lastMemOutUpdate > memTimeframe)
            {
                Logger.LogDebug($"Update Memory Allocated ({memTimeframe}s): {Math.Floor(memAllocUpdate / 1024f)} KiB");
                memAllocUpdate = 0;
                lastMemOutUpdate = Time.time;
            }
#endif
        }

        private void OnGUI()
        {
            if (!Settings.Enable.Value)
            {
                return;
            }

#if DEBUG
            long startMem = GC.GetTotalMemory(false);
#endif

            if (guiStyle == null)
            {
                guiStyle = new GUIStyle(GUI.skin.box);
                guiStyle.alignment = TextAnchor.MiddleLeft;
                guiStyle.fontSize = 24;
                guiStyle.margin = new RectOffset(3, 3, 3, 3);
                guiStyle.richText = true;
            }

            foreach (var bot in botMap)
            {
                var botData = bot.Value.Data;
                if (!botData.InitedBotData) continue;

                // If the bot hasn't been updated in over 3 seconds, it's dead Jim, remove it
                if (Time.time - bot.Value.LastUpdate >= 3f)
                {
                    deadList.Add(bot.Key);
                    continue;
                }

                // Make sure we have a GuiContent and GuiRect object for this bot
                if (bot.Value.GuiContent == null)
                {
                    bot.Value.GuiContent = new GUIContent();
                }
                if (bot.Value.GuiRect == null)
                {
                    bot.Value.GuiRect = new Rect();
                }

                // Only draw the bot data if it's visible on screen
                Vector3 aboveBotHeadPos = botData.PlayerOwner.Transform.position + (Vector3.up * 1.5f);
                if (WorldToScreenPoint(aboveBotHeadPos, Camera.main, out Vector3 screenPos))
                {
                    if (updateGuiPending)
                    {
                        StringBuilder botInfoStringBuilder = BotInfo.GetInfoText(botData, localPlayer, Settings.ActiveMode.Value);
                        if (botInfoStringBuilder != null)
                        {
                            bot.Value.GuiContent.text = botInfoStringBuilder.ToString();
                        }
                        else
                        {
                            bot.Value.GuiContent.text = "";
                        }
                    }

                    if (bot.Value.GuiContent.text.Length > 0)
                    {
                        Vector2 guiSize = guiStyle.CalcSize(bot.Value.GuiContent);
                        bot.Value.GuiRect.x = screenPos.x - (guiSize.x / 2);
                        bot.Value.GuiRect.y = Screen.height - screenPos.y - guiSize.y;
                        bot.Value.GuiRect.size = guiSize;

                        GUI.Box(bot.Value.GuiRect, bot.Value.GuiContent, guiStyle);
                    }
                }
            }
            updateGuiPending = false;

            // Remove any dead bots, just to save processing later
            foreach (string deadBotKey in deadList)
            {
                botMap.Remove(deadBotKey);
            }
            deadList.Clear();

#if DEBUG
            memAllocGui += GC.GetTotalMemory(false) - startMem;
            if (Time.time - lastMemOutGui > memTimeframe)
            {
                Logger.LogDebug($"GUI Memory Allocated ({memTimeframe}s): {Math.Floor(memAllocGui / 1024f)} KiB");
                memAllocGui = 0;
                lastMemOutGui = Time.time;
            }
#endif
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
            public Rect GuiRect;
        }
    }


}
