using UnityEngine;
using System.Collections;

public class KalimbaPdPoll : MonoBehaviour {

	// Update is called once per frame
	void Update () {
		KalimbaPd.PollForMessages();
	}
}
