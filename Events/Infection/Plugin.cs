﻿using AutoEvent.Interfaces;
using Exiled.API.Features;
using MapEditorReborn.API.Features.Objects;
using MEC;
using PlayerRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AutoEvent.Events.Infection
{
    public class Plugin : Event
    {
        public override string Name { get; set; } = AutoEvent.Singleton.Translation.ZombieName;
        public override string Description { get; set; } = AutoEvent.Singleton.Translation.ZombieDescription;
        public override string Color { get; set; } = "FF4242";
        public override string CommandName { get; set; } = "zombie";
        public static SchematicObject GameMap { get; set; }
        public static TimeSpan EventTime { get; set; }

        EventHandler _eventHandler;

        public override void OnStart()
        {
            _eventHandler = new EventHandler();
            Exiled.Events.Handlers.Player.Verified += _eventHandler.OnJoin;
            Exiled.Events.Handlers.Player.Died += _eventHandler.OnDead;
            Exiled.Events.Handlers.Player.Hurting += _eventHandler.OnDamage;
            Exiled.Events.Handlers.Server.RespawningTeam += _eventHandler.OnTeamRespawn;
            OnEventStarted();
        }
        public override void OnStop()
        {
            Exiled.Events.Handlers.Player.Verified -= _eventHandler.OnJoin;
            Exiled.Events.Handlers.Player.Died -= _eventHandler.OnDead;
            Exiled.Events.Handlers.Player.Hurting -= _eventHandler.OnDamage;
            Exiled.Events.Handlers.Server.RespawningTeam -= _eventHandler.OnTeamRespawn;
            Timing.CallDelayed(10f, () => EventEnd());
            AutoEvent.ActiveEvent = null;
            _eventHandler = null;
        }
        public void OnEventStarted()
        {
            EventTime = new TimeSpan(0, 0, 0);
            GameMap = Extensions.LoadMap("Zombie", new Vector3(115.5f, 1030f, -43.5f), new Quaternion(0, 0, 0, 0), new Vector3(1, 1, 1));
            switch(Random.Range(0, 2))
            {
                case 0: Extensions.PlayAudio("Zombie.ogg", 15, true, Name); break;
                case 1: Extensions.PlayAudio("Zombie2.ogg", 15, true, Name); break;
            }
            foreach (Player player in Player.List)
            {
                player.Role.Set(RoleTypeId.ClassD, Exiled.API.Enums.SpawnReason.None, RoleSpawnFlags.None);
                player.Position = GameMap.transform.position + new Vector3(-18.75f, 2.5f, 0f);
                player.ClearInventory();
            }
            Timing.RunCoroutine(OnEventRunning(), "zombie_run");
        }
        public IEnumerator<float> OnEventRunning()
        {
            var trans = AutoEvent.Singleton.Translation;
            // Counting down
            for (float time = 15; time > 0; time--)
            {
                Extensions.Broadcast(trans.ZombieBeforeStart.Replace("{name}", Name).Replace("{time}", time.ToString()), 1);
                yield return Timing.WaitForSeconds(1f);
            }
            // Spawn zombie
            Player.List.ToList().RandomItem().Role.Set(RoleTypeId.Scp0492);
            // Until there is one player left, the game will not end
            while (Player.List.Count(r => r.Role == RoleTypeId.ClassD) > 1)
            {
                var count = Player.List.Count(r => r.Role == RoleTypeId.ClassD);
                var time = $"{EventTime.Minutes}:{EventTime.Seconds}";
                Extensions.Broadcast(trans.ZombieCycle.Replace("{name}", Name).Replace("{count}", count.ToString()).Replace("{time}", time), 1);
                yield return Timing.WaitForSeconds(1f);
                EventTime += TimeSpan.FromSeconds(1f);
            }
            Timing.RunCoroutine(DopTime(), "EventBeginning");
            yield break;
        }
        public IEnumerator<float> DopTime()
        {
            var trans = AutoEvent.Singleton.Translation;
            var time = $"{EventTime.Minutes}:{EventTime.Seconds}";
            // If there is only one person left, then the countdown will start
            for (int extratime = 30; extratime > 0; extratime--)
            {
                if (Player.List.Count(r => r.Role == RoleTypeId.ClassD) == 0) break;
                Extensions.Broadcast(trans.ZombieExtraTime.Replace("{extratime}", extratime.ToString()).Replace("{time}", time), 1);
                yield return Timing.WaitForSeconds(1f);
                EventTime += TimeSpan.FromSeconds(1f);
            }
            if (Player.List.Count(r => r.Role == RoleTypeId.ClassD) == 0)
            {
                Extensions.Broadcast(trans.ZombieWin.Replace("{time}", time), 10);
            }
            else
            {
                Extensions.Broadcast(trans.ZombieLose.Replace("{time}", time), 10);
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
