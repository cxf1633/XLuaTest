﻿#if !UNITY_EDITOR
#define LOAD_FROM_PACK
using System.Collections.Generic;
using ICSharpCode.SharpZipLib.Zip;
#endif

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
// using Extend.Asset;
// using DG.Tweening;
// using Extend.Asset;
using Extend.Common;
using Extend.LuaUtil;
// using Extend.LuaMVVM;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
// using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using XLua;
using XLua.LuaDLL;
using Debug = UnityEngine.Debug;

namespace Extend {
	[CSharpCallLua]
	public delegate void SendCSharpMessage(string message, PointerEventData eventData);
	public class LuaVM : IService, IServiceUpdate, IDisposable {
#if LOAD_FROM_PACK
		private static readonly string LUA_DEBUG_DIRECTORY = Application.persistentDataPath + "/Lua/";
		private static readonly Dictionary<string, byte[]> m_unzipLuaFiles = new Dictionary<string, byte[]>(2048);
#endif
		private LuaFunction OnDestroy;
		private LuaFunction OnInit;
		private LuaFunction OnClearCache;
		private static LuaEnv Default { get; set; }
		public LuaTable Global => Default.Global;
		public LuaTable DestroyedTableMeta { private set; get; }
		public SendCSharpMessage SendCSharpMessage { get; private set; }

		public static Action OnPreRequestLoaded;

		public static readonly Type[] ExportToLua = {
			typeof(Stopwatch),
			typeof(TextMeshProUGUI),
			typeof(TextMeshPro),
			typeof(TMP_InputField),
			// typeof(Tweener),
			typeof(EventSystem),
			// typeof(Volume),
			typeof(LayerMask),
			typeof(RaycastHit),
			typeof(UnityEventBase),
			typeof(UnityEvent),
			typeof(Button.ButtonClickedEvent),
			// typeof(CameraExtensions),
			typeof(EventSystem),
			typeof(PointerEventData),
			// typeof(UniversalAdditionalCameraData)
		};

#if LUA_WRAP_CHECK && UNITY_EDITOR
		static LuaVM() {
			ObjectTranslator.callbackWhenLoadType = type => {
				if( !Application.isPlaying ) {
					return;
				}
				if( type.Namespace != null && type.Namespace.StartsWith("UnityEngine") ) {
					return;
				}

				if( type.GetCustomAttribute(typeof(LuaCallCSharpAttribute)) != null ||
				    type.IsSubclassOf(typeof(Delegate)) ||
				    type.GetCustomAttribute(typeof(BlackListAttribute)) != null ) {
					return;
				}

				if( type.FullName == "System.RuntimeType" ) {
					return;
				}

				if( Array.IndexOf(ExportToLua, type) != -1 ) {
					return;
				}

				Debug.LogWarning($"Type : {type} not wrap!\n" + LogCallStack());
			};
		}
#endif

		public LuaTable NewTable() {
			return Default.NewTable();
		}

		public long Memory => Default.Memory;
		public int LuaMapCount => Default.translator.objects.Count;

		public object[] LoadFileAtPath(string luaFileName) {
#if UNITY_EDITOR
			if( luaFileName.Contains("/") || luaFileName.Contains("\\") ) {
				Debug.LogError("Use . as a path separator : " + luaFileName);
				return null;
			}
#endif
			var ret = Default.DoString($"return require '{luaFileName}'");
			return ret;
		}

		public object[] DoString(string code, string chunkName = "chuck", LuaTable env = null) {
			return Default.DoString(code, chunkName, env);
		}

		public object[] DoBindingString(string code, string chunkName = "chuck") {
			return Default.DoString(code, chunkName, m_bindingEnv);
		}

		public int ServiceType => (int) CSharpServiceManager.ServiceType.LUA_SERVICE;
		private SetupLuaNewClassCallback m_newClassCallback;
		
		
		[CSharpCallLua]
		public delegate byte[] HotFixReloadDelegate(string moduleName);

		private HotFixReloadDelegate m_hotFixReload;
		

		private void OnLuaNewClass(LuaTable classMeta, LuaTable parentClassMeta) {
			LuaClassCache.Register(classMeta, parentClassMeta);
		}

		public static LuaClassCache LuaClassCache { get; private set; }
		public static Action OnVMCreated;
		public static Action OnVMQuiting;
		public static bool LoadFromPersistent { get; set; }
		private LuaTable m_bindingEnv;

		/// <summary>
		/// 在这里设置Lua文件的总入口
		/// </summary>
		/// <param name="filename"></param>
		/// <param name="extension"></param>
		/// <returns></returns>
		private static byte[] LoadFile(ref string filename, string extension) {
#if !LOAD_FROM_PACK
			filename = filename.Replace('.', '/') + extension;
			var path = $"{Application.dataPath}/../LuaScripts/{filename}";
			if( File.Exists(path) ) {
				var text = File.ReadAllText(path);
				return Encoding.UTF8.GetBytes(text);
			}

			return null;
#else
			filename = filename.Replace('.', '/') + extension;
			var hotfix = $"{LUA_DEBUG_DIRECTORY}{filename}";
			if( File.Exists(hotfix) ) {
				Debug.LogWarning("HOTFIX FILE : " + hotfix);
				filename += extension;
				return File.ReadAllBytes(hotfix);
			}

			var zipName = filename;
			if( m_unzipLuaFiles.TryGetValue(zipName, out var code) ) {
				m_unzipLuaFiles.Remove(zipName);
				return code;
			}

			Debug.LogWarning($"{zipName} not found in packed lua.");
			return null;
#endif
		}

		public void Initialize() {
#if LOAD_FROM_PACK
			Debug.LogWarning("Load from packed lua."+LoadFromPersistent);
			using( var stream = FileLoader.LoadFileSync("BuiltInData") )
			using( var zipFile = new ZipFile(stream) ) {
				zipFile.Password = Application.productName;
				var buffer = new byte[1024];
				foreach( ZipEntry zipEntry in zipFile ) {
					if( !zipEntry.IsFile ) {
						continue;
					}

					using( var zipStream = zipFile.GetInputStream(zipEntry) )
					using( var outputStream = new MemoryStream() ) {
						int count;
						do {
							count = zipStream.Read(buffer, 0, 1024);
							if( count > 0 ) {
								outputStream.Write(buffer, 0, count);
							}
						} while( count > 0 );
						if(!m_unzipLuaFiles.ContainsKey(zipEntry.Name))
							m_unzipLuaFiles.Add(zipEntry.Name, outputStream.ToArray());
						else
						{
							m_unzipLuaFiles[zipEntry.Name] = outputStream.ToArray();
						}
					}
				}
			}
#endif

			if( Default == null ) {
				Default = new LuaEnv();
				// Default.AddBuildin("cjson", Lua.LoadLuaCJson);
				// Default.AddBuildin("chronos", Lua.LoadChronos);
#if EMMY_CORE_SUPPORT
				Default.AddBuildin("emmy_core", Lua.LoadEmmyCore);
#endif
				// Default.AddBuildin("lpeg", Lua.LoadLpeg);
				// Default.AddBuildin("sproto.core", Lua.LoadSprotoCore);
				// Default.AddBuildin("luv", Lua.LoadLUV);
				// Default.AddBuildin("lsqlite", Lua.LoadLSqlite3);

				Lua.OverrideLogFunction(Default.rawL);
				LuaClassCache = new LuaClassCache();
#if UNITY_EDITOR
				Default.AddLoader((ref string filename) => LoadFile(ref filename, ".lua"));
#else
				Default.AddLoader((ref string filename) => LoadFile(ref filename, ".lua"));
#endif
			}

			m_newClassCallback = OnLuaNewClass;

			var setupNewCallback = LoadFileAtPath("base.class")[0] as LuaFunction;
			setupNewCallback.Action(m_newClassCallback);
			OnVMCreated?.Invoke();

			var ret = LoadFileAtPath("PreRequest")[0] as LuaTable;
			OnInit = ret.Get<LuaFunction>("init");
			OnDestroy = ret.Get<LuaFunction>("shutdown");
			OnClearCache = ret.Get<LuaFunction>("clearCache");

			OnPreRequestLoaded?.Invoke();

			// LoadFileAtPath("base.LuaBindingBase");
#if UNITY_EDITOR
			m_hotFixReload = moduleName => {
				moduleName = moduleName.Replace('.', '/') + ".lua";
				var path = $"{Application.dataPath}/../LuaScripts/{moduleName}";
				if( File.Exists(path) ) {
					var text = File.ReadAllText(path);
					return Encoding.UTF8.GetBytes(text);
				}

				return null;
			};
			Global.Set("hotfix_func", m_hotFixReload);
#endif
			// AssetService.Get().AddAfterDestroy(this);
			DestroyedTableMeta = Global.Get<LuaTable>("DestroyedTableMeta");
		}


		public void Restart() {
			Destroy();
			Initialize();
			StartUp();
		}

		public void StartUp() {
			OnInit.Action<Func<string, byte[]>>(filename => LoadFile(ref filename, ".lua"));

			SendCSharpMessage = Default.Global.GetInPath<SendCSharpMessage>("_CSMessageService.OnMessage");
			m_bindingEnv = Default.Global.GetInPath<LuaTable>("_BindingEnv");
		}

		private GetGlobalVM m_getGlobalVMFunc;

		public object GetGlobalVM(string path) {
			if( m_getGlobalVMFunc == null ) {
				var getLuaService = Default.Global.GetInPath<GetLuaService>("_ServiceManager.GetService");
				var globalVMTable = getLuaService(5);
				m_getGlobalVMFunc = globalVMTable.GetInPath<GetGlobalVM>("GetVM");
			}

			return m_getGlobalVMFunc(path);
		}

		public LuaClassCache.LuaClass GetLuaClass(LuaTable klass) {
			return LuaClassCache.TryGetClass(klass);
		}

		public LuaClassCache.LuaClass GetLuaClass(string klass) {
			var ret = LoadFileAtPath(klass);
			return ret[0] is LuaTable c ? LuaClassCache.TryGetClass(c) : null;
		}

		public static string LogCallStack() {
			var msg = Default.Global.GetInPath<Func<string>>("debug.traceback");
			var str = "lua stack : " + msg.Invoke();
			return str;
		}

		public void Destroy() {
			try {
				OnDestroy.Call();
			}
			catch( Exception e ) {
				Debug.LogException(e);
			}
			OnClearCache.Call();
			// TempBindingExpressCache.Clear();
			OnVMQuiting?.Invoke();

			m_newClassCallback = null;
			OnInit = null;
			OnDestroy = null;
			OnVMCreated = null;
			OnVMQuiting = null;
			GC.Collect();
		}

		public void Update() {
			Default.Tick();
		}

		public void Dispose() {
			// Default.Dispose();
		}
	}
}