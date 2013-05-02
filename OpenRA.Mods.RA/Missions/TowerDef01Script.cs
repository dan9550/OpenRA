#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using System;
using OpenRA.FileFormats;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Move;
using OpenRA.Traits;
using OpenRA.Widgets;
using OpenRA.Mods.RA.Buildings;

namespace OpenRA.Mods.RA.Missions
{
    class TowerDef01ScriptInfo : TraitInfo<TowerDef01Script>, Requires<SpawnMapActorsInfo> { }

    class TowerDef01Script : IWorldLoaded, ITick
    {
        Player allies; 
        Player soviets;

        Actor WPE;
        Actor WP1;
        Actor WP2;
        Actor WP3;
        Actor WP4;
        Actor WP5;
        Actor WP6;
        Actor WP7;
        Actor WP8;
        Actor WP9;

        Actor WPF;

        //CPos[] TDPath;

        World world;

        int wave = 1;
        static int current;

        static readonly string[] waveindex = { "wave1", "wave2", "wave3", "wave4", "wave5", "wave6", "wave7", "wave8", "wave9", "wave10", "wave11", "wave12" };

        static readonly string[] wave1 = { "e1", "e1", "e1", "e1", "e1"};
        static readonly string[] wave2 = { "e1", "e1", "e1", "e1", "e1", "e1", "e1", "e1", "e1", "e1" };
        static readonly string[] wave3 = { "e2", "e2", "e2", "e2", "e2" };
        static readonly string[] wave4 = { "e1", "e1", "e1", "e1", "e1", "e1", "e1", "e1", "e1", "e1", "e1", "e1", "e1", "e1", "e1" };
        static readonly string[] wave5 = { "e3", "e3", "e3", "e3", "e3" };
        static readonly string[] wave6 = { "e2", "e2", "e2", "e2", "e2", "e2", "e2", "e2", "e2", "e2" };
        static readonly string[] wave7 = { "e3", "e3", "e3", "e3", "e3", "e3", "e3", "e3", "e3", "e3", "e3", "e3" };
        static readonly string[] wave8 = { "e4", "e4", "e4", "e4", "e4", "e4", "e4", "e4" };
        static readonly string[] wave9 = { "shok", "shok", "shok", "shok", "shok" };
        static readonly string[] wave10 = { "e7", "e7", "e7", "e7", "e7"};
        static readonly string[] wave11 = { "shok", "shok", "shok", "shok", "shok", "shok", "shok", "shok", "shok", "shok" };
        static readonly string[] wave12 = { "e8", "e8", "e8", "e8", "e8" };

        void MissionAccomplished(string text)
        {
            MissionUtils.CoopMissionAccomplished(world, text, allies);
        }

        void MissionFailed(string text)
        {
            MissionUtils.CoopMissionFailed(world, text, allies);
        }

        public void Tick(Actor self)
        {
            if (allies.WinState != WinState.Undefined)
                return;

            //like the voids i assume these can be condensed somehow
            if (world.FrameNumber % 150 == 1) //changes from 1500 to 150 for testing purposes.
            {
                SendWave();
            }

            /*if (world.FrameNumber == 2000)
            {
                SendWave2();
            }

            if (world.FrameNumber == 3000)
            {
                SendWave3();
            }

            if (world.FrameNumber == 4000)
            {
                SendWave4();
            }

            if (world.FrameNumber == 5000)
            {
                SendWave5();
            }

            if (world.FrameNumber == 6500)
            {
                SendWave6();
            }*/
        }

        void SendWave()
        {
            //can't figure out how to compress everything and make the selected waves auto increment in the same thingy
            //current = "wave" + wave;
            //string index = waveindex[wave];
            string[] blehblah = null; //all dodgy testing things should clean up when works
            switch (wave)
            {
                case 1:
                    blehblah = wave1;
                    break;
                case 2:
                    blehblah = wave2;
                    break;
                case 3:
                    blehblah = wave3;
                    break;
                case 4:
                    blehblah = wave4;
                    break;
                case 5:
                    blehblah = wave5;
                    break;
                case 6:
                    blehblah = wave6;
                    break;
                case 7:
                    blehblah = wave7;
                    break;
                case 8:
                    blehblah = wave8;
                    break;
                case 9:
                    blehblah = wave9;
                    break;
                case 10:
                    blehblah = wave10;
                    break;
                case 11:
                    blehblah = wave11;
                    break;
                case 12:
                    blehblah = wave12;
                    break;
            }
            for (int i = 0; i < waveindex[wave].Length; i++)
            {
                //string[] blehblah = waveindex;
                var actor = world.CreateActor(blehblah[i], new TypeDictionary { new OwnerInit(soviets), new LocationInit(WPE.Location) });
                //for now it just sends them to the last waypoint, not sure have to make them follow a path of waypoints
                actor.QueueActivity(new Move.Move(WPF.Location));
                
            }
            wave++;
            Game.Debug("Wave 1 Complete!");
        }

        //All these voids below couold be combined into one with some kind of counter maybe.
        /*void SendWave1()
        {
            for (int i = 0; i < wave1.Length; i++)
            {
                var actor = world.CreateActor(wave1[i], new TypeDictionary { new OwnerInit(soviets), new LocationInit(WPE.Location) });
                //for now it just sends them to the last waypoint, not sure have to make them follow a path of waypoints
                actor.QueueActivity(new Move.Move(WPF.Location));
            }
            Game.Debug("Wave 1 Complete!");
        }

        void SendWave2()
        {
            for (int i = 0; i < wave2.Length; i++)
            {
                var actor = world.CreateActor(wave2[i], new TypeDictionary { new OwnerInit(soviets), new LocationInit(WPE.Location) });
                actor.QueueActivity(new Move.Move(WPF.Location));
            }
            Game.Debug("Wave 2 Complete!");
        }

        void SendWave3()
        {
            for (int i = 0; i < wave3.Length; i++)
            {
                var actor = world.CreateActor(wave3[i], new TypeDictionary { new OwnerInit(soviets), new LocationInit(WPE.Location) });
                actor.QueueActivity(new Move.Move(WPF.Location));
            }
            Game.Debug("Wave 2 Complete!");
        }

        void SendWave4()
        {
            for (int i = 0; i < wave4.Length; i++)
            {
                var actor = world.CreateActor(wave4[i], new TypeDictionary { new OwnerInit(soviets), new LocationInit(WPE.Location) });
                actor.QueueActivity(new Move.Move(WPF.Location));
            }
            Game.Debug("Wave 2 Complete!");
        }

        void SendWave5()
        {
            for (int i = 0; i < wave5.Length; i++)
            {
                var actor = world.CreateActor(wave5[i], new TypeDictionary { new OwnerInit(soviets), new LocationInit(WPE.Location) });
                actor.QueueActivity(new Move.Move(WPF.Location));
            }
            Game.Debug("Wave 2 Complete!");
        }

        void SendWave6()
        {
            for (int i = 0; i < wave6.Length; i++)
            {
                var actor = world.CreateActor(wave6[i], new TypeDictionary { new OwnerInit(soviets), new LocationInit(WPE.Location) });
                actor.QueueActivity(new Move.Move(WPF.Location));
            }
            Game.Debug("Wave 2 Complete!");
        }*/

		public void WorldLoaded(World w)
		{
			world = w;

            allies = w.Players.Single(p => p.InternalName == "Allies");
            soviets = w.Players.Single(p => p.InternalName == "Soviets");

			var actors = w.WorldActor.Trait<SpawnMapActors>().Actors;

            WPE = actors["WPE"];
			WP1 = actors["WP1"];
			WP2 = actors["WP2"];
			WP3 = actors["WP3"];
			WP4 = actors["WP4"];
			WP5 = actors["WP5"];
            WP6 = actors["WP6"];
            WP7 = actors["WP7"];
            WP8 = actors["WP8"];
            WP9 = actors["WP9"];
            WPF = actors["WPF"];

            //TDPath = new[] { WP1, WP2, WP3, WP4, WP5, WP6, WP7, WP8, WP9 }.Select(p => p.Location).ToArray();

            allies.PlayerActor.Trait<PlayerResources>().Cash = 1000;

            Game.Debug("You have 1 minute before the first wave.");
		}
    }
}
