using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
// using Newtonsoft.Json;

public class AddressableExterns
{
	public static Dictionary<string, string> _aaMap;
	private static string _mapFilePath = Application.dataPath + "/AddressableAssetsData/aaMap.json";
	
	[UnityEditor.InitializeOnLoadMethod]
	public static void InitFunc()
	{
		Debug.Log("init aaMap");
		_mapFilePath = Application.dataPath + "/AddressableAssetsData/aaMap.json";
		_aaMap = new Dictionary<string, string>();
		LoadMap();
	}
	
	public static void ResetMap()
	{
		Debug.Log("Reset aaMap");
		if(_aaMap == null)
			_aaMap = new Dictionary<string, string>();
		_aaMap.Clear();
	}

	private static void AddOrUpdate(string key,string value)
	{
		if(!_aaMap.ContainsKey(key))
			_aaMap.Add(key,value);
		else
		{
			Debug.Log($"_dic contains same key is {key} old value is {_aaMap[key]} new value is {value}");
		}
	}
	public static void UpdateBundleMap(string source,string output,string GroupName)
	{
		var len = source.Length;
		var name = source.Substring(7,len-14);
		if (name == "all")
		{
			Debug.Log($"originName {source} bundleResultInfo.SourceAssetGroup {GroupName}");
			name = "Group_" + GroupName;
		}
		AddOrUpdate(name,output);
	}

	public static string GetBundleName(string key)
	{
		if (_aaMap == null)
		{
			Debug.LogError("_aaMap is null,please Rebuilt aa");
			return "";
		}

		if (_aaMap.ContainsKey(key))
			return _aaMap[key];
		else
		{
			//Debug.LogWarning($"_aaMap is not contains key {key},please Rebuilt aa");
			return "";
		}
	}

	public static void ClearAll()
	{
		_aaMap.Clear();
	}

	public static void SaveFile()
	{
		// string result = JsonConvert.SerializeObject(_aaMap);
		// File.WriteAllText(_mapFilePath,result);
	}
	public static bool LoadMap()
	{
		if (!File.Exists(_mapFilePath))
		{
			Debug.LogWarning("_mapFilePath is not exists,please Rebuilt aa");
			return false;
		}
		// string json =File.ReadAllText(_mapFilePath);
		// _aaMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
		return true;
	}
}