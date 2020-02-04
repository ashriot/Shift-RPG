using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class BattleManager : MonoBehaviour {

  public static BattleManager instance;

  [Header("Object Refs")]
  public Canvas canvas;
  public ActionMenu actionMenu;
  public ShiftMenu shiftMenu;
  public Tooltip tooltip;

  [Header("Prefabs")]
  public PopupText popup;
  public PopupText damagePopup;
  public StatusEffectDisplay buffDisplay;
  public StatusEffectDisplay debuffDisplay;

  [Header("Hero/Enemy Refs")]
  public HeroPanel[] heroPanels;
  public EnemyPanel[] enemyPanels;
  public List<HeroPanel> heroes;
  public List<EnemyPanel> enemies;
  public List<Enemy> enemyLoadList;

  [Header("Control Vars")]
  public float battleSpeed;
  public bool battleActive;
  public bool battleWaiting;
  public bool choosingTarget;
  public bool delaying;
  public bool paused;
  public bool pausedForTriggers;
  public List<Panel> combatants = new List<Panel>();
  public Panel currentPanel;
  public Panel playerTarget;
  public HeroPanel enemyTarget;
  public Action currentAction;  // TODO: hook this up

  [Header("Text colors")]
  public Color orange;
  public Color purple;
  public Color yellow;
  public Color gray;
  public Color black;
  public Color blue;
  public Color green;

  private const float INITIATIVE_GROWTH = .05f;
  private const float SHAKE_INTENSITY = 20f;

  private void Awake() {
    instance = this;
    DontDestroyOnLoad(gameObject);
  }
  private void Start() {
    actionMenu.gameObject.SetActive(false);
    foreach(var panel in enemyPanels) {
      panel.gameObject.SetActive(false);
    }

    shiftMenu.gameObject.SetActive(false);
    Tooltip.HideTooltip();
    InitializeBattle();
  }

  public void InitializeBattle() {
    if (battleActive) return;

    AudioManager.instance.PlayBgm("battle-conflict");
    battleActive = true;

    for (var i = 0; i < heroPanels.Length; i++) {
      if (i >= GameManager.instance.heroes.Count) { 
        heroPanels[i].gameObject.SetActive(false);
        heroPanels[i].GetComponent<RectTransform>().gameObject.SetActive(false);
        continue;
      }

      var panel = heroPanels[i];

      panel.gameObject.SetActive(true);
      panel.GetComponent<RectTransform>().gameObject.SetActive(true);
      panel.unit = GameManager.instance.heroes[i];
      var hero = panel.unit as Hero;
      panel.jobColor.color = hero.currentJob.jobColor;
      panel.jobIcon.sprite = hero.currentJob.jobIcon;
      panel.unit.hpCurrent = panel.unit.hpMax;
      panel.unit.armorCurrent = panel.unit.armorMax;
      panel.unit.mpCurrent = panel.unit.mp;
      panel.unitNameText.text = panel.unit.name;
      panel.currentJobName.text = hero.currentJob.name.ToUpper();
      panel.hpFillImage.fillAmount = panel.unit.hpPercent;
      panel.currentHp.text = panel.unit.hpMax.ToString();
      UpdateCrystalsAndShields(panel);
    }

    for(var i = 0; i < enemyPanels.Length; i++) {
      if (i >= enemyLoadList.Count) { 
        enemyPanels[i].gameObject.SetActive(false);
        continue;
      } else {
        var panel = enemyPanels[i] as EnemyPanel;
        panel.gameObject.SetActive(true);
        panel.unit = Instantiate(enemyLoadList[i]);
        var enemy = panel.unit as Enemy;
        panel.image.sprite = enemy.sprite;
        panel.unit.hpCurrent = panel.unit.hpMax;
        panel.unit.armorCurrent = panel.unit.armorMax;
        panel.unit.mpCurrent = panel.unit.mp;
        panel.hpFillImage.fillAmount = panel.unit.hpPercent;
        UpdateCrystalsAndShields(panel);
        SetupTooltip(panel.tooltipButton, enemy.name, "Enemy".ToUpper(), "NA", enemy.tooltipDescription);
      }
    }
    heroes = heroPanels.Where(p => p.gameObject.activeInHierarchy).ToList();
    enemies = enemyPanels.Where(p => p.gameObject.activeInHierarchy).ToList();

    combatants.AddRange(heroes);
    combatants.AddRange(enemies);

    ClearAllTargetting();
    StartCoroutine(DoDelay(0.5f));
    SetInitialTicks();
    NextTurn();
  }

  private void UpdateCrystalsAndShields(Panel panel) {
    var crystals = panel.unit.mpCurrent / 10;
    for (var m = 0; m < panel.crystals.Length; m++) {
      if (m >= crystals) {
        panel.crystals[m].gameObject.SetActive(false);
        continue;
      }
      panel.crystals[m].gameObject.SetActive(true);
    }
    var shieldColor = gray;
    if (panel.isStaggered) {
      shieldColor = Color.red;
      panel.unit.armorCurrent = panel.unit.armorMax;
    }
    var shields = panel.unit.armorCurrent / 10;
    for (var m = 0; m < panel.shields.Length; m++) {
      if (m >= shields) {
        panel.shields[m].gameObject.SetActive(false);
        continue;
      }
      panel.shields[m].gameObject.SetActive(true);
      panel.shields[m].shieldIcon.color = shieldColor;
    }
  }

  private void Update() {
    foreach(var panel in heroes) {
      if (panel.updateHpBar) {
        SmoothHpBar(panel, panel.unit.hpPercent);
      }
    }
    foreach (var panel in enemies) {
      if (panel.updateHpBar) {
        SmoothHpBar(panel, panel.unit.hpPercent);
      }
    }
  }

  void FixedUpdate() {
    if (!battleActive || !battleWaiting || delaying || paused || pausedForTriggers || choosingTarget) { return; }
    battleWaiting = false;

    // check status effects

    if (combatants.All(x => x.unit.isDead && !x.unit.isPlayer)) return;

    currentPanel.unit.mpCurrent += currentPanel.unit.mpRegen;
    UpdateUi();

    if (currentPanel.isStunned) {
      CalculateSpeedTicks(currentPanel, 1f);
      currentPanel.isStunned = false;
      if (currentPanel.isStaggered) {
        currentPanel.isStaggered = false;
      }
      var position = currentPanel.gameObject.transform.position;
      position.y += currentPanel.GetComponent<RectTransform>().rect.height;
      Instantiate(popup, position, currentPanel.gameObject.transform.rotation, canvas.transform).DisplayMessage("Recovered", battleSpeed * 0.8f, Color.white, false);
      UpdateUi();
      NextTurn();
      return;
    }
    var buffs = currentPanel.buffs.Where(b => b.trigger == Triggers.StartOfTurn).ToList();
    if (buffs.Count > 0) {
      StartCoroutine(DoHandleStatusEffects(buffs, currentPanel));
    }

    if (currentPanel.unit.isPlayer) {
      // Debug.Log("Player turn");
      // HERO TURN
      var hero = currentPanel.unit as Hero;
      hero.shiftedThisTurn = false;
      shiftMenu.shiftLTooltipButton.interactable = true;
      shiftMenu.shiftRTooltipButton.interactable = true;
      MovePanel(currentPanel, true);
      SlideHeroMenus(hero, true);

    } else {
      // Debug.Log("Enemy turn");
      // ENEMY TURN

      var taunters = heroes.Where(h => h.isTaunting).ToList();
      if (taunters.Count > 0) {
        enemyTarget = taunters.First();
      } else {
        // random target
        // var random = Random.Range(0, heroes.Count);
        var random = 0;
        enemyTarget = heroes[random];
      }
      EnemyAction();
    }
  }

  private void SetInitialTicks() {
      foreach (var c in combatants) {
          CalculateSpeedTicks(c, 1f);
      }
  }

  private void NextTurn() {
    ResetActions();
    playerTarget = null;
    actionMenu.gameObject.SetActive(false);
    shiftMenu.gameObject.SetActive(false);
    // check for battle end

    // UpdatePlayerTarget();

    CountdownTicks();

  }

  private void UpdateUi() {
    for (var i = 0; i < heroes.Count; i++) {
      var panel = heroPanels[i];
      if (panel.unit.armorCurrent < 0 && !panel.isStaggered && !panel.unit.isDead) {
        panel.isStaggered = true;
        ShakePanel(heroes[i], SHAKE_INTENSITY, 1.5f);
        AudioManager.instance.PlaySfx("damage02");
        Debug.Log("Stagger!");
        var position = panel.gameObject.transform.position;
        position.y += panel.GetComponent<RectTransform>().rect.height;
        Instantiate(popup, position, panel.gameObject.transform.rotation, canvas.transform).DisplayMessage("Stagger!", battleSpeed * 0.8f, Color.white, false);
        panel.isStunned = true;
        CalculateSpeedTicks(panel, 1f);
        StartCoroutine(DoFlashImage(panel.image, Color.red));
        StartCoroutine(DoPause());
      }
      var hero = panel.unit as Hero;
      panel.currentJobName.text = hero.currentJob.name.ToUpper();
      panel.jobColor.color = hero.currentJob.jobColor;
      panel.jobIcon.sprite = hero.currentJob.jobIcon;

      if (!panel.buffs.Contains(hero.currentJob.trait)) {
        panel.buffs.Add(hero.currentJob.trait);
      }
      panel.updateHpBar = true;
      panel.currentHp.text = panel.unit.hpCurrent.ToString();
      UpdateCrystalsAndShields(panel);

      foreach(var buff in panel.buffs) {
        // TODO finish
      }
    }

    for(var i = 0; i < enemies.Count; i++) {
      var panel = enemies[i];
      if (panel.unit.armorCurrent <= -10 && !panel.isStaggered && !panel.unit.isDead) {
        panel.isStaggered = true;
        ShakePanel(panel, SHAKE_INTENSITY, 1.5f);
        AudioManager.instance.PlaySfx("damage02");
        Debug.Log("Stagger!");
        var popupText = Instantiate(popup, panel.transform, false);
        popupText.transform.localPosition += new Vector3(0f, 75f, 0f);
        popupText.DisplayMessage("Stagger!", battleSpeed, Color.white, false);
        panel.isStunned = true;
        CalculateSpeedTicks(panel, 1f);
        StartCoroutine(DoFlashImage(panel.image, Color.red));
        StartCoroutine(DoPause());
      }
      if (panel.unit.isDead) {
        panel.gameObject.SetActive(false);
      } else {
        panel.updateHpBar = true;
        UpdateCrystalsAndShields(panel);
      }
    }
    enemies = enemies.Where(e => !e.unit.isDead).ToList();
  }

  private void SmoothHpBar(Panel panel, float newAmount) {
    if (panel.hpFillImage.fillAmount - newAmount > 0.001f) {
      panel.hpFillImage.fillAmount = Mathf.Lerp(panel.hpFillImage.fillAmount, newAmount, Time.deltaTime * (1 / battleSpeed) * 5f);
    } else {
      if (newAmount <= 0f) {
        panel.hpFillImage.fillAmount = 0f;
      }
      panel.updateHpBar = false;
    }
  }

  private void MovePanel(Panel panel, bool up) {
    panel.panelMoved = up;
    // Debug.Log("Starting pos: " + panel.transform.localPosition);
    var distance = 40f * (up ? 1f : -1f);
    panel.Move(distance, 0.25f * battleSpeed);
  }

  private void SlideHeroMenus(Hero hero, bool positive) {
    if (positive) { DisplayHeroMenus(hero); }
    var endPos = actionMenu.transform.position + new Vector3(0f, (positive ? 160f : -160f), 0f);
    var startSize = shiftMenu.GetComponent<RectTransform>().sizeDelta;
    var endSize = (positive ? new Vector2(1250f, startSize.y) : shiftMenu.initialSize);
    StartCoroutine(DoMoveTo(actionMenu.transform, actionMenu.gameObject.transform.position, endPos, endSize, battleSpeed / 12f));
  }

  private IEnumerator DoMoveTo(Transform tform, Vector3 startPos, Vector3 endPos, Vector2 endSize, float time) {
    float elapsedTime = 0;
    // Debug.Log("Move to: " + startSize + " -> " + endSize);
    var startSize = shiftMenu.GetComponent<RectTransform>().sizeDelta;

    while (elapsedTime <= time) {
      tform.position = Vector3.Lerp(startPos, endPos, (elapsedTime / time));
      shiftMenu.GetComponent<RectTransform>().sizeDelta = Vector2.Lerp(startSize, endSize, (elapsedTime / time));
      elapsedTime += Time.deltaTime;
      yield return null;
    }
    // Make sure we got there
    tform.position = endPos;
    shiftMenu.GetComponent<RectTransform>().sizeDelta = endSize;
    yield return null;
  }

  private void DisplayHeroMenus(Hero hero) {
    AudioManager.instance.PlaySfx("end_turn");
    for (var i = 0; i < actionMenu.actionButtons.Length; i++) {
      actionMenu.transform.position = actionMenu.initialPos;
      if (hero.currentJob.actions[i] != null) {
        SetupTooltip(actionMenu.actionButtons[i].tooltipButton, hero.currentJob.actions[i].name, "Battle Action".ToUpper(), hero.currentJob.actions[i].mpCost.ToString(), hero.currentJob.actions[i].description);
        actionMenu.actionButtons[i].gameObject.SetActive(true);
        actionMenu.actionButtons[i].icon.sprite = hero.currentJob.actions[i].sprite;
        if (hero.currentJob.actions[i].mpCost <= hero.mpCurrent) {
          actionMenu.actionButtons[i].fillColor.color = hero.currentJob.jobColor;
        } else {
          var color = hero.currentJob.jobColor;
          actionMenu.actionButtons[i].fillColor.color = new Color(color.r, color.g, color.b, color.a / 2f);
        }
        actionMenu.actionButtons[i].nameText.text = hero.currentJob.actions[i].name;
        actionMenu.actionButtons[i].mpCost = hero.currentJob.actions[i].mpCost;
        var crystals = actionMenu.actionButtons[i].mpCost / 10;
        for (var m = 0; m < actionMenu.actionButtons[i].crystals.Length; m++) {
          if (m >= crystals) {
            actionMenu.actionButtons[i].crystals[m].gameObject.SetActive(false);
            continue;
          }
          actionMenu.actionButtons[i].crystals[m].gameObject.SetActive(true);
        }
      }
      else {
        actionMenu.actionButtons[i].gameObject.SetActive(false);
      }
    }

    // set current trait and shift action info
    SetupTooltip(shiftMenu.traitTooltipButton, hero.currentJob.trait.name, "Passive Trait".ToUpper(), "NA", hero.currentJob.trait.description);
    shiftMenu.traitColor.color = hero.currentJob.jobColor;
    shiftMenu.traitName.text = hero.currentJob.trait.name;
    shiftMenu.traitIcon.sprite = hero.currentJob.trait.sprite;
    SetupTooltip(shiftMenu.shiftActionTooltipButton, hero.currentJob.shiftAction.name, "Shift Action".ToUpper(), "NA", hero.currentJob.shiftAction.description);
    shiftMenu.shiftColor.color = hero.currentJob.jobColor;
    shiftMenu.shiftName.text = hero.currentJob.shiftAction.name;
    shiftMenu.shiftIcon.sprite = hero.currentJob.shiftAction.sprite;

    if (hero.jobs.Length > 1) {
      var jobIndex = System.Array.IndexOf(hero.jobs, hero.currentJob);
      var jobLIndex = (jobIndex - 1) < 0 ? hero.jobs.Length - 1 : jobIndex - 1;
      var jobRIndex = (jobIndex + 1) == hero.jobs.Length ? 0 : jobIndex + 1;
      var jobL = hero.jobs[jobLIndex];
      var jobR = hero.jobs[jobRIndex];
      shiftMenu.GetComponent<RectTransform>().sizeDelta = shiftMenu.initialSize;
      shiftMenu.gameObject.SetActive(true);
      shiftMenu.nameL.text = jobL.name;
      shiftMenu.jobIconL.sprite = jobL.jobIcon;
      shiftMenu.colorL.color = jobL.jobColor;
      shiftMenu.jobIdL = jobLIndex;
      SetupTooltip(shiftMenu.shiftLTooltipButton, jobL.name, "Job".ToUpper(), "NA", jobL.description, jobL);
      shiftMenu.nameR.text = jobR.name;
      shiftMenu.jobIconR.sprite = jobR.jobIcon;
      shiftMenu.colorR.color = jobR.jobColor;
      shiftMenu.jobIdR = jobRIndex;
      SetupTooltip(shiftMenu.shiftRTooltipButton, jobR.name, "Job".ToUpper(), "NA", jobR.description, jobR);
    }
    else {
      shiftMenu.gameObject.SetActive(false);
    }
    actionMenu.gameObject.SetActive(true);

  }

  private void SetupTooltip(TooltipButton button, string title, string kind, string cost, string content, Job job = null) {
    button.title = title;
    button.kind = kind;
    button.cost = cost;
    button.content = content;
    button.job = job;
  }

  private void ShakePanel(Panel panel, float intensity, float duration = 0.5f) {
    panel.shaker.Shake(intensity, duration * battleSpeed);
  }

  private void CalculateSpeedTicks(Panel panel, float delay) {
    var inverse = Mathf.Pow((1 + INITIATIVE_GROWTH), panel.unit.speed);
    var result = 1000 / inverse;
    result *= delay;
    panel.ticks = (int)result;
    // Debug.Log(combatant.name + " ticks: " + combatant.ticks);
  }

  // public void PredictTurnOrder() {
  //     while (potentialTurnOrder.Count < 5) {
  //         for (var i = 0; i < combatants.Count; i++) {
  //             if (combatants[i].ticks < 0){
  //                 Debug.LogError("Initiative went negative!");
  //                 combatants[i].ticks = 1;
  //             }
  //             if (combatants[i].isDead) {
  //                 potentialTurnOrder.RemoveAll(to => to == i);
  //                 continue;
  //             }
  //             combatants[i].ticks--;
  //             if (combatants[i].ticks == 0) {
  //                 potentialTurnOrder.Add(i);
  //                 // TODO: Replace with the actual speed of the action they use
  //                 combatants[i].ticks = CalculateSpeedTicks(combatants[i], 1f);
  //             }
  //         }
  //     }
  // }

  public void CountdownTicks() {
    while(true) {
      foreach(var combatant in combatants) {
        if (!combatant.unit.isDead && combatant.ticks == 0) {
          currentPanel = combatant;
          battleWaiting = true;
          return;
        } else {
          // Debug.Log(combatant.name + ": " + combatant.ticks);
          combatant.ticks--;
        }
      }
    }
  }

  public void ClickTarget(Panel target) {
    AudioManager.instance.PlaySfx("click");
    ClearAllTargetting();
    Debug.Log("Clicked target: " + target.unit.name);
    if (target.GetType() == typeof(EnemyPanel)) {
      playerTarget = target as EnemyPanel;
    } else {
      playerTarget = target as HeroPanel;
    }

    TargetChosen(currentAction);

    // } else if (playerTarget.unit.isDead) {
    //   playerTarget = combatants.Where(c => !c.unit.isPlayer && !c.unit.isDead).Cast<EnemyPanel>().First();

    // foreach (var panel in enemyPanels) {
    //   if (panel.unit == playerTarget) {
    //     panel.targetCursor.GetComponentInParent<Button>().Select();
    //   } else {
    //     panel.targetCursor.gameObject.SetActive(true);
    //   }
    // }
  }

  public void ClickActionButton(int buttonId) {
    if (!battleActive || delaying || paused || pausedForTriggers) return;

    AudioManager.instance.PlaySfx("click");
    Action action = null;
    var hero = currentPanel.unit as Hero;
    if (currentAction != null) {
      if (currentAction.name == actionMenu.actionButtons[buttonId].nameText.text) {
        ClearAllTargetting();
        ResetActions();
        return;
      }
        for (var i = 0; i < actionMenu.actionButtons.Length; i++) {
        if (actionMenu.actionButtons[i].nameText.text == currentAction.name) {
          actionMenu.actionButtons[i].GetComponent<Image>().color = Color.black;
        }
      }
    }
    actionMenu.actionButtons[buttonId].GetComponent<Image>().color = yellow;
    var actionName = actionMenu.actionButtons[buttonId].nameText.text;
    foreach (var a in hero.currentJob.actions) {
      if (a.name == actionName) {
        action = a;
        break;
      }
    }
    if (action != null){
      currentAction = action;
      Debug.Log("Current action: " + currentAction.name);

      if (action.targetType == TargetTypes.AllEnemies || action.targetType == TargetTypes.OneEnemy || action.targetType == TargetTypes.RandomEnemies) {
        ShowEnemyTargetting();
      } else if (action.targetType == TargetTypes.Self) {
        ShowSelfTargetting();
      } else if (action.targetType == TargetTypes.AlliesOnly || action.targetType == TargetTypes.SelfOrAllies || action.targetType == TargetTypes.WholeParty) {
        ShowAllyTargetting();
      } else if (action.targetType == TargetTypes.WholeParty) {
        ShowAllyOnlyTargetting();
      }
    }
    else {
      Debug.Log("action is null!");
    }
  }

  private void ShowEnemyTargetting() {
    ClearAllTargetting();
    foreach(var panel in enemies) {
      panel.tooltipButton.interactable = true;
    }
    choosingTarget = true;
  }

  private void ShowSelfTargetting() {
    ClearAllTargetting();
    foreach(var panel in heroes) {
      if (panel == currentPanel) {
        panel.tooltipButton.interactable = true;
        break;
      }
    }
    choosingTarget = true;
  }

  private void ShowAllyTargetting() {
    ClearAllTargetting();
    foreach(var panel in heroes) {
      panel.tooltipButton.interactable = true;
    }
    choosingTarget = true;
  }

  private void ShowAllyOnlyTargetting() {
    ClearAllTargetting();
    foreach(var panel in heroes) {
      panel.tooltipButton.interactable = true;
    }
    choosingTarget = true;
  }

  private void ClearAllTargetting() {
    foreach(var panel in combatants) {
      panel.tooltipButton.interactable = false;
    }
    choosingTarget = false;
  }

  private void ResetActions() {
    if (currentAction == null) return;
    currentAction = null;
    for (var i = 0; i < actionMenu.actionButtons.Length; i++) {
        actionMenu.actionButtons[i].GetComponent<Image>().color = Color.black;
      }
  }

  public void TargetChosen(Action action) {
    if (action.mpCost > currentPanel.unit.mpCurrent) {
        var position = currentPanel.gameObject.transform.position;
        position.y += currentPanel.GetComponent<RectTransform>().rect.height;
        Instantiate(popup, position, currentPanel.gameObject.transform.rotation, currentPanel.transform).DisplayMessage("Not enough Mana!", battleSpeed * .8f, Color.white, false);
        return;
      }
    HeroAction(action);
  }

  public void ClickShiftButton(int buttonId) {
    var hero = currentPanel.unit as Hero;
    if (hero.shiftedThisTurn) {
      var position = currentPanel.gameObject.transform.position;
      position.y += currentPanel.GetComponent<RectTransform>().rect.height;
      Instantiate(popup, position, currentPanel.gameObject.transform.rotation, currentPanel.transform).DisplayMessage("Already Shifted", battleSpeed * .8f, Color.white, false);
    } else {
      AudioManager.instance.PlaySfx("click");
      shiftMenu.shiftLTooltipButton.interactable = false;
      shiftMenu.shiftRTooltipButton.interactable = false;
      StartCoroutine(DoShiftJobs(buttonId));
    }
  }

  public IEnumerator DoShiftJobs(int jobId) {
    var hero = currentPanel.unit as Hero;
    hero.shiftedThisTurn = true;
    SlideHeroMenus(hero, false);
    hero.currentJob = hero.jobs[jobId];
    yield return new WaitForSeconds(battleSpeed / 3f);

    SlideHeroMenus(hero, true);
    UpdateUi();
  }

  public void HeroAction(Action action) {
    // actionMenu.gameObject.SetActive(false);
    // shiftMenu.gameObject.SetActive(false);
    SlideHeroMenus(currentPanel.unit as Hero, false);
    Debug.Log("Executing: " + action.name);
    StartCoroutine(DoHeroAction(action));
  }

  public IEnumerator DoHeroAction(Action action, HeroPanel heroPanel = null, EnemyPanel targetPanel = null, bool nextTurn = true) {
    if (targetPanel != null && targetPanel.unit.isDead) yield break;
    var panel = heroes.Where(hp => hp == (heroPanel ?? currentPanel)).Single();
    panel.unit.mpCurrent -= action.mpCost;
    UpdateUi();

    var position = panel.gameObject.transform.position;
    position.y += panel.GetComponent<RectTransform>().rect.height * 0.8f;
    Instantiate(popup, position, panel.gameObject.transform.rotation, canvas.transform).DisplayMessage(action.name, battleSpeed * .8f, Color.white, false);
    yield return new WaitForSeconds(battleSpeed / 3f);

    for (var h = 0; h < action.hits; h++) {
      while (paused || delaying) {
        yield return new WaitForEndOfFrame();
      }
      ExecuteActionEffects(action);

      if (action.targetType == TargetTypes.RandomEnemies) {
        var rand = Random.Range(0, enemies.Count - 1);
        var randomTarget = enemies[rand];
        HeroDealDamage(action, randomTarget, heroPanel);
      } else {
        HandleActionTargetting(action, heroPanel, targetPanel);
      }

      if (action.hits > 1) {
        yield return new WaitForSeconds(battleSpeed / 3f);
      }
      if (action.additionalDamage.Count > 0) {
        foreach (var add in action.additionalDamage) {
          yield return new WaitForSeconds(battleSpeed / 3f);
          Debug.Log("Additional damage" + add.name);
          HeroDealDamage(add, targetPanel ?? playerTarget, heroPanel);
        }
      }
    }

    CalculateSpeedTicks(currentPanel, action.delay);
    if (nextTurn) {
      if (panel.panelMoved) {
        MovePanel(panel, false);
      }
      Debug.Log("DoDelay");
      StartCoroutine(DoDelay());
      NextTurn();
    }
  }

  private void HandleActionTargetting(Action action, HeroPanel heroPanel = null, EnemyPanel enemyPanel = null) {
    switch (action.targetType) {
      case TargetTypes.OneEnemy:
        var targetPanel = enemies.Where(ep => ep == (enemyPanel ?? playerTarget)).Single();
        HeroDealDamage(action, targetPanel, heroPanel);
        break;
      case TargetTypes.AllEnemies:
        foreach(var panel in enemies) {
          HeroDealDamage(action, panel, heroPanel);
          if (action.additionalDamage.Count > 0) {
            foreach (var add in action.additionalDamage) {
              HeroDealDamage(add, panel, heroPanel);
            }
          }
        }
        break;
      case TargetTypes.Self:
      case TargetTypes.SelfOrAllies:
      case TargetTypes.AlliesOnly:
        HeroHeal(action, playerTarget as HeroPanel);
        break;
      default:
        Debug.LogError("Invalid action type! " + action.targetType);
        break;
    }
    // BUFFS
    
    if (action.buffs.Count > 0) {
      foreach(var buff in action.buffs) {
        Debug.Log("Added buff: " + buff.name);
        if (!currentPanel.buffs.Contains(buff)) {
          currentPanel.buffs.Add(buff);
        }
      }
    }
  }

  public void HeroHeal(Action action, HeroPanel targetPanel, HeroPanel hero = null) {
    var user = hero ?? currentPanel;
    var isCrit = false;
    var color = Color.white;

    var power = 0f;

    if (action.powerType == PowerTypes.Attack) {
      power = user.unit.attack;
    }
    else if (action.powerType == PowerTypes.Willpower) {
      power = user.unit.willpower;
    }
    else {
      Debug.LogError("Missing power type!");
    }

    var amount = power;
    
    if (action.damageType == DamageTypes.ArmorGain) {
      color = gray;
      if (targetPanel.isStaggered) {
        amount = 0;
      } else {
        amount = action.potency;
        targetPanel.unit.armorCurrent = Mathf.Clamp(targetPanel.unit.armorCurrent + (int)amount, 0, 100);
        amount /= 10;
      }
    } else if (action.damageType == DamageTypes.ManaGain) {
      color = blue;
      amount = action.potency;
      targetPanel.unit.mpCurrent = Mathf.Clamp(targetPanel.unit.mpCurrent + (int)amount, 0, 100);
      amount /= 10;
    } else if (action.damageType == DamageTypes.HealthGain) {
      color = green;
      amount *= action.potency;
    }

    var panel = heroes.Where(hp => hp == targetPanel).Single();
    var position = panel.image.transform.position;

    Instantiate(damagePopup, position, panel.gameObject.transform.rotation, canvas.transform).DisplayMessage("+" + amount.ToString("N0"), battleSpeed * 0.8f, color, isCrit, true);

    UpdateUi();
  }

  public void HeroDealDamage(Action action, Panel defender, Panel hero = null) {
    var attacker = hero ?? currentPanel;
    var isCrit = false;
    var color = Color.white;
    
    if (action.damageType == DamageTypes.Martial) {
      color = orange;
    } else if (action.damageType == DamageTypes.Ether) {
      color = purple;
    } else if (action.damageType == DamageTypes.Piercing) {
      color = yellow;
    }

    // check for Crit
    var critRoll = Random.Range(1, 100);
    if (critRoll <= attacker.unit.crit) {
      isCrit = true;
    }

    var damage = CalculateDamage(attacker, defender, action, isCrit);

    var panel = enemies.Where(ep => ep == defender).Single();
    var position = panel.image.transform.position;

    Instantiate(damagePopup, position, panel.gameObject.transform.rotation, canvas.transform).DisplayMessage(damage, battleSpeed * 0.8f, color, isCrit, true);
    ShakePanel(panel, SHAKE_INTENSITY);

    UpdateUi();
  }

  public void ExecuteActionEffects(Action action) {
    AudioManager.instance.PlaySfx(action.sfxName);
  }

  public void EnemyAction() {
    StartCoroutine(DoFlashImage(currentPanel.image, Color.clear));

    var enemy = currentPanel.unit as Enemy;

    var rand = Random.Range(0, enemy.actions.Count);

    var action = enemy.actions[rand];

    // Debug.Log("Executing: " + action.name);
    StartCoroutine(DoEnemyAction(action));
  }

  private IEnumerator DoEnemyAction(Action action) {
    yield return new WaitForSeconds(battleSpeed / 5f);
    var position = currentPanel.gameObject.transform.localPosition;
    // Debug.Log("Enemy pos: " + position);
    position.y += currentPanel.GetComponent<RectTransform>().rect.height * 2;

    var popupText = Instantiate(popup, currentPanel.transform, false);
    popupText.transform.localPosition += new Vector3(0f, 75f, 0f);
    popupText.DisplayMessage(action.name, battleSpeed * 0.8f, Color.white, false);

    currentPanel.unit.mpCurrent -= action.mpCost;
    UpdateUi();
    yield return new WaitForSeconds(battleSpeed / 2f);

    if (action.targetType == TargetTypes.OneEnemy) {
      for (var h = 0; h < action.hits; h++) {
        while (paused || delaying || pausedForTriggers) {
          yield return new WaitForEndOfFrame();
        }
        ExecuteActionEffects(action);
        EnemyDealDamage(action);
        if (action.hits > 1) {
          yield return new WaitForSeconds(battleSpeed / 4f);
        }
      }
    } else if (action.targetType == TargetTypes.AllEnemies) {
      foreach(var heroPanel in heroes) {
        for (var h = 0; h < action.hits; h++){
          while (paused || delaying || pausedForTriggers) {
            yield return new WaitForEndOfFrame();
          }
          ExecuteActionEffects(action);
          EnemyDealDamage(action, heroPanel);
          if (action.hits > 1) {
            yield return new WaitForSeconds(battleSpeed / 4f);
          }
        }
      }

    }

    

    yield return new WaitForSeconds(battleSpeed);

    while (pausedForTriggers) {
      yield return new WaitForEndOfFrame();
    }
    CalculateSpeedTicks(currentPanel, action.delay);
    Debug.Log("Enemy turn done.");
    NextTurn();
  }

  private void EnemyDealDamage(Action action, HeroPanel targetPanel = null) {
    // Debug.Log("Enemy dealing damage to " + enemyTarget.heroName);
    var attacker = currentPanel;
    var defender = targetPanel ?? enemyTarget;
    var color = Color.white;
    var isCrit = false;

    if (action.damageType == DamageTypes.Martial) {
      color = orange;
    } else if (action.damageType == DamageTypes.Ether) {
      color = purple;
    } else if (action.damageType == DamageTypes.Piercing) {
      color = yellow;
    }

    // check for Crit
    var critRoll = Random.Range(1, 100);
    // Debug.Log("Crit Roll: " + critRoll + "/" + attacker.crit);
    if (critRoll <= attacker.unit.crit) {
      isCrit = true;
    }

    var damage = CalculateDamage(attacker, defender, action, isCrit);

    var panel = heroes.Where(hp => hp == defender).Single();
    var position = panel.gameObject.transform.position + new Vector3(0f, 50f, 0f);
    Instantiate(damagePopup, position, panel.gameObject.transform.rotation, canvas.transform).DisplayMessage(damage, battleSpeed * 0.8f, color, isCrit, true);
    ShakePanel(panel, SHAKE_INTENSITY);

    if (!defender.isStaggered) {
      var onHitEffects = defender.buffs.Where(b => b.trigger == Triggers.BeingHit).ToList();
      onHitEffects.AddRange(defender.debuffs.Where(b => b.trigger == Triggers.BeingHit).ToList());
      if (onHitEffects.Count > 0) {
          StartCoroutine(DoHandleStatusEffects(onHitEffects, defender));
      }
    }

    UpdateUi();
  }

  private string CalculateDamage(Panel attacker, Panel defender, Action action, bool isCrit) {
    var power = 0f;

    if (action.powerType == PowerTypes.Attack) {
      power = attacker.unit.attack;
    }
    else if (action.powerType == PowerTypes.Willpower) {
      power = attacker.unit.willpower;
    }
    else {
      Debug.LogError("Missing power type!");
    }

    var damage = power;
    var potency = action.potency;

    if (isCrit) {
      damage += attacker.unit.surge;
    }
    
    // TODO: Fix
    if (action.name == "Mana Bolt") {
      potency *= defender.unit.mpCurrent;
      Debug.Log("Mana Bolt potency: " + potency);
    } else if (action.name == "Disintegrate") {
      potency *= (1 + (1 - defender.unit.hpPercent));
    }

    if (action.splitDamage) {
      potency /= enemies.Where(e => !e.unit.isDead).Count();
    }

    damage *= potency;
    var defense = 0f;

    if (!defender.isStaggered && action.removesArmor) {
      defender.unit.armorCurrent -= 10;
      if (defender.unit.armorCurrent <= -10)
      {
        defender.unit.armorCurrent = -10;
      }
    }
    UpdateUi();

    if (action.damageType == DamageTypes.Martial) {
      defense = (defender.isStaggered ? 0 : defender.unit.Defense);
    }
    else if (action.damageType == DamageTypes.Ether) {
      defense = (defender.isStaggered ? 0 : defender.unit.Resist);
    }
    Debug.Log(attacker.unit.name + " attacking " + defender.unit.name + " for " + damage + " * (1 - " + defense + ") = " + (int)(damage * (1f - defense)));

    damage = (damage * (1f - defense));

    if (defender.isStaggered) {
      damage *= attacker.unit.breakBonus;
    }

    // Debug.Log("Damage after def: " + damage);

    defender.unit.hpCurrent -= (int)damage;
    if (defender.unit.hpCurrent < 0) {
      defender.unit.hpCurrent = 0;
    }
    // Debug.Log(defender.name + "'s HP: " + defender.hpCurrent);

    var result = ((int)damage).ToString("N0");
    if (isCrit) {
      result += "!";
    }
    return result;
  }

  private IEnumerator DoHandleStatusEffects(List<StatusEffect> effects, Panel panel) {
    if (effects.Count == 0) yield break;
    pausedForTriggers = true;
    for(var i = 0; i < effects.Count; i++) {
      while (paused) {
        yield return new WaitForEndOfFrame();
      }

      // CHECKING EFFECTS HERE
      if (effects[i].name == "Counterattack") {
        yield return new WaitForSeconds(battleSpeed * 0.5f);
        var action = Instantiate(Resources.Load<Action>("Actions/Knight/" + "Counterattack"));
        if (effects.Where(e => e.name == "Riposte").Count() > 0) {
          action.additionalDamage.Add(Resources.Load<Action>("Actions/Knight/" + "RiposteCounter"));
        }
          StartCoroutine(DoHeroAction(action, panel as HeroPanel, currentPanel as EnemyPanel, false));
      } else if (effects[i].name == "Arcane Nova") {
        var action = Instantiate(Resources.Load<Action>("Actions/Mage/" + "Arcane Nova"));
        StartCoroutine(DoHeroAction(action, panel as HeroPanel, null, false));
      }


      yield return new WaitForSeconds(battleSpeed * 0.5f);
      if (i == effects.Count - 1) {
        pausedForTriggers = false;
      }
    }
  }

  private IEnumerator DoFlashImage(Image image, Color color) {
    Debug.Log("Flashing image: " + image.name);
    var originalColor = image.color;
    for (int n = 0; n < 2; n++) {
      image.color = color;
      yield return new WaitForSeconds(.05f);
      image.color = originalColor;
      yield return new WaitForSeconds(.05f);
    }
  }

  private IEnumerator DoDelay(float duration = 1f) {
    delaying = true;
    duration *= battleSpeed;
    yield return new WaitForSeconds(duration);
    delaying = false;
  }

  private IEnumerator DoPause(float duration = 1f) {
    paused = true;
    duration *= battleSpeed;
    yield return new WaitForSeconds(duration);
    paused = false;
  }
}
