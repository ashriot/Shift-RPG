using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Panel : MonoBehaviour {

  public Unit unit;
  public Text currentHp;
  public ShakeObject shaker;
  public TooltipButton targetButton;
  public TooltipButton infoTooltip;
  public Image targetCursor;
  public bool panelMoved;

  public Image hpFillImage, image;
  public Image[] crystals;
  public ShieldIcon[] shields;

  public GameObject buffsPanel;
  public GameObject debuffsPanel;

  public List<StatusEffect> buffs = new List<StatusEffect>();
  public List<StatusEffect> debuffs = new List<StatusEffect>();
  public List<StatusEffect> buffsOnOthers = new List<StatusEffect>();
  public List<StatusEffect> debuffsOnOthers = new List<StatusEffect>();

  public float moveLifetime;
  public float initialSpeed;
  public Text notificationText;

  public bool updateHpBar, isStaggered, isStunned, isTaunting;
  public int ticks;

  public float damagePercentMod;
  public int damageFlatMod;

  private Vector3 movePosition;
  private Vector3 initialPos;

  private bool isMoving;

  private float intensity;
  private float speed;
  private float timer;
  private Color fade;

  private void Update() {
    if (isMoving) {
      if (timer >= moveLifetime) {
        isMoving = false;
      }
      // Debug.Log("sliding... " + transform.localPosition + " towards: " + movePosition + " at speed: " + speed);
      transform.localPosition = Vector3.MoveTowards(transform.localPosition, movePosition, speed * Time.deltaTime);
      timer += Time.deltaTime;
    }
  }

  public void Move(float distance, float duration) {
    timer = 0f;
    // Debug.Log("distance: " + distance + " " + transform.localPosition + " -> " + movePosition);
    movePosition = transform.localPosition + new Vector3(0f, distance, 0f);
    // Debug.Log("distance: " + distance + " " + transform.localPosition + " -> " + movePosition);
    speed = initialSpeed * (1 / duration);
    moveLifetime = duration;
    isMoving = true;
  }

  public void Click() {
    BattleManager.instance.ClickTarget(this);
  }
}
