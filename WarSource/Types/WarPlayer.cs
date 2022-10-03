﻿using BrokeProtocol.API;
using BrokeProtocol.Entities;
using BrokeProtocol.Managers;
using BrokeProtocol.Utility;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrokeProtocol.GameSource.Types
{
    public class WarSourcePlayer
    {
        public ShPlayer player;

        public bool changePending;
        public int spawnTerritoryIndex;
        public int teamIndex;
        public int classIndex;

        public WarSourcePlayer(ShPlayer player)
        {
            this.player = player;
        }

        public bool SetSpawnTerritory()
        {
            var curSpawnIndex = spawnTerritoryIndex;

            if (curSpawnIndex < 0 ||
                Manager.territories[curSpawnIndex].ownerIndex != player.svPlayer.spawnJobIndex)
            {
                var territories = WarPlayer.GetTerritories(player.svPlayer.spawnJobIndex);
                if (territories.Count() > 0)
                    spawnTerritoryIndex = territories.GetRandom();
                else
                    spawnTerritoryIndex = -1;

                return spawnTerritoryIndex != curSpawnIndex;
            }
            return false;
        }
    }
    
    public class WarPlayer : PlayerEvents
    {
        [Execution(ExecutionMode.Additive)]
        public override bool Initialize(ShEntity entity)
        {
            var player = entity.Player;

            if (player)
            {
                var warSourcePlayer = new WarSourcePlayer(player);

                WarManager.pluginPlayers.Add(player, warSourcePlayer);

                if (!player.isHuman ||
                    !SvManager.Instance.connections.TryGetValue(player.svPlayer.connection, out var connectData) ||
                    !connectData.customData.TryFetchCustomData(WarManager.teamIndexKey, out int teamIndex) ||
                    !connectData.customData.TryFetchCustomData(WarManager.classIndexKey, out int classIndex))
                {
                    teamIndex = player.svPlayer.spawnJobIndex;
                    classIndex = Random.Range(0, WarManager.classes[teamIndex].Count);
                }

                warSourcePlayer.teamIndex = teamIndex;
                warSourcePlayer.classIndex = classIndex;

                foreach (var i in WarManager.classes[warSourcePlayer.teamIndex][warSourcePlayer.classIndex].equipment)
                {
                    player.TransferItem(DeltaInv.AddToMe, i.itemName.GetPrefabIndex(), i.count);
                }
            }
            return true;
        }

        [Execution(ExecutionMode.Additive)]
        public override bool Destroy(ShEntity entity)
        {
            WarManager.pluginPlayers.Remove(entity.Player);
            return true;
        }

        public static IEnumerable<int> GetTerritories(int team)
        {
            var territories = new List<int>();
            var index = 0;
            foreach (var t in Manager.territories)
            {
                if (t.ownerIndex == team)
                {
                    territories.Add(index);
                }
                index++;
            }

            return territories;
        }

        public const string spawnMenuID = "SpawnMenu";

        [Execution(ExecutionMode.Additive)]
        public override bool TextPanelButton(ShPlayer player, string menuID, string optionID)
        {
            if (menuID.StartsWith(spawnMenuID))
            {
                if (int.TryParse(optionID, out var index) && 
                    index < Manager.territories.Count && 
                    Manager.territories[index].ownerIndex == player.svPlayer.spawnJobIndex)
                {
                    player.WarPlayer().spawnTerritoryIndex = index;
                }
            }

            return true;
        }


        [Execution(ExecutionMode.Additive)]
        public override bool Spawn(ShEntity entity)
        {
            entity.Player.svPlayer.SetBestEquipable();
            return true;
        }

        // Override because we don't want to switch back to Hands
        [Execution(ExecutionMode.Override)]
        public override bool Respawn(ShEntity entity)
        {
            var player = entity.Player;

            if (player.isHuman)
            {
                // Back to spectate self on Respawn
                player.svPlayer.SvSpectate(player);
            }

            entity.svEntity.Restock(); // Will put on any suitable clothing
            return true;
        }

        // Test Event (disallow losing Exp/Job in PvP)
        [Execution(ExecutionMode.Test)]
        public override bool Reward(ShPlayer player, int experienceDelta, int moneyDelta) => 
            experienceDelta >= 0;

        [Execution(ExecutionMode.Additive)]
        public override bool OptionAction(ShPlayer player, int targetID, string id, string optionID, string actionID)
        {
            switch (id)
            {
                case WarManager.selectTeam:
                    {
                        var teamIndex = 0;
                        foreach (var c in BPAPI.Jobs)
                        {
                            if (c.shared.jobName == optionID)
                            {
                                var warSourcePlayer = player.WarPlayer();
                                warSourcePlayer.teamIndex = teamIndex;
                                player.svPlayer.DestroyMenu(WarManager.selectTeam);
                                WarManager.SendClassSelectMenu(player.svPlayer.connection, teamIndex);
                                warSourcePlayer.changePending = true;
                                break;
                            }
                            teamIndex++;
                        }
                    }
                    break;

                case WarManager.selectClass:
                    {
                        var warSourcePlayer = player.WarPlayer();
                        var classIndex = 0;
                        foreach (var c in WarManager.classes[warSourcePlayer.teamIndex])
                        {
                            if (c.className == optionID)
                            {
                                warSourcePlayer.classIndex = classIndex;
                                player.svPlayer.DestroyMenu(WarManager.selectClass);
                                warSourcePlayer.changePending = true;
                                break;
                            }
                            classIndex++;
                        }
                    }
                    break;
            }
            return true;
        }


        // Override to Drop items without removing them from player
        [Execution(ExecutionMode.Override)]
        public override bool RemoveItemsDeath(ShPlayer player, bool dropItems)
        {
            if (dropItems)
            {
                var briefcase = player.svPlayer.SpawnBriefcase();

                if (briefcase)
                {
                    foreach (var invItem in player.myItems.Values)
                    {
                        if (Random.value < 0.8f)
                        {
                            invItem.count = Mathf.CeilToInt(invItem.count * Random.Range(0.05f, 0.3f));
                            briefcase.myItems.Add(invItem.item.index, invItem);
                        }
                    }
                }
            }

            return true;
        }
    }
}
