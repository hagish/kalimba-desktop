using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;

public static class KalimbaPd {
	private static KalimbaPdImplAbstract impl;
	
	private static void Setup(RuntimePlatform platform)
	{
		if (impl == null)
		{
			switch(platform) {
			case RuntimePlatform.Android: impl = new KalimbaPdImplAndroid(); break;
			case RuntimePlatform.IPhonePlayer: impl = new KalimbaPdImplIOs(); break;
			case RuntimePlatform.WindowsPlayer:
			case RuntimePlatform.LinuxPlayer:
			case RuntimePlatform.OSXPlayer: impl = new KalimbaPdImplNative(); break;
			default: impl = new KalimbaPdImplNetwork(); break;
			}
		}
	}
	
	public static void CloseFile(int patchId)
	{
		if (impl != null)impl.CloseFile(patchId);
	}
	
	public static int OpenFile(string baseName, string pathName = "")
	{
		if (impl != null)return impl.OpenFile(baseName, pathName);
		else return 0;
	}
	
	public static void SendBangToReceiver(string receiverName)
	{
		if (string.IsNullOrEmpty(receiverName)) return;
		if (impl != null)impl.SendBangToReceiver(receiverName);
	}
	
	public static void SendFloat(float val, string receiverName)
	{
		if (string.IsNullOrEmpty(receiverName)) return;
		if (impl != null)impl.SendFloat(val, receiverName);
	}
	
	public static void SendSymbol(string symbol, string receiverName)
	{
		if (string.IsNullOrEmpty(receiverName)) return;
		if (impl != null)impl.SendSymbol(symbol, receiverName);
	}
	
	public static void Init(RuntimePlatform forcedPlatform) 
	{
		Setup(forcedPlatform);
		if (impl != null)impl.Init();
	}

	public static void Init()
	{
		Setup(Application.platform);
		if (impl != null)impl.Init();
	}

	public static void PollForMessages() 
	{
		if (impl != null)impl.PollForMessages();
	}
}
