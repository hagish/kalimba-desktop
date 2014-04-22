using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Net.Sockets;
using System.Threading;

public class KalimbaPdImplNetwork : KalimbaPdImplAbstract, IDisposable
{
	#region thread local variables
	// only used by network thread
    private TcpClient client;
	private Stream stream;
	private ASCIIEncoding asciiEncoding;
	/// <summary>
	/// if true errors wont get printed.
	/// Purpose is to only display connect errors once.
	/// </summary>
	private bool suppressErrors = false;
	#endregion

	private const string host = "127.0.0.1";
	private const int port = 32000;

	private Thread t;

	#region locked variables
	private System.Object thisLock = new System.Object();
	private bool readyToQueuePdMessage;
	private Queue<string> readyToSend = new Queue<string>();
	private bool doShutdown = false;
	private Queue<string> debug = new Queue<string>();
	#endregion

	private bool _disposed;

	public void Dispose() 
	{
		Dispose(true);
		
		// Call SupressFinalize in case a subclass implements a finalizer.
		GC.SuppressFinalize(this);      
	}
	
	protected virtual void Dispose(bool disposing)
	{
		// If you need thread safety, use a lock around these  
		// operations, as well as in your methods that use the resource. 
		if (!_disposed) {
			if (disposing) {
				lock (thisLock) {
					doShutdown = true;
				}
			}
			
			// Indicate that the instance has been disposed.
			_disposed = true;   
		}
	}
	private void NetworkRun() {
		while (!doShutdown) {
			// connection necessary?
			if (client == null || !client.Connected) {
				try {
					client = new TcpClient();
					lock (thisLock) { debug.Enqueue("trying to connect to pd"); }
					client.Connect(host, port);
					
					if (stream != null)stream.Dispose();
					stream = client.GetStream();
					suppressErrors = false;
				}
				catch(Exception e)
				{
					Error("network error: " + e.Message);
					if (stream != null)stream.Dispose();
					stream = null;
					client = null;
					Thread.Sleep (3 * 1000);
				}
			}

			// deliver messages
			try {
				lock (thisLock) {
					readyToQueuePdMessage = client != null && client.Connected;
					while (client.Connected && stream != null && readyToSend.Count > 0) {
						string message = readyToSend.Dequeue();
						// low level mgs send
						byte[] ba = asciiEncoding.GetBytes(message.Trim().TrimEnd(new char[]{';'}).Trim() + ";");
						
						stream.Write(ba, 0, ba.Length);
						suppressErrors = false;
					}
				}
			}
			catch(Exception e)
			{
				Error("network error: " + e.Message);
				if (stream != null)stream.Dispose();
				stream = null;
				client = null;
				Thread.Sleep (3 * 1000);
			}

			// sleep a while
			Thread.Sleep(5);
		}
	}

	public KalimbaPdImplNetwork()
	{
		asciiEncoding = new ASCIIEncoding();

		t = new Thread(NetworkRun);
		t.Start();
	}

	private void Error(string text)
	{
		if (!suppressErrors) {
			lock (thisLock) {
				debug.Enqueue(text);
			}
			suppressErrors = true;
		}
	}
	
	public override void CloseFile(int patchId)
	{
		Debug.LogWarning("closing patch");
	}
	
	public override int OpenFile(string baseName, string pathName)
	{
		Debug.LogWarning("you need to manually open patch " + baseName + " at " + pathName);
		return 1;
	}
	
	// no need adding a closing ;
	private void SendPdMessage(string message)
	{
		lock (thisLock) {
			if (readyToQueuePdMessage) {
				readyToSend.Enqueue(message);
			}
		}
	}
	
	private void ConstructAndSendMessagesToSendMessage(string message)
	{
		SendPdMessage("set;");
		SendPdMessage("addsemi;");
		SendPdMessage("add " + message);
		SendPdMessage("bang;");
	}
	
	public override void SendBangToReceiver(string receiverName)
	{
		ConstructAndSendMessagesToSendMessage(receiverName + " bang");
	}
	
	public override void SendFloat(float val, string receiverName)
	{
		ConstructAndSendMessagesToSendMessage(receiverName + " " + val.ToString());
	}
	
	public override void SendSymbol(string symbol, string receiverName)
	{
		ConstructAndSendMessagesToSendMessage(receiverName + " " + symbol);
	}


	private IEnumerator PrintDebug() {
		while (true) {
			lock (thisLock) {
				while(debug.Count > 0) {
					Debug.Log(debug.Dequeue());
				}
				if (doShutdown) yield break;
			}
			yield return null;
		}
	}

	public override void Init()
	{
		GameObject g = new GameObject("KalimbaPdImplNetwork");
		g.AddComponent<KalimbaPdImplNetworkCoroutineHelper>().StartCoroutine(PrintDebug());		
	}

	public override void PollForMessages() {
		// TODO implement me
	}
}

public class KalimbaPdImplNetworkCoroutineHelper : MonoBehaviour {
	
}
