﻿using AutoEvent.Interfaces;
using Exiled.API.Enums;
using Exiled.API.Features;
using MapEditorReborn.API.Features.Objects;
using MEC;
using PlayerRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AutoEvent.Events.Knifes
{
    public class Plugin : Event
    {
        public override string Name { get; set; } = AutoEvent.Singleton.Translation.KnivesName;
        public override string Description { get; set; } = AutoEvent.Singleton.Translation.KnivesDescription;
        public override string Color { get; set; } = "FFFF00";
        public override string CommandName { get; set; } = "knife";
        public SchematicObject GameMap { get; set; }
        public TimeSpan EventTime { get; set; }

        EventHandler _eventHandler;

        public override void OnStart()
        {
            _eventHandler = new EventHandler();

            Exiled.Events.Handlers.Player.Verified += _eventHandler.OnJoin;
            Exiled.Events.Handlers.Player.DroppingItem += _eventHandler.OnDropItem;
            Exiled.Events.Handlers.Server.RespawningTeam += _eventHandler.OnTeamRespawn;
            Exiled.Events.Handlers.Item.ChargingJailbird += _eventHandler.OnChargeJailbird;
            OnEventStarted();
        }
        public override void OnStop()
        {
            Exiled.Events.Handlers.Player.Verified -= _eventHandler.OnJoin;
            Exiled.Events.Handlers.Player.DroppingItem -= _eventHandler.OnDropItem;
            Exiled.Events.Handlers.Server.RespawningTeam -= _eventHandler.OnTeamRespawn;
            Exiled.Events.Handlers.Item.ChargingJailbird -= _eventHandler.OnChargeJailbird;

            Timing.CallDelayed(10f, () => EventEnd());
            AutoEvent.ActiveEvent = null;
            _eventHandler = null;
        }
        public void OnEventStarted()
        {
            EventTime = new TimeSpan(0, 0, 0);
            GameMap = Extensions.LoadMap("35hp_2", new Vector3(110f, 1030f, -43.5f), new Quaternion(0, 0, 0, 0), new Vector3(1, 1, 1));
            Extensions.PlayAudio("Knife.ogg", 10, true, Name);
            var count = 0;
            foreach (Player player in Player.List)
            {
                if (count % 2 == 0)
                {
                    player.Role.Set(RoleTypeId.NtfCaptain);
                    player.Position = GameMap.Position + new Vector3(Random.Range(20, 30), 7, Random.Range(-16, 16));
                }
                else
                {
                    player.Role.Set(RoleTypeId.ChaosRepressor);
                    player.Position = GameMap.Position + new Vector3(Random.Range(-32, -20), 7, Random.Range(-16, 16));
                }
                player.ResetInventory(new List<ItemType> { ItemType.Jailbird });
                player.EnableEffect(EffectType.Ensnared, 10);
                count++;
            }
            Timing.RunCoroutine(OnEventRunning(), "knives_run");
        }
        public IEnumerator<float> OnEventRunning()
        {
            var trans = AutoEvent.Singleton.Translation;
            for (int time = 10; time > 0; time--)
            {
                Extensions.Broadcast($"<size=100><color=red>{time}</color></size>", 1);
                yield return Timing.WaitForSeconds(1f);
            }
            while (Player.List.Count(r => r.Role.Team == Team.FoundationForces) > 0 && Player.List.Count(r => r.Role.Team == Team.ChaosInsurgency) > 0)
            {
                string mtfCount = Player.List.Count(r => r.Role.Team == Team.FoundationForces).ToString();
                string chaosCount = Player.List.Count(r => r.Role.Team == Team.ChaosInsurgency).ToString();
                Extensions.Broadcast(trans.KnivesCycle.Replace("{name}", Name).Replace("{mtfcount}", mtfCount).Replace("{chaoscount}", chaosCount), 1);

                yield return Timing.WaitForSeconds(1f);
            }
            if (Player.List.Count(r => r.Role.Team == Team.FoundationForces) == 0)
            {
                Extensions.Broadcast(trans.KnivesChaosWin.Replace("{name}", Name), 10);
            }
            else if (Player.List.Count(r => r.Role.Team == Team.ChaosInsurgency) == 0)
            {
                Extensions.Broadcast(trans.KnivesMtfWin.Replace("{name}", Name), 10);
            }
            OnStop();
            yield break;
        }
        public void EventEnd()
        {
            Extensions.CleanUpAll();
            Extensions.TeleportEnd();
            Extensions.UnLoadMap(GameMap);
            Extensions.StopAudio();
        }
    }
}
