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

		//Here to.....
		public string[] hashFiles = new string[] { };
		public string[] knownHashes = new string[] { };

		public string baseDiscFile = "";
		public string[] baseFiles = new string[] { };

		public string scoresDiscFile = "";
		public string[] scoresFiles = new string[] { };

		public string moviesDiscFile = "";
		public string[] moviesFiles = new string[] { };
		//.... HERE!

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
			//Basic quick check
			Func<string, bool> ValidDiskFilter = diskRoot => File.Exists(diskRoot+Path.DirectorySeparatorChar+"multi.mix") &&
					File.Exists(new string[] { diskRoot, "install", "tibsun.mix" }.Aggregate(Path.Combine));

			var path = InstallUtils.GetMountedDisk(ValidDiskFilter);

			thisisnotthevoidyourlookingfor(true, 0); //Placeholder for stuff that should be loaded from YAML

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

								if (knownHashes[contentID] == discHash)
									Install(path, contentID);
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

		void thisisnotthevoidyourlookingfor(bool instchk, int content) //All things that should be loaded externally, here for lazy testing
		{
			if (instchk == true)
			{
				hashFiles = new string[] { "TS1.DSK", "TS2.DSK", "TS3.DSK" };
				knownHashes = new string[] { "CE-33-15-C4-FA-F7-D7-7D-B2-D2-30-7D-2D-17-1E-8D-BE-91-48-97", "4B-2E-EE-3E-28-33-EC-16-DF-FB-41-4D-69-8B-CF-E6-67-9C-65-94", "BC-56-44-49-A1-5A-61-E5-D3-50-C2-63-2D-32-7A-B3-86-D9-91-C9" };
			}
			else
			{
				switch(content)
				{
					case 0:
						//GDI
						baseDiscFile = "INSTALL/TIBSUN.MIX";
						baseFiles = new string[] { "cache.mix", "conquer.mix", "isosnow.mix", "isotemp.mix", "local.mix", "sidec01.mix", "sidec02.mix", "sno.mix", "snow.mix", "sounds.mix", "speech01.mix", 
						"speech02.mix", "tem.mix", "temperat.mix" };

						//GDI Scores
						scoresDiscFile = "SCORES.MIX";
						scoresFiles = new string[] { "nodcrush.aud", "duskhour.aud", "scout.aud", "defense.aud", "infrared.aud", "pharotek.aud", "timebomb.aud", "whatlurk.aud", "redsky.aud", "heroism.aud", 
						"mutants.aud", "flurry.aud", "storm.aud", "valves1b.aud", "madrap.aud", "approach.aud", "lonetrop.aud", "gloom.aud", "score.aud" };

						//GDI Movies
						moviesDiscFile = "MOVIES01.MIX";
						moviesFiles = new string[] { "beachead.vqa", "coup.vqa", "diskdest.vqa", "empulse.vqa", "eva.vqa", "gdi01_sb.vqa", "gdi02_sb.vqa", "gdi03_sb.vqa", "gdi_finl.vqa", "gdi_m02.vqa", "gdi_m03.vqa", 
						"gdi_m04.vqa", "gdi_m05.vqa", "gdi_m06.vqa", "gdi_m07.vqa", "gdi_m08.vqa", "gdi_m09a.vqa", "gdi_m09b.vqa", "gdi_m09c.vqa", "gdi_m10a.vqa", "gdi_m11.vqa", "gdi_m12a.vqa", "gdim09cw.vqa", 
						"gdim09d1.vqa", "genwin01.vqa", "hideseek.vqa", "iceskate.vqa", "intro.vqa", "killmech.vqa", "mechatak.vqa", "n_logo_w.vqa", "nod_flag.vqa", "nowcnot.vqa", "orcastrk.vqa", "podasslt.vqa", 
						"retrbtn.vqa", "startup.vqa", "trainrob.vqa", "ufoguard.vqa", "unstpble.vqa", "wwlogo.vqa" };
						break;

					case 1:
						//Nod
						baseDiscFile = "INSTALL/TIBSUN.MIX";
						baseFiles = new string[] { "cache.mix", "conquer.mix", "isosnow.mix", "isotemp.mix", "local.mix", "sidec01.mix", "sidec02.mix", "sno.mix", "snow.mix", "sounds.mix", "speech01.mix", 
						"speech02.mix", "tem.mix", "temperat.mix" };

						//Nod Scores
						scoresDiscFile = "SCORES.MIX";
						scoresFiles = new string[] { "nodcrush.aud", "duskhour.aud", "scout.aud", "defense.aud", "infrared.aud", "pharotek.aud", "timebomb.aud", "whatlurk.aud", "redsky.aud", "heroism.aud", 
						"mutants.aud", "flurry.aud", "storm.aud", "valves1b.aud", "madrap.aud", "approach.aud", "lonetrop.aud", "gloom.aud", "score.aud" };

						//Nod Movies
						moviesDiscFile = "MOVIES02.MIX";
						moviesFiles = new string[] { "cap_trat.vqa", "dambreak.vqa", "diskdest.vqa", "eva.vqa", "gdi_flag.vqa", "gdi_logo.vqa", "gennodl1.vqa", "genwin01.vqa", "icbmlnch.vqa", "intro.vqa",
						"kill_gdi.vqa", "killmech.vqa", "n_logo_l.vqa", "n_logo_w.vqa", "nod01_sb.vqa", "nod02_sb.vqa", "nod06abw.vqa", "nod_finl.vqa", "nod_flag.vqa", "nod_m02.vqa", "nod_m03.vqa", "nod_m04.vqa", 
						"nod_m05.vqa", "nod_m06.vqa", "nod_m07.vqa", "nod_m08.vqa", "nod_m09.vqa", "nod_m10.vqa", "nod_m11.vqa", "nod_m12.vqa", "nowcnot.vqa", "retrbtn.vqa", "startup.vqa", "tenevict.vqa", 
						"unstpble.vqa", "wwlogo.vqa" };
						break;

					case 2:
						//Firestorm
						baseDiscFile = "";
						baseFiles = new string[] {  };

						//Firestorm Scores
						scoresDiscFile = "SCORES01.MIX";
						scoresFiles = new string[] {  };

						//Firestrom Movies
						moviesDiscFile = "MOVIES03.MIX";
						moviesFiles = new string[] { };
						break;
				}	
			}	
		}

		void Install(string source, int contentID)
		{
			backButton.IsDisabled = () => true;
			retryButton.IsDisabled = () => true;
			insertDiskContainer.IsVisible = () => false;
			installingContainer.IsVisible = () => true;

			var dest = new string[] { Platform.SupportDir, "Content", "ts" }.Aggregate(Path.Combine);

			//These things are hopefully not permanent

			var baseDest = new string[] { dest, "ts-base", }.Aggregate(Path.Combine);
			var scoresDest = new string[] { dest, "ts-scores" }.Aggregate(Path.Combine);
			var moviesDest = new string[] { dest, "ts-movies", }.Aggregate(Path.Combine);
			var datatypes = new string[] { "base", "scores", "movies" };

			thisisnotthevoidyourlookingfor(false, contentID); //<-- Bad code =P

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
					//Merge the below code into something more efficent and things

					//Base Content
					if (!InstallUtils.ExtractFromPackage(source, baseDiscFile, baseFiles, baseDest, onProgress, onError, true))
						return;

					//Scores
					if (!InstallUtils.ExtractFromPackage(source, scoresDiscFile, scoresFiles, scoresDest, onProgress, onError, true))
						return;

					//Movies
					if (!InstallUtils.ExtractFromPackage(source, moviesDiscFile, moviesFiles, moviesDest, onProgress, onError, true))
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

