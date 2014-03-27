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

			//These things are hopefully not permanent since the files really should be installed into packages not folders !!!!
			var baseDest = new string[] { dest, "ts-base", }.Aggregate(Path.Combine);
			var scoresDest = new string[] { dest, "ts-scores" }.Aggregate(Path.Combine);
			var moviesDest = new string[] { dest, "ts-movies", }.Aggregate(Path.Combine);

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

					//Base Content
					if (!InstallUtils.ExtractFromPackage(source, installPackages[0], baseFiles, baseDest, onProgress, onError, true))
						return;

					//Scores
					if (!InstallUtils.ExtractFromPackage(source, installPackages[1], scoresFiles, scoresDest, onProgress, onError, true))
						return;

					//Movies
					if (!InstallUtils.ExtractFromPackage(source, installPackages[2], moviesFiles, moviesDest, onProgress, onError, true))
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

