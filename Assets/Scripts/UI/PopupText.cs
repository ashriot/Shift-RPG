using UnityEngine;
using UnityEngine.UI;

public class PopupText : MonoBehaviour {

  public float lifetime;
  public float initialDistance;
  public Text text;
  public Image icon;

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
      if (icon != null)
        icon.color = fade;

      if(fade.a <=.1) {
        Destroy (gameObject);
      }
    }
    transform.position += new Vector3(0f, direction * distance * Time.deltaTime, 0f);
  }

  public void DisplayMessage(string message, Sprite sprite, float duration, Color color, bool isCrit, bool shouldMove = false) {
    if (sprite == null) {
      icon.gameObject.SetActive(false);
    } else {
      icon.sprite = sprite;
      icon.color = color;
    }
    text.color = color;
    // Debug.Log("Popup pos: " + transform.position);
    if (shouldMove) {
      text.fontSize = (int)(text.fontSize * (isCrit ? 1.5f : 1));
      // Debug.Log($"Font size: { text.fontSize }");
      distance = initialDistance * (1 / duration) * (isCrit ? 1.25f : 1);
    } else {
      transform.position += new Vector3(0f, 1.25f, 0f); 
      distance = 0f;
    }
    lifetime = duration;
    text.text = message;
  }
}