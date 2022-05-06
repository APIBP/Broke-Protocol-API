﻿using BrokeProtocol.API;
using BrokeProtocol.Collections;
using BrokeProtocol.Entities;
using BrokeProtocol.GameSource.Types;
using BrokeProtocol.Managers;
using BrokeProtocol.Required;
using BrokeProtocol.Utility;
using BrokeProtocol.Utility.Networking;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BrokeProtocol.WarSource.Types
{
    public class TerritoryState
    {
        private ShTerritory territory;
        public float lastSpeed;
        public float captureState;
        public IDCollection<ShPlayer> players = new IDCollection<ShPlayer>();

        public TerritoryState(ShTerritory territory)
        {
            this.territory = territory;
        }

        public float CaptureSpeed()
        {
            int count = 0;

            foreach(var p in players)
            {
                if (p.svPlayer.job.info.shared.jobIndex == territory.ownerIndex)
                    count++;
                else
                    count--;
            }

            return count * 0.1f;
        }

        public void Update()
        {
            foreach(var p in players.ToArray())
            {
                if (!p.isActiveAndEnabled || p.IsDead || Manager.GetTerritory(p) != territory)
                {
                    p.svPlayer.SvProgressStop();
                    players.Remove(p);
                }
            }

            var newSpeed = CaptureSpeed();

            if (newSpeed > 0f && captureState >= 1f || newSpeed < 0f && captureState <= 0f)
            {
                return;
            }

            if (newSpeed != 0f)
            {
                captureState += newSpeed * Time.deltaTime;

                if (captureState >= 1f)
                {
                    captureState = 0f;
                    territory.svTerritory.SvSetTerritory(territory.attackerIndex);
                    foreach (var p in players) p.svPlayer.SvProgressBar(0f, 0f);
                }
                else if (captureState <= 0f)
                {
                    captureState = 0f;
                    territory.svTerritory.SvSetTerritory(territory.ownerIndex);
                    foreach (var p in players) p.svPlayer.SvProgressBar(0f, 0f);
                }
                else if (territory.attackerIndex >= 0)
                {
                    territory.svTerritory.SvSetTerritory(territory.ownerIndex, InvertOwnerIndex);
                }
            }

            if (newSpeed != lastSpeed)
            {
                lastSpeed = newSpeed;

                foreach (var p in players)
                {
                    p.svPlayer.SvProgressBar(captureState, newSpeed);
                }
            }
        }

        public byte InvertOwnerIndex => territory.ownerIndex == 0 ? (byte)1 : (byte)0;
    }

    public class WarManager
    {
        public Dictionary<ShTerritory, TerritoryState> territoryStates = new Dictionary<ShTerritory, TerritoryState>();

        public List<ShPlayer>[] skinPrefabs = new List<ShPlayer>[2];

        [Target(GameSourceEvent.ManagerStart, ExecutionMode.Event)]
        public void OnStart(SvManager svManager)
        {
            var skins = new HashSet<string>();

            for (int i = 0; i <= 1; i++)
            {
                svManager.ParseFile(ref skins, Paths.AbsolutePath($"skins{i}.txt"));
                skinPrefabs[i] = skins.ToEntityList<ShPlayer>();
            }

            for (int i = 0; i < 20; i++)
            {
                Utility.GetSpawn(out var position, out var rotation, out var place);
                svManager.AddNewEntity(skinPrefabs[i % 2].GetRandom(), place, position, rotation, true);
            }

            foreach(var t in Manager.territories)
            {
                territoryStates.Add(t, new TerritoryState(t));
            }
        }

        [Target(GameSourceEvent.ManagerUpdate, ExecutionMode.Override)]
        public void OnUpdate(SvManager svManager)
        {
            foreach(var t in territoryStates.Values)
            {
                t.Update();
            }

            foreach(var p in EntityCollections.Players)
            {
                if(p.isActiveAndEnabled && !p.IsDead)
                {
                    var territory = Manager.GetTerritory(p);

                    if(territory && territoryStates.TryGetValue(territory, out var state) && state.players.TryAdd(p))
                    {
                        p.svPlayer.SvProgressBar(state.captureState, state.lastSpeed);
                    }
                }
            }
        }

        //[Target(GameSourceEvent.ManagerFixedUpdate, ExecutionMode.Override)]
        //public void OnFixedUpdate(SvManager svManager) { }

        //[Target(GameSourceEvent.ManagerConsoleInput, ExecutionMode.Override)]
        //public void OnConsoleInput(SvManager svManager, string cmd) { }

        [Target(GameSourceEvent.ManagerTryLogin, ExecutionMode.Override)]
        public void OnTryLogin(SvManager svManager, ConnectionData connectData)
        {
            // Logins disabled here
            svManager.Disconnect(connectData.connection, DisconnectTypes.ClientIssue);
        }


        [Target(GameSourceEvent.ManagerTryRegister, ExecutionMode.Override)]
        public void OnTryRegister(SvManager svManager, ConnectionData connectData)
        {
            if (ValidateUser(svManager, connectData))
            {
                if (svManager.TryGetUserData(connectData.username, out var playerData))
                {
                    if (playerData.PasswordHash != connectData.passwordHash)
                    {
                        svManager.RegisterFail(connectData.connection, "Invalid credentials");
                        return;
                    }
                }

                if (!connectData.username.ValidCredential())
                {
                    svManager.RegisterFail(connectData.connection, $"Name cannot be registered (min: {Util.minCredential}, max: {Util.maxCredential})");
                    return;
                }

                if (connectData.customData.TryFetchCustomData(teamIndexKey, out int teamIndex) && connectData.skinIndex >= 0 && connectData.skinIndex < skinPrefabs[teamIndex].Count && connectData.wearableIndices?.Length == svManager.manager.nullWearable.Length)
                {
                    if (Utility.GetSpawn(out var position, out var rotation, out var place))
                    {
                        svManager.AddNewPlayer(skinPrefabs[teamIndex][connectData.skinIndex], connectData, playerData?.Persistent, position, rotation, place.mTransform);
                    }
                    else
                    {
                        svManager.RegisterFail(connectData.connection, "No spawn territories");
                    }
                }
                else
                {
                    svManager.RegisterFail(connectData.connection, "Invalid data");
                }
            }
        }

        public readonly List<string> teams = new List<string> { "SpecOps", "OpFor" };

        public readonly List<string> classes = new List<string> { "Rifleman", "Officer", "Sniper", "Support", "Medic", "Anti-Tank" };

        public const string selectTeam = "Select Team";
        public const string selectClass = "Select Class";

        [Target(GameSourceEvent.ManagerPlayerLoaded, ExecutionMode.Override)]
        public void OnPlayerLoaded(SvManager svManager, ConnectionData connectData)
        {
            connectData.connectionStatus = ConnectionStatus.LoadedMap;

            var options = new List<LabelID>();
            foreach (var c in teams) options.Add(new LabelID(c, c));
            var actions = new LabelID[] { new LabelID(selectTeam, selectTeam)};

            svManager.SendOptionMenu(connectData.connection, selectTeam, 0, selectTeam, options.ToArray(), actions);
        }

        private bool ValidateUser(SvManager svManager, ConnectionData connectData)
        {
            if (!svManager.HandleWhitelist(connectData.username))
            {
                svManager.RegisterFail(connectData.connection, "Account not whitelisted");
                return false;
            }

            // Don't allow multi-boxing, WebAPI doesn't prevent this
            if (EntityCollections.Accounts.ContainsKey(connectData.username))
            {
                svManager.RegisterFail(connectData.connection, "Account still logged in");
                return false;
            }

            return true;
        }

        private const string teamIndexKey = "teamIndex";
        private const string classIndexKey = "classIndex";

        // Read packet data from Buffers.reader
        [Target(GameSourceEvent.ManagerCustomPacket, ExecutionMode.Override)]
        public void OnCustomPacket(SvManager svManager, ConnectionData connectData, SvPacket packet)
        {
            switch(packet)
            {
                case SvPacket.OptionAction:
                    Util.ParseOptionAction(out var targetID, out var menuID, out var optionID, out var actionID);

                    switch (menuID)
                    {
                        case selectTeam:
                            {
                                var teamIndex = teams.IndexOf(optionID);
                                if (teamIndex >= 0)
                                {
                                    connectData.customData.AddOrUpdate(teamIndexKey, teamIndex);
                                    var options = new List<LabelID>();
                                    foreach (var c in classes) options.Add(new LabelID(c, c));
                                    var actions = new LabelID[] { new LabelID(selectTeam, selectTeam) };
                                    svManager.SendOptionMenu(connectData.connection, selectClass, 0, selectClass, options.ToArray(), actions);
                                }
                            }
                            break;

                        case selectClass:
                            {
                                var classIndex = classes.IndexOf(optionID);
                                if (classIndex >= 0 && connectData.customData.TryFetchCustomData(teamIndexKey, out int teamIndex))
                                {
                                    connectData.customData.AddOrUpdate(classIndexKey, classIndex);

                                    svManager.SendRegisterMenu(connectData.connection, false, skinPrefabs[teamIndex]);
                                }
                            }
                            break;
                    }
                    break;

                case SvPacket.MenuClosed:
                    Buffers.reader.ReadString(); // skip menuID
                    var manualClose = Buffers.reader.ReadBoolean();
                    if(manualClose) svManager.Disconnect(connectData.connection, DisconnectTypes.Normal);
                    break;
            }
        }
    }
}
