﻿using BrokeProtocol.API;
using BrokeProtocol.Entities;
using BrokeProtocol.Managers;
using BrokeProtocol.Required;
using BrokeProtocol.Utility;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BrokeProtocol.GameSource.Types
{
    public class WarMovable : MovableEvents
    {
        [Execution(ExecutionMode.Override)]
        public override bool Respawn(ShEntity entity)
        {
            var player = entity.Player;
            
            if (player)
            {
                var warSourcePlayer = player.WarPlayer();
                if (warSourcePlayer.changePending)
                {
                    warSourcePlayer.changePending = false;

                    player.svPlayer.spawnJobIndex = warSourcePlayer.teamIndex;

                    // Remove all inventory (will be re-added either here or on spawn)
                    foreach (var i in player.myItems.ToArray())
                    {
                        player.TransferItem(DeltaInv.RemoveFromMe, i.Key, i.Value.count);
                    }
                    var newPlayer = WarManager.skinPrefabs[warSourcePlayer.teamIndex].GetRandom();
                    player.svPlayer.ApplyWearableIndices(newPlayer.wearableOptions);

                    // Clamp class if it's outside the range on team change
                    warSourcePlayer.classIndex = Mathf.Clamp(
                        warSourcePlayer.classIndex,
                        0,
                        WarManager.classes[warSourcePlayer.teamIndex].Count - 1);

                    foreach (var i in WarManager.classes[warSourcePlayer.teamIndex][warSourcePlayer.classIndex].equipment)
                    {
                        player.TransferItem(DeltaInv.AddToMe, i.itemName.GetPrefabIndex(), i.count);
                    }

                    // Set null so it will be reset on Spawn
                    player.svPlayer.defaultItems = null;
                }

                if(!player.isHuman)
                {
                    // Pick a new random spawn territory for NPCs
                    warSourcePlayer.spawnTerritoryIndex = -1;
                    warSourcePlayer.SetSpawnTerritory();
                }

                var territoryIndex = warSourcePlayer.spawnTerritoryIndex;

                if (WarUtility.GetValidTerritoryPosition(territoryIndex, out var position, out var rotation, out var place))
                {
                    player.svEntity.originalPosition = position;
                    player.svEntity.originalRotation = rotation;
                    player.svEntity.originalParent = place.mTransform;
                }
            }

            entity.svEntity.instigator = null; // So players aren't charged with Murder crimes after vehicles reset

            entity.svEntity.SpawnOriginal();

            return true;
        }

        [Execution(ExecutionMode.Override)]
        public override bool Death(ShDestroyable destroyable, ShPlayer attacker)
        {
            ShManager.Instance.StartCoroutine(DeathLoop(destroyable));
            return true;
        }

        private LabelID[] GetSpawnOptions(ShPlayer player)
        {
            var options = new List<LabelID>();
            foreach (var territoryIndex in WarPlayer.GetTerritories(player.svPlayer.spawnJobIndex))
            {
                var locationName = WarUtility.GetTerritoryName(territoryIndex);

                options.Add(new LabelID(locationName, territoryIndex.ToString()));
            }

            return options.ToArray();
        }

        private void SendSpawnMenu(WarSourcePlayer warPlayer)
        {
            if (!warPlayer.player.isHuman)
                return;

            var sb = new StringBuilder();
            sb.AppendLine("Spawn Select");
            sb.AppendLine("Current Spawn:");
            if(warPlayer.spawnTerritoryIndex >= 0)
            {
                sb.AppendLine(WarUtility.GetTerritoryName(warPlayer.spawnTerritoryIndex));
            }
            else
            {
                sb.AppendLine("None");
            }
            warPlayer.player.svPlayer.SendTextPanel(sb.ToString(), WarPlayer.spawnMenuID, GetSpawnOptions(warPlayer.player));
        }

        private IEnumerator DeathLoop(ShDestroyable destroyable)
        {
            if(destroyable.Player &&
                WarManager.pluginPlayers.TryGetValue(destroyable.Player, out var warSourcePlayer))
            {
                SendSpawnMenu (warSourcePlayer);
            }
            else
            {
                warSourcePlayer = null;
            }
            
            var respawnTime = Time.time + destroyable.svDestroyable.RespawnTime;

            while (destroyable && destroyable.IsDead)
            {
                if (warSourcePlayer != null && warSourcePlayer.SetSpawnTerritory())
                {
                    SendSpawnMenu(warSourcePlayer);
                }

                if (Time.time >= respawnTime)
                {
                    if (warSourcePlayer == null || warSourcePlayer.spawnTerritoryIndex >= 0)
                    {
                        destroyable.svDestroyable.DestroyEffect();
                        destroyable.svDestroyable.Respawn();
                        break;
                    }
                }

                yield return null;
            }

            if (destroyable && destroyable.Player && destroyable.isHuman)
            {
                destroyable.Player.svPlayer.DestroyTextPanel(WarPlayer.spawnMenuID);
            }
        }
    }
}
