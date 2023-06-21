using System;
using System.Collections.Generic;
using System.IO;
using Extend.Editor;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets.Build.AnalyzeRules;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace CustomBuildPipeline.Editor
{
	public static partial class CommandLine
	{
		private static bool noNative = false;

		[UnityEditor.InitializeOnLoadMethod]
		private static void Init()
		{
		}

		[MenuItem("Tools/Clear Progressbar")]
		private static void ClearProgressBar()
		{
			EditorUtility.ClearProgressBar();
		}

		public static void GenerateCSCode()
		{
			EditorApplication.ExecuteMenuItem("Assets/Open C# Project");
		}

		// [MenuItem("Tools/Asset/自动检查")]
		// public static void CheckAssets()
		// {
		// 	string savePath = "Assets/Settings/AssetsCheckSettings.asset";
		// 	var _settings = AssetDatabase.LoadAssetAtPath<AssetsCheckSettings>(savePath);
		// 	CheckManager.InitCheckList(_settings);
		// 	CheckManager.StartChecksAndSaveData(_settings);
		// }

		// public static void CheckAssetsCmd()
		// {
		// 	string savePath = "Assets/Settings/AssetsCheckSettings.asset";
		// 	var _settings = AssetDatabase.LoadAssetAtPath<AssetsCheckSettings>(savePath);
		// 	CheckManager.InitCheckList(_settings);
		// 	var path = GetArg("-xlsxPath");
		// 	CheckManager.StartChecksAndSaveData(path, _settings);
		// }

		public static string GetArg(string name)
		{
			var args = Environment.GetCommandLineArgs();
			for (var i = 0; i < args.Length; i++)
				if (args[i] == name && args.Length > i + 1)
					return args[i + 1];

			return null;
		}

		/// <summary>
		/// 处理PBXProject设置(iOS端)
		/// </summary>
		[PostProcessBuild]
		public static void OnPostBuild(BuildTarget buildTarget, string buildPath)
		{
			if (buildTarget != BuildTarget.iOS || noNative)
			{
				return;
			}
#if UNITY_IOS
			Debug.LogWarning("Wwise post build");
            string pbxProjPath = PBXProject.GetPBXProjectPath(buildPath);
            var pbxProject = new PBXProject();
            pbxProject.ReadFromString(File.ReadAllText(pbxProjPath));

            //获取UnityFramework的UUID
#if UNITY_2019_3_OR_NEWER
            string targetGuid = pbxProject.GetUnityFrameworkTargetGuid();
#else
            string targetGuid = pbxProject.TargetGuidByName(PBXProject.GetUnityTargetName());
#endif
            //分解为多个步骤来配置iOS的工程，在每一步根据对应的Option，进行对应的操作

            // 1.设置关闭Bitcode（如果不需要，可注释掉）
            //pbxProject.SetBuildProperty(targetGuid, ENABLE_BITCODE_KEY, "NO");

            SetSystemFrameworks(pbxProject, targetGuid);
            Debug.Log("------------pbxProjPath is "+pbxProjPath);
            //.重新写回配置
            File.WriteAllText(pbxProjPath, pbxProject.WriteToString());
        }

        private static void SetSystemFrameworks(PBXProject pbxProject, string targetGuid)
        {
            //获取UnityFramework下的PBXResourcesBuildPhase Section UUID
            string resourceTarget = pbxProject.GetResourcesBuildPhaseByTarget(targetGuid);
            //根据Data目录相对路径获取UUID
            string resGUID = pbxProject.FindFileGuidByProjectPath("Data");
            //添加Data UUID进file目录
            pbxProject.AddFileToBuildSection(targetGuid, resourceTarget, resGUID);

            // 2、添加系统Framework
           // pbxProject.AddFrameworkToProject(targetGuid, "Contacts.framework", false);

            //3.设置NativeCallProxy权限
            var guidHeader = pbxProject.FindFileGuidByProjectPath("Libraries/Plugins/iOS/NativeCallProxy.h");
            pbxProject.AddPublicHeaderToBuild(targetGuid, guidHeader);
            
            //4.添加头文件
            pbxProject.AddBuildProperty(targetGuid, "HEADER_SEARCH_PATHS", "$(SRCROOT)/Libraries/Wwise/API/Runtime/Plugins/iOS/Include");
            
            //拷贝库文件
            /*pbxProject.AddFileToBuild(targetGuid,
                pbxProject.AddFolderReference("Include", "$(SRCROOT)/Libraries/Wwise/API/Runtime/Plugins/iOS/",
          
                    PBXSourceTree.Source));*/
#endif
		}

		#region Command Build
		#region 热更包相关
		public static void BuildAddressable()
		{
			string _contentbinPath = GetArg("-contentbinPath");
			bool.TryParse(GetArg("-base"), out bool _base);
			Debug.Log("[UnityBuildLog:] Auto Generate Addressable Group");
			if (_base)
				AutoGroupAssets();
			Debug.Log("[UnityBuildLog:] Build Addressable Assets");

			AddressableBuildScript.BuildAddressables(_base, _contentbinPath);
		}
		public static void BuildIOSAAProject()
		{
			BuildAAProject(BuildTarget.iOS);
		}
		public static void BuildAndroidAAProject()
		{
			BuildAAProject(BuildTarget.Android);
		}

		private static void BuildAAProject(BuildTarget target)
		{
			try
			{
				SwitchPlatform(target);
				var needBuilPlayer = NeedBuildPlayer();
				if(needBuilPlayer)
					UnitySettings(target,true);
				if (NeedBuildAA())
					BuildAddressable();
				if (needBuilPlayer)
					BuildPlayerAll(target);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				EditorApplication.Exit(1);
			}
		}
		#endregion

		#region 全量包相关

		private static void BuildLocalProject(BuildTarget target)
		{
			try
			{
				SwitchPlatform(target);
				UnitySettings(target,true,true);
				if (NeedBuildAA())
					BuildAddressableDebug();
				if (NeedBuildPlayer())
					BuildPlayerAll(target);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				EditorApplication.Exit(1);
			}
		}
		public static void BuildAndroidLocalProject()
		{
			BuildLocalProject(BuildTarget.Android);
		}
		public static void BuildIOSLocalProject()
		{
			BuildLocalProject(BuildTarget.iOS);
		}
		
		#endregion
		private static bool NeedBuildAA()
		{
			bool buildaa = true;
			string _buildaa = GetArg("-buildaa");
			if (!string.IsNullOrEmpty(_buildaa))
			{
				bool.TryParse(_buildaa, out buildaa);
			}

			return buildaa;
		}

		private static bool NeedBuildPlayer()
		{
			bool.TryParse(GetArg("-base"), out bool _base);
			return _base;
		}
		
		private static void ClearLuaGen()
		{
			string common_path = Application.dataPath + "/XLua/Gen/";
			if (Directory.Exists(common_path))
			{
				Directory.Delete(common_path, true);
				AssetDatabase.DeleteAsset(common_path.Substring(common_path.IndexOf("Assets") + "Assets".Length));
				AssetDatabase.Refresh();
			}
		}

		private static string SetDefine(string symbol, string define, bool isAdd)
		{
			string result = symbol;
			if (isAdd)
			{
				if (!symbol.Contains(define))
				{
					result = symbol + ";" + define;
				}
			}
			else
			{
				if (symbol.Contains(define))
				{
					int index = symbol.IndexOf(define);
					if (index < 0)
					{
						return symbol;
					}

					if (index > 0) //如果不在第一个  把前边的分号删掉
					{
						index -= 1;
					}

					int _length = define.Length;
					if (symbol.Length > _length) //如果长度大于当前长度，才有分号
					{
						_length += 1;
					}

					result = symbol.Remove(index, _length);
				}
			}

			return result;
		}

		private static string GetSymbolString(BuildTarget target)
		{
			string result = "";
			switch (target)
			{
				case BuildTarget.iOS:
					result = UnityEditor.PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS);
					break;
				case BuildTarget.Android:
					result =
						UnityEditor.PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android);
					break;
				case BuildTarget.StandaloneWindows64:
					result =
						UnityEditor.PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
					break;
			}

			return result;
		}

		public static void UnitySettings(BuildTarget buildTarget, bool isNative,bool fullPackage = false,Action callback = null)
		{
			string symboldefine = GetSymbolString(buildTarget);

			if (!isNative)
			{
				if (buildTarget == BuildTarget.Android)
					EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
			}
			else
			{
				string _strDefine = GetArg("-define");
				symboldefine = SetDefine(symboldefine, _strDefine, true);
				if (buildTarget == BuildTarget.Android)
				{
					EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
					EditorUserBuildSettings.androidCreateSymbolsZip = true;
				}
			}
			symboldefine = SetDefine(symboldefine, "FULL_PACKAGE", fullPackage);
			string _prod = "false";//GetArg("-prod");
			// if (string.IsNullOrEmpty(_prod))
			// {
			// 	throw new Exception("-prod is not define");
			// }

			BuildTargetGroup group;
			switch( buildTarget ) {
				case BuildTarget.iOS:
					group = BuildTargetGroup.iOS;
					break;
				case BuildTarget.Android:
					group = BuildTargetGroup.Android;
					break;
				default:
					group = BuildTargetGroup.Standalone;
					break;
			}
			PlayerSettings.SetIl2CppCompilerConfiguration(group, Il2CppCompilerConfiguration.Master);
			_prod = _prod.ToLower();
			switch (_prod)
			{
				case "true":
					symboldefine = SetDefine(symboldefine, "XIAOICE_PROD", true);
					
					SetDevelopmentMode(false);
					break;
				case "false":
					symboldefine = SetDefine(symboldefine, "XIAOICE_PROD", false);
					
					SetDevelopmentMode(true);
					break;
			}

			if (_prod != "false" || buildTarget != BuildTarget.Android)
				symboldefine = SetDefine(symboldefine, "EMMY_CORE_SUPPORT", false);

			symboldefine = SetDefine(symboldefine, "LUA_WRAP_CHECK", false);

			Debug.Log("[UnityBuildLog:] symbol is " + symboldefine);
			var targetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
			UnityEditor.PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, symboldefine);
			CompilationPipeline.compilationFinished += (_) => { callback?.Invoke(); };
			CompilationPipeline.RequestScriptCompilation();
		}

		private static void SwitchPlatform(BuildTarget buildTarget)
		{
			if (buildTarget != EditorUserBuildSettings.activeBuildTarget)
			{
				Debug.Log("Start switch platform to: " + buildTarget);
				var beginTime = System.DateTime.Now;
				var targetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
				EditorUserBuildSettings.SwitchActiveBuildTarget(targetGroup, buildTarget);
				Debug.Log("End switch platform to: " + buildTarget);
				Debug.Log("[UnityBuildLog:] Build SwitchPlatform Time : " +
				          (System.DateTime.Now - beginTime).TotalSeconds);
				CSObjectWrapEditor.Generator.ClearAll();
			}
		}

		#endregion

		#region 打测试包（无native）

		[MenuItem("Tools/Build/资源本地测试包")]
		public static void BuildAndroidNoNative()
		{
			BuildUnityOnly(BuildTarget.Android);
		}
		
		public static void BuildIOSNoNative()
		{
			BuildUnityOnly(BuildTarget.iOS);
		}
		[MenuItem("Tools/Build/BuildWindow")]
		public static void BuildWin()
		{
			PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
			PlayerSettings.defaultScreenHeight = 1080;
			PlayerSettings.defaultScreenWidth = 608;
			BuildUnityOnly(BuildTarget.StandaloneWindows64);
		}
		public static void BuildUnityOnly(BuildTarget target) {
			try
			{
				noNative = true;
				SwitchPlatform(target);
				UnitySettings(target,false,true);
				BuildWithOutNative(target);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
				// EditorApplication.Exit(1);
			}
		}

		public static void BuildWithOutNative(BuildTarget buildTarget)
		{
			//检查BuildSceneList
			if (!CheckScenesInBuildValid())
			{
				return;
			}

			Debug.Log("[UnityBuildLog:] Set Platform=");
			
			
			Debug.Log("[UnityBuildLog:] Generate Xlua Wrap Script=");
			CSObjectWrapEditor.Generator.GenAll();

			AssetDatabase.Refresh();

			Debug.Log("[UnityBuildLog:] Build Lua Script=");
			FileFormatUtils.ConvertLuaFileFormatAndEncode();
			PlayerBuild.PackLuaZipAndCopy();
			
			//0.根据buildTarget区分BuildGroup
			BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
			if (BuildTargetGroup.Unknown == buildTargetGroup)
			{
				throw new System.Exception(
					string.Format("{0} is Unknown Build Platform ! Build Failture!", buildTarget));
			}


			UnityEditor.EditorUserBuildSettings.exportAsGoogleAndroidProject = false;

			Debug.Log("[UnityBuildLog:] Build  Addressables Bundles");
			EditorUtility.DisplayProgressBar("Build Addressable", $"{buildTarget}", 0);
			BuildAddressableDebug();

			Debug.Log("[UnityBuildLog:] BuildPlayer ");
			//打包player
			var buildPath = GetArg("-buildPath");
			BuildPlayer(buildTarget, buildPath);
			EditorUtility.ClearProgressBar();
		}
		#endregion
		public static void BuildAddressableDebug()
		{
			Debug.Log("[UnityBuildLog:] Auto Generate Addressable Group");
			AutoGroupAssets();
			Debug.Log("[UnityBuildLog:] Build Addressable Assets=");
			AddressableBuildScript.BuildAddressablesDebug();
		}

		/// <summary>
		/// int 下开启调试模式
		/// </summary>
		/// <param name="isOpen"></param>
		private static void SetDevelopmentMode(bool isOpen)
		{
			UnityEditor.EditorUserBuildSettings.development = isOpen;
			EditorUserBuildSettings.connectProfiler = isOpen;
			EditorUserBuildSettings.buildWithDeepProfilingSupport = isOpen;
			EditorUserBuildSettings.allowDebugging = isOpen;
		}

		public static bool CheckScenesInBuildValid()
		{
			foreach (var scene in EditorBuildSettings.scenes)
			{
				if (!File.Exists(scene.path))
				{
					Debug.LogError("Error! Scene In BuildList中有场景丢失！请检查！");
					return false;
				}
			}

			return true;
		}

		private static void Copy(string filename, string from, string destinationDir)
		{
			// var from = Settings.GetBuildPath(filename);
			if (File.Exists(from))
			{
				var dest = $"{destinationDir}/{filename}";
				File.Copy(from, dest, true);
			}
			else
			{
				Debug.LogErrorFormat("File not found: {0}", from);
			}
		}
		
		public static void BuildPlayerAll(BuildTarget buildTarget)
		{
			//检查BuildSceneList
			if (!CheckScenesInBuildValid())
			{
				return;
			}

			Debug.Log("[UnityBuildLog:] =Generate Xlua Wrap Script=");
			CSObjectWrapEditor.Generator.GenAll();

			AssetDatabase.Refresh();

			Debug.Log("[UnityBuildLog:] =Build Lua Script=");
			FileFormatUtils.ConvertLuaFileFormatAndEncode();
			PlayerBuild.PackLuaZipAndCopy();

			//0.根据buildTarget区分BuildGroup
			BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup(buildTarget);
			if (BuildTargetGroup.Unknown == buildTargetGroup)
			{
				throw new System.Exception(
					string.Format("{0} is Unknown Build Platform ! Build Failture!", buildTarget));
			}

			Debug.Log("[UnityBuildLog:] =BuildPlayer-=");
			//打包player
			var buildPath = GetArg("-buildPath");
			BuildPlayer(buildTarget, buildPath);
		}

		public static void BuildPlayer(BuildTarget buildTarget, string path)
		{
			var levels = new List<string>();
			foreach (var scene in EditorBuildSettings.scenes)
				if (scene.enabled)
					levels.Add(scene.path);

			bool islocal = false;
			if (levels.Count == 0)
			{
				Debug.Log("Nothing to build.");
				return;
			}

			var buildTargetName = GetBuildTargetName(buildTarget);
			Debug.Log("buildTargetName is " + buildTargetName);
			if (buildTargetName == null) return;

			if (string.IsNullOrEmpty(path))
			{
				path = Path.Combine(Environment.CurrentDirectory, "Builds") + buildTargetName;
				Debug.Log("Output path is " + path);
				islocal = true;
			}

			var buildPlayerOptions = new BuildPlayerOptions
			{
				scenes = levels.ToArray(),
				locationPathName = path,
				target = buildTarget,
				options = EditorUserBuildSettings.development
					? BuildOptions.Development
					: BuildOptions.None
			};
			Debug.LogWarning($"[UnityBuildLog:] {buildPlayerOptions.options}");

			var pipeline = GraphicsSettings.defaultRenderPipeline;
			var qualityPipeline = QualitySettings.renderPipeline;
			QualitySettings.renderPipeline = null;
			GraphicsSettings.defaultRenderPipeline = null;
			BuildPipeline.BuildPlayer(buildPlayerOptions);
			EditorUtility.ClearProgressBar();
			GraphicsSettings.defaultRenderPipeline = pipeline;
			QualitySettings.renderPipeline = qualityPipeline;
			if (islocal)
			{
				EditorUtility.OpenWithDefaultApp(Path.Combine(Environment.CurrentDirectory, "Builds"));
			}
		}

		private static string GetBuildTargetName(BuildTarget target)
		{
			var productName = "XLuaTestUnity" + "-v" + UnityEditor.PlayerSettings.bundleVersion + "-";
			var targetName = $"/{productName}{GetGitBranchName()}{GetTimeForNow()}";
			switch (target)
			{
				case BuildTarget.Android:
					return "/Android" + targetName + ".apk";
				case BuildTarget.StandaloneWindows:
				case BuildTarget.StandaloneWindows64:
					return "/Windows" + targetName + targetName + ".exe";
				case BuildTarget.StandaloneOSX:
					return targetName + ".app";
				default:
					return targetName;
			}
		}

		private static string GetTimeForNow()
		{
			return DateTime.Now.ToString("yyyyMMdd-HHmmss");
		}

		private static string GetGitBranchName()
		{
			// Start the child process.
			System.Diagnostics.Process p = new System.Diagnostics.Process();
			// Redirect the output stream of the child process.
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.RedirectStandardOutput = true;
			p.StartInfo.FileName = "git";
			p.StartInfo.Arguments = "branch --show-current";
			p.StartInfo.WorkingDirectory = ".";
			p.Start();
			// Do not wait for the child process to exit before
			// reading to the end of its redirected stream.
			// p.WaitForExit();
			// Read the output stream first and then wait.
			string output = p.StandardOutput.ReadToEnd();
			p.WaitForExit();

			return output.Replace("\n", "").Replace("\r", "").Replace("/", "-");
		}

		#region 自动检查生成Group

		public static List<string> autoCheckFolderList = new List<string>()
		{
			"Assets/Res",
			"Assets/Shader",
			"Assets/Scenes",
			"Assets/TextMesh Pro",
		};

		[MenuItem("Tools/Asset/AddressableAutoGroup")]
		private static void AutoGroupAssets()
		{
			AddressableImporter.FolderImporter.ReimportFolders(autoCheckFolderList);

			var settings =
				AssetDatabase.LoadAssetAtPath<AddressableAssetSettings>(
					"Assets/AddressableAssetsData/AddressableAssetSettings.asset");
			var bundleDupeDependenciesRule = new CheckBundleDupeDependencies();
			bundleDupeDependenciesRule.GroupName = "Duplicate Asset Isolation";
			bundleDupeDependenciesRule.FixIssues(settings);
		}

		#endregion
	}
}