using System;
using UnityEngine;

public class Observer : MonoBehaviour {
  void OnEnable () {
    this.AddObserver(OnNotification, "TEST_NOTIFICATION");
  }
  
  void OnDisable () {
    this.RemoveObserver(OnNotification, "TEST_NOTIFICATION");
  }

  void OnNotification (object sender, EventArgs e) {
    Debug.Log("Got it! " + ((MessageEventArgs)e).message);
  }
}
