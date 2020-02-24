using UnityEngine;
using UnityEngine.UI;

public class VictoryText : MonoBehaviour {

  public float lifetime;
  public float initialDistance;
  public Text text;

  private float distance, moveTimer, waitTimer;
  private Color fade;
  private int direction = 1;

  private void Update() {
    moveTimer += Time.deltaTime;
    if (moveTimer >= lifetime) { // done moving
      if (waitTimer >= lifetime * 2) {  // done waiting
        fade = text.color;
        fade.a = fade.a /1.1f;
        text.color = fade;

        if(fade.a <=.1) {
          Destroy (gameObject);
        }
      } else {
        waitTimer += Time.deltaTime;
      }
    } else {
      transform.position += new Vector3(0f, direction * distance * Time.deltaTime, 0f);
    }
  }

  public void DisplayVictoryText(string message, float duration) {
    // Debug.Log("Popup pos: " + transform.position);
    distance = initialDistance * (1 / duration);
    lifetime = duration;
    text.text = message;
    waitTimer = 0;
  }
}