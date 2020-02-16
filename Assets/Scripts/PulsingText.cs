using UnityEngine;
using UnityEngine.UI;

public class PulsingText : MonoBehaviour
{
  private Text text;
  private bool pulsing;
  private float lifetime, timer, alpha;

  void Start() {
    text = GetComponent<Text>();
  }

  void OnEnable() {
    pulsing = false;
  }

  void Update() {
    if (!pulsing) { return; }

    if (text.color.a == 0f) {
      alpha = 1f;
    } else if (text.color.a == 1f) {
      if (timer == 0f) {
        timer = lifetime * 2f;
      }
      alpha = 0f;
    }
    if (timer > 0f) {
      timer -= Time.deltaTime;
    } else {
      timer = 0f;
      text.color = new Color(text.color.r, text.color.g, text.color.b,
        Mathf.MoveTowards(text.color.a, alpha, ( 1 / lifetime) * Time.deltaTime));
    }
  }

  public void Pulse(float duration) {
    lifetime = duration;
    pulsing = true;
  }
}
