using UnityEngine;
using System.IO;
using LibPDBinding;

public class KalimbaPdImplNative : KalimbaPdImplAbstract
{
	public NativePdAudioPlayer Player = null;

	public override void CloseFile(int patchId) {
		LibPD.ClosePatch(patchId);
	}

	public override int OpenFile(string baseName, string pathName) {
		string p = Path.Combine(Application.streamingAssetsPath + "/", pathName);
		return LibPD.OpenPatch(Path.Combine(p, baseName));
	}

	public override void SendBangToReceiver(string receiverName) {
		LibPD.SendBang(receiverName);
	}

	public override void SendFloat(float val, string receiverName) {
		LibPD.SendFloat(receiverName, val);
	}

	public override void SendSymbol(string symbol, string receiverName) {
		LibPD.SendSymbol(receiverName, symbol);
	}

	public override void Init() {
		if (Player == null) {
			GameObject o = new GameObject("PD");
			GameObject.DontDestroyOnLoad(o);
			Player = o.AddComponent<NativePdAudioPlayer>();
		}
	}

	public override void PollForMessages() {
		// TODO implement me
	}

}
