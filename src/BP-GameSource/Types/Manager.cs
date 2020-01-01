﻿using BrokeProtocol.Server.LiteDB.Models;
using BrokeProtocol.Utility;
using BrokeProtocol.Managers;
using BrokeProtocol.Entities;
using BrokeProtocol.Collections;


namespace BrokeProtocol.GameSource.Types
{
    public class Manager
    {
        [Target(typeof(API.Events.Manager), (int)API.Events.Manager.OnStarted)]
        protected void OnStarted(SvManager svManager)
        {
        }

        private bool ValidateUser(SvManager svManager, AuthData authData)
        {
            if (!svManager.HandleWhitelist(authData.accountID))
            {
                svManager.RegisterFail(authData.connection, "Account not whitelisted");
                return false;
            }

            // Don't allow multi-boxing, WebAPI doesn't prevent this
            foreach (ShPlayer p in EntityCollections.Humans)
            {
                if (p.accountID == authData.accountID)
                {
                    svManager.RegisterFail(authData.connection, "Account still logged in");
                    return false;
                }
            }

            return true;
        }

        [Target(typeof(API.Events.Manager), (int)API.Events.Manager.OnTryLogin)]
        protected void OnTryLogin(SvManager svManager, AuthData authData, ConnectData connectData)
        {
            if (ValidateUser(svManager, authData))
            {
                if (!svManager.TryGetUserData(authData.accountID, out User playerData))
                {
                    svManager.RegisterFail(authData.connection, "Account not found - Please Register");
                    return;
                }

                if (playerData.BanInfo.IsBanned)
                {
                    svManager.RegisterFail(authData.connection, $"Account banned: {playerData.BanInfo.Reason}");
                    return;
                }

                if (!svManager.settings.auth.steam && playerData.PasswordHash != connectData.passwordHash)
                {
                    svManager.RegisterFail(authData.connection, $"Invalid credentials");
                    return;
                }

                svManager.LoadSavedPlayer(playerData, authData, connectData);
            }
        }

        [Target(typeof(API.Events.Manager), (int)API.Events.Manager.OnTryRegister)]
        protected void OnTryRegister(SvManager svManager, AuthData authData, ConnectData connectData)
        {
            if (ValidateUser(svManager, authData))
            {
                if (svManager.TryGetUserData(authData.accountID, out User playerData))
                {
                    if (playerData.BanInfo.IsBanned)
                    {
                        svManager.RegisterFail(authData.connection, $"Account banned: {playerData.BanInfo.Reason}");
                        return;
                    }

                    if (!svManager.settings.auth.steam && playerData.PasswordHash != connectData.passwordHash)
                    {
                        svManager.RegisterFail(authData.connection, $"Invalid credentials");
                        return;
                    }
                }

                if (!connectData.username.ValidCredential())
                {
                    svManager.RegisterFail(authData.connection, $"Name cannot be registered (min: {Util.minCredential}, max: {Util.maxCredential})");
                    return;
                }

                svManager.AddNewPlayer(authData, connectData);
            }
        }
    }
}
