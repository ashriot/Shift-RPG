using UnityEngine;
using UnityEngine.UI;

public class PulsingImage : MonoBehaviour {
  private Image image;
  private bool pulsing;
  private float lifetime, timer, alpha;

  void Start() {
    image = GetComponent<Image>();
  }

  void OnEnable() {
    pulsing = false;
  }

  void Update() {
    if (!pulsing) { return; }

    if (image.color.a == 0f) {
      alpha = 1f;
    } else if (image.color.a == 1f) {
      if (timer == 0f) {
        timer = lifetime;
      }
      alpha = 0f;
    }
    if (timer > 0f) {
      timer -= Time.deltaTime;
    } else {
      timer = 0f;
      image.color = new Color(image.color.r, image.color.g, image.color.b,
        Mathf.MoveTowards(image.color.a, alpha, ( 1 / lifetime) * Time.deltaTime));
    }
  }

  public void Pulse(float duration) {
    lifetime = duration * 0.33f;
    pulsing = true;
  }

  public void StopPulse() {
    pulsing = false;
    image.color = new Color(image.color.r, image.color.g, image.color.b, 1f);
  }
}
