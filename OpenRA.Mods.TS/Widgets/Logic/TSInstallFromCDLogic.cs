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

		public readonly Dictionary<string, string> Packages;

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
			string[] hashFiles = { "TS1.DSK", "TS2.DSK", "TS3.DSK" }; //again load from yaml i guess is the right way
			string[] knownHashes = { "CE-33-15-C4-FA-F7-D7-7D-B2-D2-30-7D-2D-17-1E-8D-BE-91-48-97", "4B-2E-EE-3E-28-33-EC-16-DF-FB-41-4D-69-8B-CF-E6-67-9C-65-94", "" }; //load from YAML l8a

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

		static string[] YamlList(Dictionary<string, MiniYaml> yaml, string key)
		{
			if (!yaml.ContainsKey(key))
				return new string[] { };

			return yaml[key].NodesDict.Keys.ToArray();
		}

		void Install(string source, int contentID)
		{
			backButton.IsDisabled = () => true;
			retryButton.IsDisabled = () => true;
			insertDiskContainer.IsVisible = () => false;
			installingContainer.IsVisible = () => true;

			var dest = new string[] { Platform.SupportDir, "Content", "ts" }.Aggregate(Path.Combine);

			//ContentID controls which values are loaded from yaml
			var yamlpath = new[] { "mods", "ts", "install.yaml" }.Aggregate(Path.Combine);
			var yaml = new MiniYaml(null, MiniYaml.FromFile(yamlpath)).NodesDict;

			//Base
			var baseDest = new string[] { dest, "ts-base", }.Aggregate(Path.Combine);
			var baseDiscFile = YamlList(yaml, "BaseDiscFile").ToString(); //This ToString() thing needs to be replaced
			var baseFiles = YamlList(yaml, "BaseFiles");

			Console.WriteLine(baseDest);
			Console.WriteLine(baseDiscFile);
			Console.WriteLine(baseFiles);

			//Base Scores
			var scoresDest = new string[] { dest, "ts-scores" }.Aggregate(Path.Combine);
			var scoresDiscFile = "SCORES.MIX";
			var scoresFiles = new string[] { "nodcrush.aud", "duskhour.aud", "scout.aud", "defense.aud", "infrared.aud", "pharotek.aud", "timebomb.aud", "whatlurk.aud", "redsky.aud", "heroism.aud", 
				"mutants.aud", "flurry.aud", "storm.aud", "valves1b.aud", "madrap.aud", "approach.aud", "lonetrop.aud", "gloom.aud", "score.aud" };

			//Base Movies
			var moviesDest = new string[] { dest, "ts-movies", }.Aggregate(Path.Combine);
			var moviesDiscFile = "MOVIES01.MIX";
			var moviesFiles = new string[] { "cache.mix", "conquer.mix", "isosnow.mix", "isotemp.mix", "local.mix", "sidec01.mix", "sidec02.mix", "sno.mix", "snow.mix", "sounds.mix", "speech01.mix", 
				"speech02.mix", "tem.mix", "temperat.mix" };

			//var dest = new string[] { Platform.SupportDir, "Content", "ts" }.Aggregate(Path.Combine);
			//var copyFiles = new string[] { "install/tibsun.mix", "scores.mix", "multi.mix"};

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

