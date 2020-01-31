using UnityEngine;
using UnityEngine.UI;

public class PopupText : MonoBehaviour {

  public float lifetime;
  public float distance;
  public Text notificationText;

  private float timer;
  private Color fade;
  private int direction = 1;

  private void Update() {
    timer += Time.deltaTime;
    if (timer >= lifetime) {
      fade = notificationText.color;
      fade.a = fade.a /1.1f;
      notificationText.color = fade;

      if(fade.a <=.1) {
        Destroy (gameObject);
      }
    }
    transform.position += new Vector3(0f, direction * distance * Time.deltaTime, 0f);
  }

  public void DisplayMessage(string message, float duration, bool shouldMove = true) {
    Debug.Log("Popup pos: " + transform.position);
    if (shouldMove) {
      distance *= (1 / duration);
    } else {
      transform.position = new Vector3(transform.position.x, transform.position.y * 1.25f, 1f);
      distance = 0f;
    }
    lifetime = duration;
    notificationText.text = message;
  }
}