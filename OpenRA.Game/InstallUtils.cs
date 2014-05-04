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
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Zip;
using OpenRA.FileSystem;

namespace OpenRA
{
	public static class InstallUtils
	{
		static IEnumerable<ZipEntry> GetEntries(this ZipInputStream z)
		{
			for (;;)
			{
				var e = z.GetNextEntry();
				if (e != null) yield return e; else break;
			}
		}

		public static string GetMountedDisk(Func<string, bool> isValidDisk)
		{
			var volumes = DriveInfo.GetDrives()
				.Where(v => v.DriveType == DriveType.CDRom && v.IsReady)
				.Select(v => v.RootDirectory.FullName);

			return volumes.FirstOrDefault(v => isValidDisk(v));
		}

		// TODO: The package should be mounted into its own context to avoid name collisions with installed files
		public static bool ExtractFromPackage(string srcPath, string package, string[] files, string destPath, Action<string> onProgress, Action<string> onError, bool encrypted = false)
		{
			if (!Directory.Exists(destPath))
				Directory.CreateDirectory(destPath);

			if (!Directory.Exists(srcPath)) { onError("Cannot find " + package); return false; }
			GlobalFileSystem.Mount(srcPath);
			if (!GlobalFileSystem.Exists(package)) { onError("Cannot find " + package); return false; }
			{
				if (encrypted == true)
				{
					GlobalFileSystem.Mount(package, "CRC32");
				}
				else
				{
					GlobalFileSystem.Mount(package);
				}
			}

			foreach (string s in files)
			{
				var destFile = Path.Combine(destPath, s);
				if (!File.Exists(destFile)) //Should skip existing files :)
				{
					using (var sourceStream = GlobalFileSystem.Open(s))
					using (var destStream = File.Create(destFile))
					{
						onProgress("Extracting " + s);
						destStream.Write(sourceStream.ReadAllBytes());
					}
				}
			}

			return true;
		}

		public static bool ExtractceptionFromPackage(string srcPath, string package, string inpackage, string[] files, string destPath, Action<string> onProgress, Action<string> onError, bool encrypted = false)
		{
			//mount main mix on cd
			//extract sets of files from individual mixes
			if (!Directory.Exists(destPath))
				Directory.CreateDirectory(destPath);

			if (!Directory.Exists(srcPath)) { onError("Cannot find " + package); return false; }
			GlobalFileSystem.Mount(srcPath);
			if (!GlobalFileSystem.Exists(package)) { onError("Cannot find " + package); return false; }
			{
				if (encrypted == true)
				{
					GlobalFileSystem.Mount(package, "CRC32");
					GlobalFileSystem.Mount(inpackage, "CRC32");
				}
				else
				{
					GlobalFileSystem.Mount(package);
					GlobalFileSystem.Mount(inpackage);
				}
			}

			foreach (string s in files)
			{
				var destFile = Path.Combine(destPath, s);
				if (!File.Exists(destFile)) //Should skip existing files :)
				{
					using (var sourceStream = GlobalFileSystem.Open(s))
					using (var destStream = File.Create(destFile))
					{
						onProgress("Extracting " + s);
						destStream.Write(sourceStream.ReadAllBytes());
					}
				}
			}

			return true;
		}

		public static bool CopyFiles(string srcPath, string[] files, string destPath, Action<string> onProgress, Action<string> onError)
		{
			foreach (var file in files)
			{
				var fromPath = Path.Combine(srcPath, file);
				if (!File.Exists(fromPath))
				{
					onError("Cannot find " + file);
					return false;
				}

				var destFile = Path.GetFileName(file).ToLowerInvariant();
				onProgress("Extracting " + destFile);
				File.Copy(fromPath,	Path.Combine(destPath, destFile), true);
			}

			return true;
		}

		public static bool PackageFiles(string package, string[] files, string destPath, Action<string> onProgress, Action<string> onError)
		{	
			//InstallUtils.ExtractFromPackage(source, installPackages[0], baseFiles, baseDest, onProgress, onError, true))
			var packFiles = new Dictionary<string, byte[]>();

			//ExtractFromPackage(srcPath, package, files, destPath, onProgress, onError, encrypted);
			//Console.WriteLine("Package extracted!");

			int ff = 0;
			foreach (var f in files)
				{
					packFiles.Add(f, File.ReadAllBytes(Path.Combine(destPath, files[ff])));
					ff++;
				}
			Console.WriteLine("Files loaded into dictionary");

			//check if file already exists if so update it rather than create new
			if (File.Exists(Path.Combine(destPath, package)))
			{
				onProgress("Updating " + package);
				Console.WriteLine("KANE IS ALREADY HERE!");
			}
			else
			{
				onProgress("Creating " + package);
				Console.WriteLine(destPath);
				GlobalFileSystem.CreatePackage(Path.Combine(destPath, package), int.MaxValue, packFiles);
			}
			//remove strays
			int d = 0;
			foreach (var f in files)
			{
				File.Delete(Path.Combine(destPath, files[d]));
				//packFiles.Add(f, File.ReadAllBytes(Path.Combine(destPath, files[ff])));
				d++;
			}
			Console.WriteLine("orapak created!!!!");

			return true;
		}

		public static bool ExtractZip(string zipFile, string dest, Action<string> onProgress, Action<string> onError)
		{
			if (!File.Exists(zipFile))
			{
				onError("Invalid path: " + zipFile);
				return false;
			}

			List<string> extracted = new List<string>();
			try
			{
				var z = new ZipInputStream(File.OpenRead(zipFile));
				z.ExtractZip(dest, extracted, s => onProgress("Extracting " + s));
			}
			catch (SharpZipBaseException)
			{
				foreach (var f in extracted)
					File.Delete(f);

				onError("Invalid archive");
				return false;
			}

			return true;
		}

		// TODO: this belongs in FileSystem/ZipFile
		static void ExtractZip(this ZipInputStream z, string destPath, List<string> extracted, Action<string> onProgress)
		{
			foreach (var entry in z.GetEntries())
			{
				if (!entry.IsFile) continue;

				onProgress(entry.Name);

				Directory.CreateDirectory(Path.Combine(destPath, Path.GetDirectoryName(entry.Name)));
				var path = Path.Combine(destPath, entry.Name);
				extracted.Add(path);

				using (var f = File.Create(path))
				{
					int bufSize = 2048;
					byte[] buf = new byte[bufSize];
					while ((bufSize = z.Read(buf, 0, buf.Length)) > 0)
					f.Write(buf, 0, bufSize);
				}
			}

			z.Close();
		}
	}
}
