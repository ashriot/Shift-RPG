using UnityEngine;

public class Poster : MonoBehaviour {
  // 
  
  private void Start() {
    this.PostNotification(Notifications.TEST_NOTIFICATION, new MessageEventArgs("Hello world!"));
  }
}
