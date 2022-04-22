﻿using BrokeProtocol.API;
using BrokeProtocol.Entities;
using BrokeProtocol.GameSource.Jobs;
using BrokeProtocol.Managers;
using BrokeProtocol.Utility;
using BrokeProtocol.Utility.Jobs;
using Newtonsoft.Json.Linq;
using System.Collections;
using UnityEngine;

namespace BrokeProtocol.CustomEvents
{
    public class CustomEvents : IScript
    {
        // Can be called either via an in-game Trigger map object or CEF/JavaScript events
        [CustomTarget]
        public void AreaWarning(ShEntity trigger, ShPhysical physical)
        {
            if (physical is ShPlayer player && player.svPlayer.job.info.shared.groupIndex != GroupIndex.LawEnforcement)
            {
                player.svPlayer.SendGameMessage($"Warning! You are about to enter {trigger.svEntity.data}!");

                /* Execute client C# example */
                //player.svPlayer.ExecuteCS("clManager.SendToServer(Channel.Unsequenced, SvPacket.GlobalMessage, \"ExecuteCS Test\");");

                /* Execute client C# via JavaScript example */
                /* Note inner quote is escaped twice due to being unwrapped across 2 languages */
                //player.svPlayer.ExecuteJS("exec(\"clManager.SendToServer(Channel.Unsequenced, SvPacket.GlobalMessage, \\\"ExecuteJS Test\\\");\");");
            }
        }

        // Example call from CEF used in www/cef/index2.html -> "window.trigger("YourEventName", {argument : YourArgs});"
        [CustomTarget]
        public void OnPressedKey(ShPlayer caller, JToken args)
        {
            caller.svPlayer.SendGameMessage((string)args["argument"]);
        }

        bool voidRunning;

        [CustomTarget]
        public void ButtonPush(ShEntity target, ShPlayer caller)
        {
            if (voidRunning)
            {
                caller.svPlayer.SendGameMessage("Do not challenge the void");
                return;
            }

            const int cost = 500;

            if (caller.MyMoneyCount < cost)
            {
                caller.svPlayer.SendGameMessage("Not enough cash");
                return;
            }

            caller.TransferMoney(DeltaInv.RemoveFromMe, cost);

            target.StartCoroutine(EnterTheVoid(target.svEntity.svManager));
        }

        private IEnumerator EnterTheVoid(SvManager svManager)
        {
            voidRunning = true;

            var delay = new WaitForSecondsRealtime(0.1f);

            var duration = 4f;
            var startTime = Time.time;

            var originalTimeScale = Time.timeScale;
            var targetTimeScale = 0.25f;

            var defaultEnvironment = SceneManager.Instance.defaultEnvironment;

            var originalSky = SceneManager.Instance.skyColor;
            var originalCloud = SceneManager.Instance.cloudColor;
            var originalWater = SceneManager.Instance.waterColor;

            var targetSky = Color.red;
            var targetCloud = Color.black;
            var targetWater = Color.cyan;

            var normalizedClip = 0.25f;

            while (Time.time < startTime + duration)
            {
                yield return delay;

                var normalizedTime = (Time.time - startTime) / duration;

                float lerp;

                if (normalizedTime < normalizedClip)
                {
                    lerp = normalizedTime / normalizedClip;
                }
                else if (normalizedTime >= 1f - normalizedClip)
                {
                    lerp = (1f - normalizedTime) / normalizedClip;
                }
                else
                {
                    lerp = 1f;
                }

                svManager.SvSetTimeScale(Mathf.Lerp(originalTimeScale, targetTimeScale, lerp));
                svManager.SvSetSkyColor(Color.LerpUnclamped(originalSky, targetSky, lerp));
                svManager.SvSetCloudColor(Color.LerpUnclamped(originalCloud, targetCloud, lerp));
                svManager.SvSetWaterColor(Color.LerpUnclamped(originalWater, targetWater, lerp));
            }

            svManager.SvSetTimeScale(originalTimeScale);

            if (defaultEnvironment)
            {
                svManager.SvSetDefaultEnvironment();
            }
            else
            {
                svManager.SvSetSkyColor(originalSky);
                svManager.SvSetCloudColor(originalCloud);
                svManager.SvSetWaterColor(originalWater);
            }

            voidRunning = false;
        }

        [CustomTarget]
        public void PlaceBounty(ShEntity target, ShPlayer player)
        {
            if (target is ShPlayer targetPlayer)
                if (targetPlayer.svPlayer.job is Hitman hitmanJob)
                    hitmanJob.PlaceBountyAction(player);
        }

        [CustomTarget]
        public void BountyList(ShEntity target, ShPlayer player)
        {
            if(target is ShPlayer targetPlayer)
                if(targetPlayer.svPlayer.job is Hitman hitmanJob)
                    hitmanJob.BountyListAction(player);
        }

        [CustomTarget]
        public void BountyListSelf(ShPlayer player)
        {
            if(player.svPlayer.job is Hitman hitmanJob)
                hitmanJob.BountyListAction(player);
        }

        [CustomTarget]
        public void ResetTarget(ShPlayer player)
        {
            if (player.svPlayer.job is TargetEntityJob targetEntityJob)
                targetEntityJob.ResetTarget();
        }

        [CustomTarget]
        public void RequestItem(ShEntity target, ShPlayer player) => ((target as ShPlayer)?.svPlayer.job as Mayor)?.RequestItemAction(player);

        [CustomTarget]
        public void RequestList(ShPlayer player) => (player.svPlayer.job as Mayor)?.RequestListAction();

        [CustomTarget]
        public void DeliverItem(ShEntity target, ShPlayer player) => (player.svPlayer.job as DeliveryMan)?.DeliverItemAction(target);

        [CustomTarget]
        public void RequestHeal(ShEntity target, ShPlayer player) => ((target as ShPlayer)?.svPlayer.job as Paramedic)?.RequestHeal(player);

        [CustomTarget]
        public void StealItem(ShPlayer player, ShItem item)
        {
            //TransferItem already has sanity checks -> if(player.otherEntity && player.otherEntity.HasItem(item))
            player.TransferItem(DeltaInv.StealFromOther, item.index);
        }
    }
}