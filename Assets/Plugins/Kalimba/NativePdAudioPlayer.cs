using UnityEngine;
using System.Collections;
using LibPDBinding;
using System.Runtime.InteropServices;
using System;

// 
public class NativePdAudioPlayer : MonoBehaviour {

	/**
	 * based on usfxr - https://github.com/zeh/usfxr/blob/master/source/SfxrAudioPlayer.cs
	 *
	 * Licensed under the Apache License, Version 2.0 (the "License");
	 * you may not use this file except in compliance with the License.
	 * You may obtain a copy of the License at
	 *
	 * 	http://www.apache.org/licenses/LICENSE-2.0
	 *
	 * Unless required by applicable law or agreed to in writing, software
	 * distributed under the License is distributed on an "AS IS" BASIS,
	 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	 * See the License for the specific language governing permissions and
	 * limitations under the License.
	 *
	 */

	// Properties
	private bool		isDestroyed = false;		// If true, this instance has been destroyed and shouldn't do anything yes
	private bool		needsToDestroy = false;		// If true, it has been scheduled for destruction (from outside the main thread)

	private GCHandle dataHandle;
	private IntPtr dataPtr;

	private GCHandle bufferHandle;
	private IntPtr bufferPtr;
	private float[] buffer;

	private GCHandle zeroHandle;
	private IntPtr zeroPtr;
	private float[] zero;

	private bool islibpdready;

	public int Channels = 2;

	public float Gain = 1f;

	private int pdBlocksize;

	//private MinMaxAvgCollector minMax = new MinMaxAvgCollector();

	// ================================================================================================================
	// INTERNAL INTERFACE ---------------------------------------------------------------------------------------------
	
	void Start() {
		// Creates an empty audio source so this GameObject can receive audio events
		AudioSource soundSource = gameObject.AddComponent<AudioSource>();

		soundSource.clip = new AudioClip();
		soundSource.volume = 1f;
		soundSource.pitch = 1f;
		soundSource.priority = 128;

		Setup();
	}

	void OnDestroy() {
		Teardown();
	}

	void Update () {
		// Destroys self in case it has been queued for deletion
		if (needsToDestroy) {
			needsToDestroy = false;
			Destroy();
		}
	}

	void OnAudioFilterRead(float[] data, int channels) {
		// Requets that sfxrSynth generates the needed audio data

		if (!isDestroyed && !needsToDestroy) {
			bool hasMoreSamples = GenerateAudioFilterData(data, channels);

			//minMax.Reset();
			//for (int i = 0; i < data.Length; ++i) minMax.PutInNumber(data[i]);
			//Debug.Log(minMax);

			// If no more samples are needed, there's no more need for this GameObject so schedule a destruction (cannot do this in this thread)
			if (!hasMoreSamples) needsToDestroy = true;
		}
  	}


	/**
	 * If there is a cached sound to play, reads out of the data.
	 * If there isn't, synthesises new chunch of data, caching it as it goes.
	 * @param	data		Float[] to write data to
	 * @param	channels	Number of channels used
	 * @return	Whether it needs to continue (there are samples left) or not
	 */
	public bool GenerateAudioFilterData(float[] data, int channels) {
		//Debug.Log(string.Format("process {0} floats in {1} channels", data.Length, channels));

		if(dataPtr == IntPtr.Zero)
		{
			dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
			dataPtr = dataHandle.AddrOfPinnedObject();
		}

		if(bufferPtr == IntPtr.Zero)
		{
			buffer = new float[channels * pdBlocksize];
			bufferHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
			bufferPtr = bufferHandle.AddrOfPinnedObject();
		}

		if(zeroPtr == IntPtr.Zero)
		{
			zero = new float[channels * pdBlocksize];
			zeroHandle = GCHandle.Alloc(zero, GCHandleType.Pinned);
			zeroPtr = zeroHandle.AddrOfPinnedObject();
			for(int i = 0; i < zero.Length; ++i) zero[i] = 0f;
		}
		
		if (channels != Channels) {
			Debug.LogWarning("number of pd output channels does not match unity");
		}

		if (islibpdready) {
			// buffersize = channels * ticks * pdBlocksize
			int ticks = data.Length / channels / pdBlocksize;

			//Debug.Log(string.Format("ticks={0} data={1} buffer={3} check={2}", 
			  //        	ticks, data.Length, channels * ticks * pdBlocksize, buffer.Length));

			int p = 0;

			for (int i = 0; i < ticks; ++i) {
				//System.Text.StringBuilder b = new System.Text.StringBuilder();

				LibPD.Process(1, zeroPtr, bufferPtr);

				for (int j = 0; j < buffer.Length; ++j) {
					buffer[j] = buffer[j] * Gain;
				}

				Array.Copy(buffer, 0, data, p, buffer.Length);
				p += buffer.Length;

				/*
				for (int j = 0; j < buffer.Length; ++j) {
					b.Append(string.Format("{0:0.000} ", buffer[j]));
				}

				Debug.Log(b.ToString());
				*/
			}
		}

		bool endOfSamples = false;		

		
		return !endOfSamples;
	}

	// ================================================================================================================
	// PUBLIC INTERFACE -----------------------------------------------------------------------------------------------

	public void Destroy() {
		// Stops audio immediately and destroys self
		if (!isDestroyed) {
			isDestroyed = true;
			UnityEngine.Object.Destroy(gameObject);
		}
	}

	void Setup() {
		Debug.Log("PD SETUP");

		LibPD.Print += Receive;
		LibPD.Message += Message;
		LibPD.Bang += Bang;
		LibPD.Float += Float;

		// we can also work in stereo or more: LibPD.OpenAudio (2, 2, 48000);
		if(!islibpdready)
		{
			LibPD.OpenAudio (1, Channels, AudioSettings.outputSampleRate);
			pdBlocksize = LibPD.BlockSize;
			Debug.Log(string.Format("init pd with {0} channels at {1} freq {2} block", Channels, AudioSettings.outputSampleRate, pdBlocksize));
		}
		LibPD.ComputeAudio (true);
		islibpdready = true;

		LibPD.Subscribe("mainInst");
		LibPD.Subscribe("sec");

		LibPD.AddToSearchPath(Application.streamingAssetsPath);
	}

	void OnGUI() {
		GUILayout.Space(300f);

		Gain = GUILayout.HorizontalSlider(Gain, 0f, 10f);
		if (GUILayout.Button("(   +   )")) Gain += 0.5f;
		if (GUILayout.Button("(   -   )")) Gain -= 0.5f;
	}

	void Float (string recv, float x)
	{
		Debug.Log(string.Format("float {0} {1}", recv, x));
	}

	void Bang (string recv)
	{
		Debug.Log(recv);
	}

	void Message (string recv, string msg, object[] args)
	{
		Debug.Log(string.Format("recv {0} msg {1} args {2}", recv, msg, args));
	}

	void Teardown() {
		Debug.Log("PD TEARDOWN");

		LibPD.Unsubscribe("mainInst");
		LibPD.Unsubscribe("sec");

		LibPD.Print -= Receive;
		LibPD.Message -= Message;
		LibPD.Bang -= Bang;
		LibPD.Release ();

		dataHandle.Free();
		dataPtr = IntPtr.Zero;

		bufferHandle.Free();
		bufferPtr = IntPtr.Zero;

		zeroHandle.Free();
		zeroPtr = IntPtr.Zero;
	}

	// delegate for [print]
	void Receive(string msg) 
	{
		Debug.Log("print:" + msg);
	}
}
