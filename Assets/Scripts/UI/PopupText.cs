using UnityEngine;
using UnityEngine.UI;

public class PopupText : MonoBehaviour {

  public float lifetime;
  public float initialDistance;
  public Text text;

  private float distance;
  private float timer;
  private Color fade;
  private int direction = 1;

  private void Update() {
    timer += Time.deltaTime;
    if (timer >= lifetime) {
      fade = text.color;
      fade.a = fade.a /1.1f;
      text.color = fade;

      if(fade.a <=.1) {
        Destroy (gameObject);
      }
    }
    transform.localPosition += new Vector3(0f, direction * distance * Time.deltaTime, 0f);
  }

  public void DisplayMessage(string message, float duration, Color color, bool shouldMove = true) {
    text.color = color;
    // Debug.Log("Popup pos: " + transform.position);
    if (shouldMove) {
      distance = initialDistance * (1 / duration);
    } else {
      distance = 0f;
    }
    transform.position += new Vector3(0f, 60f, 0f); 
    lifetime = duration;
    text.text = message;
  }
}