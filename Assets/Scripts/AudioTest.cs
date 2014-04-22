using UnityEngine;
using System.Collections;
using System.IO;
using System.Text;
using LibPDBinding;

public class AudioTest : MonoBehaviour
{
	public string PatchName = "test.pd";

	public RuntimePlatform ForcedPlatform;
	public bool ForcePlatform;

	void Start ()
	{
		if (ForcePlatform) KalimbaPd.Init(ForcedPlatform);
		else KalimbaPd.Init();

		KalimbaPd.OpenFile(PatchName);
	}

	void OnGUI ()
	{
		if (GUI.Button (new Rect (10, 10 + 0*60, 100, 50), "sine_on")) 
		{
			KalimbaPd.SendBangToReceiver("sine_on");
		}
		
		if (GUI.Button (new Rect (10, 10 + 1*60, 100, 50), "sine_off")) 
		{
			KalimbaPd.SendBangToReceiver("sine_off");
		}

		if (GUI.Button (new Rect (10, 10 + 2*60, 100, 50), "oggtest")) 
		{
			KalimbaPd.SendBangToReceiver("startogg");
		}

		if (GUI.Button (new Rect (10, 10 + 3*60, 100, 50), "oggload")) 
		{
			KalimbaPd.SendBangToReceiver("loadogg");
		}
	}
}
