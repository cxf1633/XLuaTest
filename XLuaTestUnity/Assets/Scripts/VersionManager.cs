using System;
using System.Collections;
using System.Collections.Generic;
// using DigitalRubyShared;
// using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
// using XiaoIceland;
// using XiaoIceland.Service;
// using XiaoIceland.Update;
// using XiaoIceland.Update.IEnumeratorOperations;
using CC;

namespace XiaoIceIsland.Update
{
	public enum UpdateStatus
	{
		None,
		StartUpdate,
		ConnectFailed
	}

	public class VersionManager : MonoBehaviour
	{
#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		private static void InitUpdater()
		{
			_openVersion = UnityEditor.EditorPrefs.GetBool("openVersion", false);
			UnityEditor.Menu.SetChecked("Tools/Updater/Open Version", _openVersion);
		}

		private static bool _openVersion = false;

		[UnityEditor.MenuItem("Tools/Updater/Open Version")]
		private static void OpenVersion()
		{
			_openVersion = !_openVersion;
			UnityEditor.Menu.SetChecked("Tools/Updater/Open Version", _openVersion);
			UnityEditor.EditorPrefs.SetBool("openVersion", _openVersion);
		}
#endif

		// public GameObject notWifiWindow;
		// public GameObject downloadFailWindow;
		// public GameObject connectFaileWindow;
		//
		// public Text statusText;
		// private Text sizeInfoText;
		// private GameObject _currentPopUpWindow;
		// private GameObject _eventSystemObj;
		//
		// private static readonly List<AsyncOperationBase> _operations = new List<AsyncOperationBase>(10);
		//
		// private float ConnectFailedTimeOut = 2;
		// private float _currentConnectTime = 0;
		// private bool _isConnectRetry = false;
		//
		// private UpdateStatus _status;
		//
		private VersionService _versionService;
		// private UpdaterOperation[] _allOperations;
		// private Coroutine _checkVersionCoroutine;
		// private UpdaterOperation _currentOperation;
		private bool _isPauseCoroutine = false;

		private void Start()
		{
			CloseAllWindow();
#if !UNITY_EDITOR && XIAOICE_NATIVE
			NativeManager.UpdateInfo += UpdateInfo;
			NativeManager.OnLanding += OnLanding;
			NativeManager.UnLanding += UnLanding;
#endif
			if (VersionService.Instance != null)
			{
				_versionService = VersionService.Instance;
				_versionService.ReStartService();
			}
			else
			{
				// GameObject nm = new GameObject("SceneProvider");
				// nm.AddComponent<NativeManager>();
				// var fingersScript = nm.AddComponent<FingersScript>();
				// fingersScript.LevelUnloadOption = FingersScript.GestureLevelUnloadOption.Nothing;
				_versionService = new VersionService();
			}
			_versionService.Initialize();
			// sizeInfoText = notWifiWindow.transform.Find("SizeInfo").GetComponent<Text>();
			//
			// _eventSystemObj = transform.Find("EventSystem").gameObject;
			// if (statusText)
			// {
			// 	statusText.gameObject.SetActive(true);
			// 	statusText.text = "";
			// }
			//
			// _status = UpdateStatus.None;
			//
			// _allOperations = new UpdaterOperation[]
			// {
			// 	new BeginnerGuideOperation(_versionService),
			// 	new CheckVersionOperation(_versionService),
			// 	new GetMoonSizeOperation(_versionService),
			// 	new GetResSizeOperation(_versionService),
			// 	new NoWifiOperation(_versionService),
			// 	new UMoonOperation(_versionService),
			// 	new ResUpdateOperation(_versionService),
			// };
			// NativeManager.SendToNative("SceneLoadComplete", null);
#if UNITY_EDITOR
			_versionService.AccountId = "";
			// if(_openVersion)
			// 	CheckVersion();
			// else
			// {
				EnterGame("Editor");
			// }
#elif FULL_PACKAGE && !XIAOICE_NATIVE
		StartCoroutine(EnterFullpackage());
#elif !XIAOICE_NATIVE
//For Test without Native
			OnLanding();
#endif
		}

		#region CheckUpdate

		private void CheckVersion()
		{
#if FULL_PACKAGE
StartCoroutine(EnterFullpackage());
return;
#endif
			// if (_status == UpdateStatus.StartUpdate)
			// {
			// 	Debug.LogWarning("already is in CheckVersion");
			// 	return;
			// }
			// _isPauseCoroutine = false;
			// Debug.LogWarning("Start CheckVersion");
			// if (_checkVersionCoroutine != null)
			// 	StopCoroutine(_checkVersionCoroutine);
			// _checkVersionCoroutine = StartCoroutine(CheckVersionCoroutine());
		}

		// private IEnumerator CheckVersionCoroutine()
		// {
		// 	_status = UpdateStatus.StartUpdate;
		// 	foreach (var operation in _allOperations)
		// 	{
		// 		_currentOperation = operation;
		// 		Debug.LogWarning("Enter hotfix operation : " + _currentOperation);
		// 		if (HasNet())
		// 		{
		// 			if (operation.Status == EUpdateOperationStatus.NeedWifi)
		// 			{
		// 				yield return operation.CheckWifi();
		// 				if (operation.Status == EUpdateOperationStatus.Succeed)
		// 				{
		// 					continue;
		// 				}
		// 	
		// 				if (operation.Status == EUpdateOperationStatus.NoWifi)
		// 				{
		// 					OpenNoWifi();
		// 					_isPauseCoroutine = true;
		// 					yield return PauseCoroutine();
		// 				}
		// 	
		// 				if (CheckFailed(operation.Status))
		// 					yield break;
		// 			}
		// 	
		// 			yield return operation.Start();
		// 			if (CheckSucceed(operation.Status))
		// 				yield break;
		// 			if (CheckFailed(operation.Status))
		// 				yield break;
		// 		}
		// 		else
		// 		{
		// 			Debug.LogWarning("No Net,Open connectFaileWindow");
		// 			OpenConnectFailed();
		// 			yield break;
		// 		}
		// 	}
		//
		// 	if (CheckAllOperation())
		// 	{
		// 		EnterGame("All Operation Ok");
		// 	}
		// 	else
		// 	{
		// 		OpenDownloadFailed();
		// 	}
		// }
		//
		private IEnumerator EnterFullpackage()
		{
			var init = Addressables.InitializeAsync();
			yield return init;
			EnterGame("FullPackage");
		}
		
		// private bool CheckAllOperation()
		// {
		// 	foreach (var operation in _allOperations)
		// 	{
		// 		if (operation.IsReady)
		// 			continue;
		// 		else
		// 		{
		// 			return false;
		// 		}
		// 	}
		//
		// 	return true;
		// }
		// private void ResetAllOperation()
		// {
		// 	if (_checkVersionCoroutine != null)
		// 		StopCoroutine(_checkVersionCoroutine);
		// 	foreach (var operation in _allOperations)
		// 	{
		// 		operation.Reset();
		// 	}
		// 	_currentOperation = null;
		// }
		//
		// private bool CheckFailed(EUpdateOperationStatus status)
		// {
		// 	if (status == EUpdateOperationStatus.ConnectFailed)
		// 	{
		// 		OpenConnectFailed();
		// 		return true;
		// 	}
		// 	else if (status == EUpdateOperationStatus.DownloadFailed)
		// 	{
		// 		OpenDownloadFailed();
		// 		return true;
		// 	}
		//
		// 	return false;
		// }

		// private bool CheckSucceed(EUpdateOperationStatus status)
		// {
		// 	if (status == EUpdateOperationStatus.EnterGame)
		// 	{
		// 		EnterGame("Step EnterGame");
		// 		return true;
		// 	}
		//
		// 	return false;
		// }

		// private IEnumerator PauseCoroutine()
		// {
		// 	while (_isPauseCoroutine)
		// 	{
		// 		yield return null;
		// 	}
		// }
		//
		// public static void StartOperation(AsyncOperationBase operationBase)
		// {
		// 	_operations.Add(operationBase);
		// 	operationBase.Start();
		// }

		#endregion

		// private bool HasNet()
		// {
		// 	var status = NativeManager.GetNativeStatus("networkStatus");
		// 	return status == 1 || status == 2;
		// }

		private void EnterGame(string sender)
		{
			if (!_isEnterGame)
			{
				_isEnterGame = true;
				// _versionService.SaveVersion(VersionService.PLATFROM_KEY);
				Debug.LogWarning($"Begin EnterGame  {sender}");
				Dispose();
				// if (_eventSystemObj)
				// 	Destroy(_eventSystemObj);
				_versionService.StartLua();
			}
		}

		private void UpdateInfo(string id)
		{
			_versionService.AccountId = id;
		}
		
		private void OnLanding(string data = "")
		{
			// if (!string.IsNullOrEmpty(data))
			// {
			// 	JObject obj = JObject.Parse(data);
			// 	var name = obj["Meta"]["sceneName"]?.ToString();
			// 	if (!string.IsNullOrEmpty(name) && name == "BeginnerGuide")
			// 	{
			// 		Debug.LogWarning("OnLanding BeginnerGuide");
			// 		// _versionService.isBeginner = true; 暂时取消新手引导不更新
			// 	}
			// }
			// CheckVersion();
		}

		// private void UnLanding()
		// {
		// 	NativeManager.ClearCache();
		// }
		//
		// private void BackToNative()
		// {
		// 	ResetAllOperation();
		// 	Debug.LogWarning("OnQuitClick showChatView");
		// 	NativeManager.ClearCache();
		// 	NativeManager.SendToNative("showChatView", null);
		// }

		#region UI

		public void OpenNoWifi()
		{
			// _currentPopUpWindow = notWifiWindow;
			// if (_currentOperation != null)
			// 	sizeInfoText.text = GetSizeOperation.GetTotalSizeInfo();// UpdaterOperation.TotalDownloadSize //_currentOperation.PopupSizeInfo;
			// notWifiWindow.SetActive(true);
		}

		public void OpenConnectFailed()
		{
			// if (_status == UpdateStatus.ConnectFailed) return;
			// _currentPopUpWindow = connectFaileWindow;
			// connectFaileWindow.SetActive(true);
			// sizeInfoText.text = "";
			// _currentConnectTime = 0;
			// _status = UpdateStatus.ConnectFailed;
		}

		public void OpenDownloadFailed()
		{
			// _currentPopUpWindow = downloadFailWindow;
			// downloadFailWindow.SetActive(true);
			// statusText.text = "下载失败";
		}

		#region ButtonClick

		public void OnStartDownloadClick()
		{
			// CloseWindow();
			// _isPauseCoroutine = false;
		}

		public void OnRetryClick()
		{
			// CloseWindow();
			// _status = UpdateStatus.None;
			// CheckVersion();
			// _isConnectRetry = true;
		}

		public void OnQuitClick()
		{
			// CloseAllWindow();
			// _status = UpdateStatus.None;
			// BackToNative();
		}

		#endregion

		public void CloseAllWindow()
		{
			// notWifiWindow.SetActive(false);
			// connectFaileWindow.SetActive(false);
			// downloadFailWindow.SetActive(false);
		}

		public void CloseWindow()
		{
			// if (_currentPopUpWindow != null)
			// 	_currentPopUpWindow.SetActive(false);
		}

		#endregion

		private bool _isEnterGame = false;

		private void Update()
		{
			// if (_status == UpdateStatus.StartUpdate)
			// {
			// 	if (_currentOperation != null)
			// 	{
			// 		if(Time.frameCount%3 ==0)
			// 			statusText.text = _currentOperation.GetUpdateInfo();
			// 	}
			// }
			//
			// if (_isConnectRetry)
			// {
			// 	_currentConnectTime += Time.deltaTime;
			// 	if (_currentConnectTime > ConnectFailedTimeOut)
			// 	{
			// 		_currentConnectTime = 0;
			// 		_isConnectRetry = false;
			// 		if (_status == UpdateStatus.ConnectFailed)
			// 		{
			// 			Debug.LogWarning("Update Status == UpdateStatus.ConnectFailed");
			// 			OpenConnectFailed();
			// 		}
			// 	}
			// }
			//
			// if (_operations.Count > 0)
			// {
			// 	for (int i = _operations.Count - 1; i >= 0; i--)
			// 	{
			// 		var operation = _operations[i];
			// 		operation.Update();
			// 		if (operation.IsDone)
			// 		{
			// 			_operations.RemoveAt(i);
			// 			operation.Finish();
			// 		}
			// 	}
			// }
		}

		public void Dispose()
		{
			// foreach (var op in _allOperations)
			// {
			// 	op.Release();
			// }
			// _operations.Clear();
			// NativeManager.OnLanding -= OnLanding;
			// NativeManager.UnLanding -= UnLanding;
			// NativeManager.UpdateInfo -= UpdateInfo;
		}

		public void OnApplicationQuit()
		{
			Dispose();
		}

		public void OnDestroy()
		{
			Dispose();
		}
	}
}