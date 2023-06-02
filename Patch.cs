using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using UnityEngine;

namespace BotDebug
{
    internal class BotDebugComponent : MonoBehaviour
    {
        private static GameWorld gameWorld;
        private static Dictionary<Player, string> botNumbers = new Dictionary<Player, string>();
        private static Dictionary<Player, string> baseBrain = new Dictionary<Player, string>();
        //private static Dictionary<Player, string> currentBrainLayers = new Dictionary<Player, string>();
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
            if (BotDebug.BotDebugPlugin.EnableGui.Value)
            {
                try
                {
                    // Update the bot information based on gameWorld.RegisteredPlayers or any other source of bot data
                    foreach (Player player in gameWorld.RegisteredPlayers)
                    {
                        if (!player.IsYourPlayer)
                        {
                            botNumbers[player] = GetBotNumber(player);
                            baseBrain[player] = player.AIData.BotOwner.Brain.BaseBrain.ShortName();
                            //currentBrainLayers[player] = player.AIData.BotOwner.Brain.GetStateName;
                        }
                    }
                }
                catch { }

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
            if (BotDebug.BotDebugPlugin.EnableGui.Value)
            {
                GUIStyle textStyle = new GUIStyle(GUI.skin.box);
                textStyle.normal.textColor = textColor;

                foreach (var bot in botNumbers)
                {
                    Player player = bot.Key;

                    //draw only if the bot is still alive
                    if (!player.AIData.BotOwner.IsDead && 
                        player.isActiveAndEnabled &&
                        player.IsVisible)
                    {
                        string botNumber = bot.Value;
                        //string currentBrainLayer = currentBrainLayers[player];
                        string brain = baseBrain[player];

                        Vector3 position = player.gameObject.transform.position + Vector3.up * 2.5f;
                        Vector3 screenPos = Camera.main.WorldToScreenPoint(position);


                        GUI.Box(new Rect(screenPos.x - 50, Screen.height - screenPos.y, BotDebug.BotDebugPlugin.debugBoxWidth.Value, BotDebug.BotDebugPlugin.debugBoxHeight.Value),
                        $"Bot: {botNumber}\n" +
                        $"Base Brain: {brain}\n");

                        //$"Layer: {currentBrainLayer}\n");
                        
                    }


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
