using BepInEx.Logging;
using EFT;
using System;
using System.Text;
using UnityEngine;
using ActorDataStruct = GStruct15;
using BotDataStruct = GStruct14;
using HealthDataStruct = GStruct13;
using EnemyInfoClass = GClass475;

namespace DrakiaXYZ.BotDebug
{
    internal class BotInfo
    {
        private static readonly StringBuilder stringBuilder = new StringBuilder();
        private static readonly string greyTextColor = new Color(0.8f, 0.8f, 0.8f).GetRichTextColor();
        private static readonly string greenTextColor = new Color(0.25f, 1f, 0.2f).GetRichTextColor();
        private static ManualLogSource Logger;

        public static StringBuilder GetInfoText(ActorDataStruct actorDataStruct, Player localPlayer, EBotInfoMode mode)
        {
            if (Logger == null)
            {
                Logger = BepInEx.Logging.Logger.CreateLogSource(sourceName: typeof(BotInfo).Name);
            }

            Color botNameColor = Color.white;
            if (actorDataStruct.PlayerOwner != null)
            {
                botNameColor = Color.green;
                foreach (EnemyInfoClass enemyInfo in actorDataStruct.PlayerOwner.AIData.BotOwner.EnemiesController.EnemyInfos.Values)
                {
                    if (enemyInfo.ProfileId == localPlayer.ProfileId)
                    {
                        botNameColor = Color.red;
                        break;
                    }
                }
            }

            switch (mode)
            {
                case EBotInfoMode.Behaviour:
                    return GetBehaviour(actorDataStruct, botNameColor, localPlayer);
                case EBotInfoMode.BattleState:
                    return GetBattleState(actorDataStruct, botNameColor);
                case EBotInfoMode.Health:
                    return GetHealth(actorDataStruct, botNameColor);
                case EBotInfoMode.Specials:
                    return GetSpecial(actorDataStruct, botNameColor);
                case EBotInfoMode.Custom:
                    return GetCustom(actorDataStruct, botNameColor);
                default:
                    return null;
            }
        }

        private static string GetBlackoutLabel(bool val)
        {
            return val ? "(BL)" : "";
        }

        private static string GetBrokenLabel(bool val)
        {
            return val ? "(Broken)" : "";
        }

        private static StringBuilder GetCustom(ActorDataStruct actorDataStruct, Color botNameColor)
        {
            BotDataStruct botData = actorDataStruct.BotData;
            stringBuilder.Clear();
            stringBuilder.AppendLabeledValue("Bot (Brain)", $"{botData.Name} ({botData.StrategyName})", Color.white, botNameColor, false);
            stringBuilder.AppendLabeledValue("Layer", botData.LayerName, Color.white, Color.white, true);
            stringBuilder.AppendLabeledValue("Nickname", actorDataStruct.PlayerOwner?.Profile.Nickname, Color.white, Color.white, true);
            if (string.IsNullOrEmpty(botData.CustomData))
            {
                stringBuilder.AppendLine("No Custom Data");
            }
            else
            {
                stringBuilder.AppendLine(botData.CustomData);
            }
            return stringBuilder;
        }

        private static StringBuilder GetSpecial(ActorDataStruct actorDataStruct, Color botNameColor)
        {
            BotDataStruct botData = actorDataStruct.BotData;
            stringBuilder.Clear();
            stringBuilder.AppendLabeledValue("Bot (Brain)", $"{botData.Name} ({botData.StrategyName})", Color.white, botNameColor, false);

            for (int i = 0; i < actorDataStruct.ProfileId.Length; i += 12)
            {
                int chunkSize = Math.Min(12, actorDataStruct.ProfileId.Length - i);
                if (i == 0)
                {
                    stringBuilder.AppendLabeledValue("Id", actorDataStruct.ProfileId.Substring(0, chunkSize), Color.white, Color.white, true);
                }
                else
                {
                    stringBuilder.AppendLabeledValue("", actorDataStruct.ProfileId.Substring(i, chunkSize), Color.white, Color.white, false);
                }
            }
            
            stringBuilder.AppendLabeledValue("WeapSpawn", $"{botData.IsInSpawnWeapon}", Color.white, Color.white, true);
            stringBuilder.AppendLabeledValue("AxeEnemy", $"{botData.HaveAxeEnemy}", Color.white, Color.white, true);
            stringBuilder.AppendLabeledValue("Role", $"{botData.BotRole}", Color.white, Color.white, true);
            stringBuilder.AppendLabeledValue("State", $"{botData.PlayerState}", Color.white, Color.white, true);
            string data;
            try
            {
                data = actorDataStruct.PlayerOwner.MovementContext.CurrentState.Name.ToString();
            }
            catch (Exception)
            {
                data = "no data";
            }
            stringBuilder.AppendLabeledValue("StateLc", data, Color.white, Color.white, true);
            stringBuilder.AppendLabeledValue("Shoot", actorDataStruct.BotData.ToString(), Color.white, Color.white, true);
            return stringBuilder;
        }

        private static StringBuilder GetBehaviour(ActorDataStruct actorDataStruct, Color botNameColor, Player localPlayer)
        {
            BotDataStruct botData = actorDataStruct.BotData;
            stringBuilder.Clear();

            stringBuilder.AppendLabeledValue("Bot (Brain)", $"{botData.Name} ({botData.StrategyName})", Color.white, botNameColor, false);
            stringBuilder.AppendLabeledValue("Layer", botData.LayerName, Color.yellow, Color.yellow, true);
            stringBuilder.AppendLabeledValue("Node", botData.NodeName, Color.white, Color.white, true);
            stringBuilder.AppendLabeledValue("EnterBy", botData.Reason, greenTextColor, greenTextColor, true);
            if (!string.IsNullOrEmpty(botData.PrevNodeName))
            {
                stringBuilder.AppendLabeledValue("PrevNode", botData.PrevNodeName, greyTextColor, greyTextColor, true);
                stringBuilder.AppendLabeledValue("ExitBy", botData.PrevNodeExitReason, greyTextColor, greyTextColor, true);
            }

            if (actorDataStruct.PlayerOwner != null)
            {
                int dist = Mathf.RoundToInt((actorDataStruct.PlayerOwner.Transform.position - localPlayer.Transform.position).magnitude);
                stringBuilder.AppendLabeledValue("Dist", $"{dist}", Color.white, Color.white, true);
            }

            return stringBuilder;
        }

        private static StringBuilder GetBattleState(ActorDataStruct actorDataStruct, Color botNameColor)
        {
            BotDataStruct botData = actorDataStruct.BotData;
            stringBuilder.Clear();
            stringBuilder.AppendLabeledValue("Bot (Brain)", $"{botData.Name} ({botData.StrategyName})", Color.white, botNameColor, false);
            try
            {
                if (actorDataStruct.PlayerOwner != null)
                {
                    Player.FirearmController firearmController = actorDataStruct.PlayerOwner.GetPlayer.HandsController as Player.FirearmController;
                    if (firearmController != null)
                    {
                        int chamberAmmoCount = firearmController.Item.ChamberAmmoCount;
                        int currentMagazineCount = firearmController.Item.GetCurrentMagazineCount();
                        stringBuilder.AppendLabeledValue("Ammo", $"C: {chamberAmmoCount} M: {currentMagazineCount} T: {botData.Ammo}", Color.white, Color.white, true);
                    }
                    stringBuilder.AppendLabeledValue("Hits", $"{botData.HitsOnMe} / {botData.ShootsOnMe}", Color.white, Color.white, true);
                    stringBuilder.AppendLabeledValue("Reloading", $"{botData.Reloading}", Color.white, Color.white, true);
                    stringBuilder.AppendLabeledValue("CoverId", $"{botData.CoverIndex}", Color.white, Color.white, true);
                }
                else
                {
                    stringBuilder.Append("no battle data");
                }
            }
            catch (Exception ex)
            {
                stringBuilder.AppendLabeledValue("Error", "Debug panel firearms error", Color.red, Color.red, true);
                Logger.LogError(ex);
            }

            if (actorDataStruct.PlayerOwner != null)
            {
                var goalEnemy = actorDataStruct.PlayerOwner.AIData.BotOwner.Memory.GoalEnemy;
                if (goalEnemy?.Person?.IsAI == true)
                {
                    stringBuilder.AppendLabeledValue("GoalEnemy", $"{goalEnemy?.Person?.AIData?.BotOwner?.name}", Color.white, Color.white, true);
                }
                else
                {
                    stringBuilder.AppendLabeledValue("GoalEnemy", $"{goalEnemy?.Person?.GetPlayer?.Profile?.Nickname}", Color.white, Color.white, true);
                }
            }

            return stringBuilder;
        }

        private static StringBuilder GetHealth(ActorDataStruct actorDataStruct, Color botNameColor)
        {
            BotDataStruct botData = actorDataStruct.BotData;
            HealthDataStruct healthData = actorDataStruct.HeathsData;
            stringBuilder.Clear();
            stringBuilder.AppendLabeledValue("Bot (Brain)", $"{botData.Name} ({botData.StrategyName})", Color.white, botNameColor, false);
            stringBuilder.AppendLabeledValue("Head", $"{healthData.HealthHead}{GetBlackoutLabel(healthData.HealthHeadBL)}{GetBrokenLabel(healthData.HealthHeadBroken)}", Color.white, Color.white, true);
            stringBuilder.AppendLabeledValue("Chest", $"{healthData.HealthBody}{GetBlackoutLabel(healthData.HealthBodyBL)}{GetBrokenLabel(healthData.HealthHeadBroken)}", Color.white, Color.white, true);
            stringBuilder.AppendLabeledValue("Stomach", $"{healthData.HealthStomach}{GetBlackoutLabel(healthData.HealthStomachBL)}", Color.white, Color.white, true);
            stringBuilder.AppendLabeledValue("Arms", $"{healthData.HealthLeftArm}{GetBlackoutLabel(healthData.HealthLeftArmBL)}{GetBrokenLabel(healthData.HealthLeftArmBroken)} {healthData.HealthRightArm}{GetBlackoutLabel(healthData.HealthRightArmBL)}{GetBrokenLabel(healthData.HealthRightArmBroken)}", Color.white, Color.white, true);
            stringBuilder.AppendLabeledValue("Legs", $"{healthData.HealthLeftLeg}{GetBlackoutLabel(healthData.HealthLeftLegBL)}{GetBrokenLabel(healthData.HealthLeftLegBroken)} {healthData.HealthRightLeg}{GetBlackoutLabel(healthData.HealthRightLegBL)}{GetBrokenLabel(healthData.HealthRightLegBroken)}", Color.white, Color.white, true);
            return stringBuilder;
        }
    }
}
