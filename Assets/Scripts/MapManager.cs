using UnityEngine;
using UnityEngine.UI;

public class MapManager : MonoBehaviour {

  public Image foregroundImage;
  public float animationSpeed = 1f;
  public float initialSpeed = 150f;

  private float moveLifetime, fadeLifetime, flashLifetime, timer, speed;

  private Vector3 movePosition;

  private bool moving, fadingOut, fadingIn;

  void Awake() {
    foregroundImage.gameObject.SetActive(true);  
  }

  void Start() {
      Move();
  }

  void Update() {
    if (moving) {
      if (timer >= moveLifetime) {
        moving = false;
      }
      foregroundImage.transform.localPosition = Vector3.MoveTowards(foregroundImage.transform.localPosition, movePosition, speed * Time.deltaTime);
      // Debug.Log($"Moving { foregroundImage.transform.localPosition } -> { movePosition } at speed: { speed }.");
      timer += Time.deltaTime;
    } else if (fadingOut) {
      foregroundImage.color = new Color(foregroundImage.color.r, foregroundImage.color.g, foregroundImage.color.b,
          Mathf.MoveTowards(foregroundImage.color.a, 0f, ( 1 / fadeLifetime) * Time.deltaTime));
          
      if (foregroundImage.color.a == 0f) {
          fadingOut = false;
          foregroundImage.gameObject.SetActive(false);
      }
    }
  }

  void Move() {
    timer = 0f;
    movePosition = foregroundImage.transform.localPosition + new Vector3(0f, 500f, 0f);
    speed = initialSpeed * (1 / animationSpeed);
    Debug.Log($"Speed = {speed}.");
    moveLifetime = animationSpeed;
    moving = true;
  }

  void FadeOut() {
    timer = 0f;
    fadeLifetime = animationSpeed;
    fadingOut = true;
  }
}
