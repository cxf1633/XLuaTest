using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Build {
	public static class GitVersionStat {
		private static Dictionary<GUID, int> _modifiedAssets;

		public static int CheckAssetIsModified(GUID guid) {
			if( _modifiedAssets.TryGetValue(guid, out var modifyValue) ) {
				return modifyValue;
			}

			return 0;
		}

		public static void FetchGitLogAndCollect(string startTime = "2023-1-16") {
			_modifiedAssets = new Dictionary<GUID, int>();
			var process = new Process();
			process.StartInfo = new ProcessStartInfo {
#if UNITY_EDITOR_WIN
				FileName = "git.exe",
#else
				FileName = "git",
#endif
				Arguments = $"log --no-merges --name-status --pretty=oneline --since={startTime} -- {Application.dataPath}",
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				UseShellExecute = false
			};
			process.Start();
			var reader = process.StandardOutput;
			var gitLog = "";
			while( !process.HasExited ) {
				gitLog += reader.ReadToEnd();
			}

			gitLog += reader.ReadToEnd();
			using( var logReader = new StringReader(gitLog) ) {
				var logLine = logReader.ReadLine();
				do {
					try
					{
						if( logLine.EndsWith(".meta") ) {
							logLine = logLine.Substring(0, logLine.Length - 5);
						}

						var modifyValue = logLine.StartsWith("M") ? 1 : logLine.StartsWith("A") ? 2 : 0;
						if( modifyValue > 0 ) {
							var assetPath = logLine.Substring(2 + 22);
							var assetGUID = AssetDatabase.AssetPathToGUID(assetPath);

							if( GUID.TryParse(assetGUID, out var guid) ) {
								if( _modifiedAssets.TryGetValue(guid, out var existModifyValue) ) {
									if( existModifyValue < modifyValue ) {
										_modifiedAssets[guid] = modifyValue;
									}
								}
								else {
									_modifiedAssets.Add(guid, modifyValue);
								}
							}
						}

						logLine = logReader.ReadLine();
					}
					catch (Exception e)
					{
						Console.WriteLine(e);
					//	throw;
					}
				} while( !string.IsNullOrEmpty(logLine) );
			}
		}
	}
}