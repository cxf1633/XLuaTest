using System;
using UnityEngine;
public class TimeLineEvent : MonoBehaviour
{
	public void PostEvent(string eventName)
	{
		Debug.Log("PostEvent eventName = " + eventName);
	}
}
