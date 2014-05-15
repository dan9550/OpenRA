#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Security.Cryptography;
using OpenRA.FileFormats;
using OpenRA.Widgets;

namespace OpenRA.Mods.TS.Widgets.Logic
{
	public class TSInstallFromCDLogic
	{
		Widget panel;
		ProgressBarWidget progressBar;
		LabelWidget statusLabel;
		Action continueLoading;
		ButtonWidget retryButton, backButton;
		Widget installingContainer, insertDiskContainer;


		string[] installPackages = new string[] { };
		string[] baseFiles = new string[] { };
		string[] scoresFiles = new string[] { };
		string[] moviesFiles = new string[] { };


		[ObjectCreator.UseCtor]
		public TSInstallFromCDLogic(Widget widget, Action continueLoading)
		{
			this.continueLoading = continueLoading;
			panel = widget.Get("INSTALL_FROMCD_PANEL");
			progressBar = panel.Get<ProgressBarWidget>("PROGRESS_BAR");
			statusLabel = panel.Get<LabelWidget>("STATUS_LABEL");

			backButton = panel.Get<ButtonWidget>("BACK_BUTTON");
			backButton.OnClick = Ui.CloseWindow;

			retryButton = panel.Get<ButtonWidget>("RETRY_BUTTON");
			retryButton.OnClick = CheckForDisk;

			installingContainer = panel.Get("INSTALLING");
			insertDiskContainer = panel.Get("INSERT_DISK");
			CheckForDisk();
		}

		void CheckForDisk()
		{
			string[] hashFiles;
			string[] knownHashes;

			//Basic quick check
			Func<string, bool> ValidDiskFilter = diskRoot => File.Exists(diskRoot+Path.DirectorySeparatorChar+"MULTI.MIX") &&
					File.Exists(new string[] { diskRoot, "INSTALL", "TIBSUN.MIX" }.Aggregate(Path.Combine));

			var path = InstallUtils.GetMountedDisk(ValidDiskFilter);
			var yaml = new MiniYaml(null, MiniYaml.FromFile("mods/ts/install.yaml")).NodesDict;

			hashFiles = YamlList(yaml, "HashFiles");
			knownHashes = YamlList(yaml, "KnownHashes");

			new Thread(() =>
			{
				if (path != null)
				{
					int contentID = 0;
					foreach (string checkFile in hashFiles)
					{
						try
						{
							var sha1File = File.OpenRead(path + Path.DirectorySeparatorChar + checkFile);
							using (var cryptoProvider = new SHA1CryptoServiceProvider())
							{
								string discHash = BitConverter.ToString(cryptoProvider.ComputeHash(sha1File));

								//This i guess should check against all the hashes, i think this works :S
								foreach (string checkHash in knownHashes)
								{
									if (checkHash == discHash)
									{
										sha1File.Close();
										Install(path, contentID);
									}
								}
							}
							sha1File.Close();
						}
						catch { }
						contentID++;
					}
				}
				else
				{
					insertDiskContainer.IsVisible = () => true;
					installingContainer.IsVisible = () => false;
				}
			}) { IsBackground = true}.Start();
		}

		static string[] YamlList(Dictionary<string, MiniYaml> yaml, string key)
		{
			if (!yaml.ContainsKey(key))
				return new string[] { };

			return yaml[key].NodesDict.Keys.ToArray();
		}

		void DataLoader(int content)
		{
			var yaml = new MiniYaml(null, MiniYaml.FromFile("mods/ts/install.yaml")).NodesDict;


			switch(content)
			{
				case 0: //GDI Disc
					installPackages = new string[] { "INSTALL/TIBSUN.MIX", "SCORES.MIX", "MOVIES01.MIX" };
					baseFiles = YamlList(yaml, "BaseFiles");
					scoresFiles = YamlList(yaml, "BaseScores");
					moviesFiles = YamlList(yaml, "GDIMovies");
					break;

				case 1: //Nod Disc
					installPackages = new string[] { "INSTALL/TIBSUN.MIX", "SCORES.MIX", "MOVIES02.MIX" };
					baseFiles = YamlList(yaml, "BaseFiles");
					scoresFiles = YamlList(yaml, "BaseScores");
					moviesFiles = YamlList(yaml, "NodMovies");
					break;

				case 2: //Firestorm Expansion
					installPackages = new string[] { "", "SCORES01.MIX", "MOVIES03.MIX" };
					baseFiles = new string[] {  };
					scoresFiles = YamlList(yaml, "ExpansionScores");
					moviesFiles = YamlList(yaml, "ExpansionMovies");
					break;
			}		
		}

		void Install(string source, int contentID)
		{
			backButton.IsDisabled = () => true;
			retryButton.IsDisabled = () => true;
			insertDiskContainer.IsVisible = () => false;
			installingContainer.IsVisible = () => true;

			var dest = new string[] { Platform.SupportDir, "Content", "ts" }.Aggregate(Path.Combine);
			Console.WriteLine(dest);

			//These things are hopefully not permanent since the files really should be installed into packages not folders !!!!
			//var baseDest = new string[] { dest, "ts-base", }.Aggregate(Path.Combine);
			//var scoresDest = new string[] { dest, "ts-scores" }.Aggregate(Path.Combine);
			//var moviesDest = new string[] { dest, "ts-movies", }.Aggregate(Path.Combine);

			DataLoader(contentID);

			var installCounter = 0;
			var installTotal = baseFiles.Count() + scoresFiles.Count() + moviesFiles.Count();
			var onProgress = (Action<string>)(s => Game.RunAfterTick(() =>
			{
				progressBar.Percentage = installCounter*100/installTotal;
				installCounter++;

				statusLabel.GetText = () => s;
			}));

			var onError = (Action<string>)(s => Game.RunAfterTick(() =>
			{
				statusLabel.GetText = () => "Error: "+s;
				backButton.IsDisabled = () => false;
				retryButton.IsDisabled = () => false;
			}));

			var t = new Thread( _ =>
			{
				try
				{
					//Merge the below code into something more efficent and things probs won;t matter when files are packaged

					Console.WriteLine(source);
					Console.WriteLine(installPackages[0]);

					//Base Content
					if (!InstallUtils.ExtractFromPackage(source, installPackages[0], baseFiles, dest, onProgress, onError, true))
						return;
					Console.WriteLine("TEST CODE, extracting files from cache.mix inside tibsun.mix");
					string[] test = new string[] { "wrench.shp", "temperat.pal", "pilotlit.shp", "anim.pal", "8point.fnt", "dropdown.shp", "isosno.pal", "loadout.shp", "palette.pal", "poweroff.shp", 
					"cameo.pal", "mousepal.pal", "unitsno.pal", "harvestr.shp", "12metfnt.fnt", "grad6fnt.fnt", "6point.fnt", "isotem.pal", "kia6pt.fnt", "unittem.pal", "snow.pal", "editfnt.fnt", 
					"waypoint.pal", "dropup.shp", "key.ini" };
					if (!InstallUtils.ExtractceptionFromPackage(source, installPackages[0], "cache.mix", test, Path.Combine(dest, "cache"), onProgress, onError, true))
						return;
					string[] test2 = new string[] { "120mm.shp", "dbris6lg.shp", "gclock2.shp", "nwalicon.shp", "ship.shp", "50cal.shp", "dbris6sm.shp", "gghunt.shp", "nwepicon.shp", "shroud.shp", 
					"dbris7lg.shp", "ghost.shp", "obliicon.shp", "shroudx.shp", "alphatst.shp", "dbris7sm.shp", "gosticon.shp", "obmbicon.shp", "slav.shp", "apchicon.shp", "dbris8lg.shp", "gunfire.shp",
					"orcaicon.shp", "smchicon.shp" };

 					/*
					apcicon.shp   dbris8sm.shp  h2o_exp1.shp  otrnicon.shp  smech.shp
					apwricon.shp  dbris9lg.shp  h2o_exp2.shp  oxanna.shp    smokey2.shp
					armor.shp     dbris9sm.shp  h2o_exp3.shp  palet01.shp   smokey.shp
					arrow.shp     dbrs10lg.shp  handicon.shp  palet02.shp   smokland.shp
					artyicon.shp  dbrs10sm.shp  healall.shp   palet03.shp   soniicon.shp
					batricon.shp  death_a.shp   healone.shp   palet04.shp   spoticon.shp
					beacon.shp    death_b.shp   heliicon.shp  parabomb.shp  sredsmk1.shp
					bggyicon.shp  death_c.shp   hlight.shp    parach.shp    static.shp
					bomb.shp      death_d.shp   hmecicon.shp  patriot.shp   steampuf.shp
					brrkicon.shp  death_e.shp   hovricon.shp  paveicon.shp  stnkicon.shp
					buildngz.shp  death_f.shp   infdie.shp    piffpiff.shp  s_tumu22.shp
					burn-l.shp    detnicon.shp  inviso.shp    piff.shp      s_tumu30.shp
					burn-m.shp    dig.shp       ionbeam.shp   pips2.shp     s_tumu42.shp
					burn-s.shp    dirtexpl.shp  ioncicon.shp  pips.shp      s_tumu60.shp
					canister.shp  discus.shp    jjeticon.shp  place.shp     subticon.shp
					cellsel.shp   doggie.shp    jumpjet.shp   plticon.shp   techicon.shp
					chamicon.shp  dragon.shp    key.ini       plugicon.shp  tickicon.shp
					chamspy.shp   dropexp.shp   lasricon.shp  podring.shp   tmplicon.shp
					chemicon.shp  droppod2.shp  lgrysmk1.shp  pod.shp       torpedo.shp
					chemisle.shp  droppod.shp   liteicon.shp  podsicon.shp  towricon.shp
					civ1.shp      drum01.shp    lredsmk1.shp  powricon.shp  tratos.shp
					civ2.shp      drum02.shp    medic.shp     progbar2.shp  treesprd.shp
					civ3.shp      e1.shp        mediicon.shp  progbarm.shp  turbicon.shp
					clckicon.shp  e2icon.shp    metdebri.shp  progbar.shp   twinkle1.shp
					cldrngl1.shp  e2.shp        metlarge.shp  proicon.shp   twinkle2.shp
					cldrngl2.shp  e3.shp        metltral.shp  pulsball.shp  twinkle3.shp
					cldrngmd.shp  e4icon.shp    metricon.shp  pulsefx1.shp  twlt026.shp
					cldrngsm.shp  ebtn-dn.shp   metsmall.shp  pulsefx2.shp  twlt036.shp
					cloak.shp     ebtn-up.shp   metstral.shp  pulsicon.shp  twlt050.shp
					cloud1d.shp   e_enhn.shp    mgun-e.shp    rad1icon.shp  twlt070.shp
					cloud1.shp    electro.shp   mgun-ne.shp   rad2icon.shp  twlt100.shp
					cloud2d.shp   emp_fx01.shp  mgun-n.shp    rad3icon.shp  twr1icon.shp
					cloud2.shp    engineer.shp  mgun-nw.shp   radricon.shp  twr2icon.shp
					crat01.shp    explolrg.shp  mgun-se.shp   rboticon.shp  twr3icon.shp
					crat02.shp    explomed.shp  mgun-s.shp    rclock2.shp   umagicon.shp
					crat03.shp    explosml.shp  mgun-sw.shp   reveal.shp    umagon.shp
					crat04.shp    facticon.shp  mgun-w.shp    ring1.shp     veteran.shp
					crat0a.shp    fire1.shp     mhijack.shp   ring.shp      visc_lrg.shp
					crat0b.shp    fire2.shp     missile.shp   samicon.shp   visc_sml.shp
					crat0c.shp    fire3.shp     mltiicon.shp  sapcicon.shp  vislgatk.shp
					crryicon.shp  fire4.shp     mltimisl.shp  sbagicon.shp  vislrg.shp
					cybcicon.shp  firepowr.shp  mmchicon.shp  s_bang16.shp  vissml.shp
					cybiicon.shp  flak.shp      mmch.shp      s_bang24.shp  wallicon.shp
					cyborg.shp    flameall.shp  money.shp     s_bang34.shp  wasticon.shp
					cyc2.shp      flameguy.shp  mouse.shp     s_bang48.shp  weapicon.shp
					cyclicon.shp  flamthro.shp  msslicon.shp  s_brnl20.shp  weaticon.shp
					darken.shp    fog.shp       mutant3.shp   s_brnl30.shp  weedicon.shp
					dbris1lg.shp  fsair.shp     mutant.shp    s_brnl40.shp  weed.shp
					dbris1sm.shp  fsdicon.shp   mutcicon.shp  s_brnl58.shp  w_piff.shp
					dbris2lg.shp  fsgrnd.shp    mwmn.shp      s_clsn16.shp  xgrymed1.shp
					dbris2sm.shp  fsidle.shp    nga2icon.shp  s_clsn22.shp  xgrymed2.shp
					dbris3lg.shp  fspicon.shp   ngaticon.shp  s_clsn30.shp  xgrysml1.shp
					dbris3sm.shp  fstdicon.shp  nhpdicon.shp  s_clsn42.shp  xgrysml2.shp
					dbris4lg.shp  gaslrgmk.shp  npwricon.shp  s_clsn58.shp  xxicon.shp
					dbris4sm.shp  gat2icon.shp  nradicon.shp  seekicon.shp
					dbris5lg.shp  gateicon.shp  ntchicon.shp  select.shp
					dbris5sm.shp  gbayicon.shp  null.shp      sgrysmk1.shp
					 */

					//if (!InstallUtils.ExtractceptionFromPackage(source, installPackages[0], "conquer.mix", test, Path.Combine(dest, "conquer"), onProgress, onError, true))
					//	return;
					//if (!InstallUtils.PackageFiles("ts-base.orapak", baseFiles, dest, onProgress, onError))
					//	return;

					//Scores
					if (!InstallUtils.ExtractFromPackage(source, installPackages[1], scoresFiles, dest, onProgress, onError, true))
						return;
					if (!InstallUtils.PackageFiles("ts-scores.mix", scoresFiles, dest, onProgress, onError))
						return;

					//Movies
					if (!InstallUtils.ExtractFromPackage(source, installPackages[2], moviesFiles, dest, onProgress, onError, true))
						return;
					if (!InstallUtils.PackageFiles("ts-movies.mix", moviesFiles, dest, onProgress, onError))
						return;

					Game.RunAfterTick(() =>
					{
						Ui.CloseWindow();
						continueLoading();
					});
				}
				catch
				{
					onError("Installation failed");
				}
			}) { IsBackground = true };
			t.Start();
		}
	}
}

