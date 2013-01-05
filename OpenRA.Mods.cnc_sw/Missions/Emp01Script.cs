using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Mods.Cnc;
using OpenRA.Mods.RA;
using OpenRA.Network;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.cnc_sw.Missions
{
    class Emp01ScriptInfo : TraitInfo<Emp01Script>, Requires<SpawnMapActorsInfo> { }

        class Emp01Script : IWorldLoaded, ITick
        {
            static readonly string[] Objectives =
		{
			"Destroy the Rebel Alliance"
		};

            int currentObjective;
            //int GDIReinforceSthSpawn;

            Player gdi;
            Player nod;

            //actors and the likes go here
            //e.g. Actor nikoomba;
            Actor temple;

            World world;

            //in the allies01 script stuff was here not needed for me so far
            //const string NR1Name = "E3";
            //const int GDIReinfSthRange = 10;

            void DisplayObjective()
            {
                Game.AddChatLine(Color.LimeGreen, "Objective", Objectives[currentObjective]);
                Sound.Play("bleep2.aud");
            }

            void MissionFailed(string text)
            {
                if (gdi.WinState != WinState.Undefined)
                {
                    return;
                }
                gdi.WinState = WinState.Lost;
                foreach (var actor in world.Actors.Where(a => a.IsInWorld && a.Owner == nod && !a.IsDead()))
                {
                    actor.Kill(actor);
                }
                Game.AddChatLine(Color.Red, "Mission failed", text);
                //Sound.Play("misnlst1.aud");
                Game.Debug("again more sounds not implemented");
            }

            void MissionAccomplished(string text)
            {
                if (gdi.WinState != WinState.Undefined)
                {
                    return;
                }
                gdi.WinState = WinState.Won;
                Game.AddChatLine(Color.Green, "Mission accomplished", text);
                //Sound.Play("misnwon1.aud");
            }

            public void Tick(Actor self)
            {
                if (gdi.WinState != WinState.Undefined)
                {
                    return;
                }
                // display current objective every so often
                if (world.FrameNumber % 1500 == 1)
                {
                    DisplayObjective();
                }
                //spawns nod reinf
                //if (world.FrameNumber == 150)
                //{
                //    NODReinforceNth();
                //}
                // objectives
                if (currentObjective == 0)
                {
                    if (temple.Destroyed)
                    {
                        MissionAccomplished("The Rebel Alliance was obliterated!");
                        //currentObjective++;
                        //DisplayObjective();
                        //GDIReinforceNth();
                    }
                }
            }

            IEnumerable<Actor> UnitsNearActor(Actor actor, int range)
            {
                return world.FindUnitsInCircle(actor.CenterLocation, Game.CellSize * range)
                    .Where(a => a.IsInWorld && a != world.WorldActor && !a.Destroyed && a.HasTrait<IMove>() && !a.Owner.NonCombatant);
            }

            void NODReinforceNth()
            {
                Game.Debug("Your reinforcements should have spawned");
                //nr1 = world.CreateActor(false, NR1Name, new TypeDictionary { new OwnerInit(nod), new LocationInit(nr1.Location) });
                //nr1.QueueActivity(new Move.Move(nr1.Location - new CVec(0, 2)));
            }

            void GDIReinforceNth()
            {
                Game.Debug("gdi are sending stuff from the north");
                //nr1 = world.CreateActor(false, NR1Name, new TypeDictionary { new OwnerInit(nod), new LocationInit(nr1.Location) });
                //nr1.QueueActivity(new Move.Move(nr1.Location - new CVec(0, 2)));
            }

            void GDIReinforceSth()
            {
                Game.Debug("gdi are sending stuff from the south");
                //nr1 = world.CreateActor(false, NR1Name, new TypeDictionary { new OwnerInit(nod), new LocationInit(nr1.Location) });
                //nr1.QueueActivity(new Move.Move(nr1.Location - new CVec(0, 2)));
            }

            public void WorldLoaded(World w)
            {
                world = w;
                gdi = w.Players.Single(p => p.InternalName == "GDI");
                nod = w.Players.Single(p => p.InternalName == "NOD");
                var actors = w.WorldActor.Trait<SpawnMapActors>().Actors;
                temple = actors["Temple"];
                //Game.MoveViewport(nr1.Location.ToFloat2());
                //Game.ConnectionStateChanged += StopMusic;
                //causes an exception atm
                //Media.PlayFMVFullscreen(w, "nod1.vqa", () =>
                //{
                //    Media.PlayFMVFullscreen(w, "landing.vqa", () =>
                //    {
                //        PlayMusic();
                //    });
                //});
            }

            void PlayMusic()
            {
                if (!Rules.InstalledMusic.Any())
                {
                    return;
                }
                //somehow get this to play aoi, did it in the map.yaml
                var track = Rules.InstalledMusic.Random(Game.CosmeticRandom);
                Sound.PlayMusicThen(track.Value, PlayMusic);
            }

            void StopMusic(OrderManager orderManager)
            {
                if (!orderManager.GameStarted)
                {
                    Sound.StopMusic();
                    Game.ConnectionStateChanged -= StopMusic;
                }
            }
        }
    }
