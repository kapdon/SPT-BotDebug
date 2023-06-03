using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using UnityEngine;
using BotDebugStruct = GStruct15;

namespace DrakiaXYZ.BotDebug.Components
{
    internal class BotDebugComponent : MonoBehaviour
    {
        private static GameWorld gameWorld;
        private static BotSpawnerClass botSpawner;
        private static Player localPlayer;

        private GUIStyle guiStyle;

        private Dictionary<string, BotData> botMap = new Dictionary<string, BotData>();

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
            if (!BotDebugPlugin.Enable.Value || gameWorld == null)
            {
                return;
            }

            if (localPlayer == null)
            {
                localPlayer = GetPlayer();
            }

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
        }

        private Player GetPlayer()
        {
            foreach (var player in gameWorld.RegisteredPlayers)
            {
                // See if this is a ClientPlayer object
                var clientPlayer = player as ClientPlayer;
                if (clientPlayer != null)
                {
                    return clientPlayer;
                }

                // See if this is a LocalPlayer object, and isn't AI
                LocalPlayer localPlayer = player as LocalPlayer;
                if (localPlayer != null && !localPlayer.AIData.IsAI)
                {
                    return localPlayer;
                }
            }

            return null;
        }

        public static void Enable()
        {
            if (Singleton<IBotGame>.Instantiated)
            {
                botSpawner = (Singleton<IBotGame>.Instance).BotsController.BotSpawner;
                gameWorld = Singleton<GameWorld>.Instance;

                gameWorld.GetOrAddComponent<BotDebugComponent>();

                Logger.LogDebug("BotDebugComponent enabled");
            }
        }

        private void OnGUI()
        {
            if (BotDebugPlugin.Enable.Value)
            {
                if (guiStyle == null)
                {
                    guiStyle = new GUIStyle(GUI.skin.box);
                    guiStyle.alignment = TextAnchor.MiddleRight;
                    guiStyle.fontSize = 24;
                    guiStyle.margin = new RectOffset(3, 3, 3, 3);
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

                    // Only draw the bot data if it's visible on screen
                    if (WorldToScreenPoint(botData.PlayerOwner.Transform.position, Camera.main, out Vector3 screenPos))
                    {
                        int dist = Mathf.RoundToInt((botData.PlayerOwner.Transform.position - localPlayer.Transform.position).magnitude);
                        string guiText = $"Bot: {botData.Name}\n";
                        guiText += $"Brain: {botData.StrategyName}\n";
                        guiText += $"Layer: {botData.LayerName}\n";
                        guiText += $"Action: {botData.NodeName} ({botData.Reason})\n";
                        guiText += $"Distance: {dist}";

                        if (bot.Value.GuiContent == null)
                        { 
                            bot.Value.GuiContent = new GUIContent();
                        }

                        bot.Value.GuiContent.text = guiText;
                        Vector2 guiSize = guiStyle.CalcSize(bot.Value.GuiContent);
                        //Vector3 screenPos = Camera.main.WorldToScreenPoint(position);

                        Rect guiRect = new Rect(
                            screenPos.x - (guiSize.x / 2),
                            Screen.height - screenPos.y - guiSize.y,
                            guiSize.x,
                            guiSize.y);
                        GUI.Box(guiRect, bot.Value.GuiContent, guiStyle);
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

        private static string GetBotNumber(Player player)
        {
            // its the players gameobject name
            return player.AIData.BotOwner.gameObject.name;
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
