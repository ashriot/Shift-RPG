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
  public Image[] crystals, crystalFills;
  public ShieldIcon[] shields;
  public StaggerIcon staggerIcon;

  public GameObject hud;
  public StatusEffectPanel buffs, debuffs;

  public List<StatusEffectDisplay> buffsOnOthers = new List<StatusEffectDisplay>();
  public List<StatusEffectDisplay> debuffsOnOthers = new List<StatusEffectDisplay>();

  public float moveLifetime, fadeLifetime;
  public float initialSpeed;
  public Text notificationText;

  public new string name { get { return unit.name; } }
  public bool updateHpBar, isStunned, isTaunting;
  public bool isStaggered { get { return remainingStaggeredTurns > 0; } }
  public int remainingStaggeredTurns;
  public int staggerDelayAmount { get { return unit.staggerDelayAmount; } }
  public bool isDead { get { return unit.isDead; } }
  public decimal ticks;

  public float damageDealtPercentMod, damageTakenPercentMod, speedMod;
  public int damageDealtFlatMod;

  private Vector3 movePosition;
  private Vector3 initialPos;

  private bool isMoving, isFading;

  private float intensity, speed, timer;
  private Color fade;

  private void Update() {
    if (isMoving) {
      if (timer >= moveLifetime) {
        isMoving = false;
      }
      transform.localPosition = Vector3.MoveTowards(transform.localPosition, movePosition, speed * Time.deltaTime);
      timer += Time.deltaTime;
    } else if (isFading) {
      image.color = new Color(image.color.r, image.color.g, image.color.b,
          Mathf.MoveTowards(image.color.a, 0f, ( 1 / fadeLifetime) * Time.deltaTime));
          
      if (image.color.a == 0f) {
          isFading = false;
          image.gameObject.SetActive(false);
          hud.SetActive(false);
          buffs.gameObject.SetActive(false);
          debuffs.gameObject.SetActive(false);
      }
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

  public void FadeOut(float duration) {
    timer = 0f;
    fadeLifetime = duration;
    isFading = true;
  }
}

public class PredictedPanel {
  
  public decimal ticks;
  public Panel panel;

  public PredictedPanel(decimal _ticks, ref Panel _panel) {
    ticks = _ticks;
    panel = _panel;
  }
}
