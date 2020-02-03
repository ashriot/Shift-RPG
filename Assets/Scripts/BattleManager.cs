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
  public List<Hero> heroes = new List<Hero>();
  public List<Enemy> enemies = new List<Enemy>();
  public List<Enemy> enemyLoadList = new List<Enemy>();

  [Header("Control Vars")]
  public float battleSpeed;
  public bool battleActive;
  public bool battleWaiting;
  public bool choosingEnemyTarget;
  public bool choosingAllyOnlyTarget;
  public bool choosingSelfOrAllyTarget;
  public bool delaying;
  public bool paused;
  public bool pausedForTriggers;
  public List<Unit> combatants = new List<Unit>();
  public Unit currentCombatant;
  public Enemy playerTarget;
  public Hero enemyTarget;

  [Header("Text colors")]
  public Color orange;
  public Color purple;
  public Color yellow;

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
    HideTooltip();
    // TEMPORARY BATTLE START
    var enemyName = "Bat";
    var newEnemy = Instantiate(Resources.Load<Enemy>("Enemies/" + enemyName));
    enemyLoadList.Add(newEnemy);
    enemyName = "Tree";
    var newEnemy2 = Instantiate(Resources.Load<Enemy>("Enemies/" + enemyName));
    enemyLoadList.Add(newEnemy2);
    enemyName = "Wolf";
    var newEnemy3 = Instantiate(Resources.Load<Enemy>("Enemies/" + enemyName));
    enemyLoadList.Add(newEnemy3);
    InitializeBattle();
  }

  public void InitializeBattle() {
    if (battleActive) return;
    AudioManager.instance.PlayBgm("battle-conflict");
    battleActive = true;

    combatants.AddRange(GameManager.instance.heroes);
    combatants.AddRange(enemyLoadList);

    heroes = combatants.Where(c => c.isPlayer).Cast<Hero>().ToList();
    enemies = combatants.Where(c => !c.isPlayer).Cast<Enemy>().ToList();

    for (var i = 0; i < heroPanels.Length; i++) {
      if (i >= heroes.Count) { 
        heroPanels[i].gameObject.SetActive(false);
        heroPanels[i].GetComponent<RectTransform>().gameObject.SetActive(false);
        continue;
      }

      var panel = heroPanels[i];

      panel.gameObject.SetActive(true);
      panel.GetComponent<RectTransform>().gameObject.SetActive(true);
      panel.hero = heroes[i];
      panel.jobColor.color = panel.hero.currentJob.jobColor;
      panel.jobIcon.sprite = panel.hero.currentJob.jobIcon;
      panel.hero.hpCurrent = panel.hero.hp;
      panel.hero.armorCurrent = panel.hero.armor;
      panel.hero.mpCurrent = panel.hero.mp;
      panel.heroName.text = panel.hero.heroName;
      panel.currentJobName.text = panel.hero.currentJob.name.ToUpper();
      panel.hero.martialDefense = panel.hero.currentJob.martialDefense;
      panel.hero.etherDefense = panel.hero.currentJob.etherDefense;
      panel.hpFillImage.fillAmount = panel.hero.hpPercent;
      panel.currentHp.text = panel.hero.hp.ToString();
      var crystals = panel.hero.mpCurrent / 10;
      for (var m = 0; m < panel.crystals.Length; m++) {
        if (m >= crystals) {
          panel.crystals[m].gameObject.SetActive(false);
          continue;
        }
        panel.crystals[m].gameObject.SetActive(true);
      }
      var shields = panel.hero.armorCurrent / 10;
      for (var m = 0; m < panel.shields.Length; m++) {
        if (m >= shields) {
          panel.shields[m].gameObject.SetActive(false);
          continue;
        }
        panel.shields[m].gameObject.SetActive(true);
      }
    }

    for(var i = 0; i < enemyPanels.Length; i++) {
      if (i >= enemies.Count) { 
        enemyPanels[i].gameObject.SetActive(false);
        continue;
      } else {
        var panel = enemyPanels[i];
        panel.gameObject.SetActive(true);
        panel.enemy = enemies[i];
        panel.image.sprite = panel.enemy.sprite;
        panel.enemy.hpCurrent = panel.enemy.hp;
        panel.enemy.armorCurrent = panel.enemy.armor;
        panel.enemy.mpCurrent = panel.enemy.mp;
        panel.hpFillImage.fillAmount = panel.enemy.hpPercent;
        var crystals = panel.enemy.mpCurrent / 10;
        for (var m = 0; m < panel.crystals.Length; m++) {
          if (m >= crystals) {
            panel.crystals[m].gameObject.SetActive(false);
            continue;
          }
          panel.crystals[m].gameObject.SetActive(true);
        }
        var shields = panel.enemy.armorCurrent / 10;
        for (var m = 0; m < panel.shields.Length; m++) {
          if (m >= shields) {
            panel.shields[m].gameObject.SetActive(false);
            continue;
          }
          panel.shields[m].gameObject.SetActive(true);
        }
      }
    }
    // initial target
    playerTarget = combatants.Where(c => !c.isPlayer && !c.isDead).Cast<Enemy>().First();

    StartCoroutine(DoDelay(0.5f));
    SetInitialTicks();
    NextTurn();
  }

  private void Update() {
    foreach(var panel in heroPanels) {
      if (panel.updateHpBar) {
        SmoothHpBar(panel, panel.hero.hpPercent);
      }
    }
    foreach (var panel in enemyPanels) {
      if (panel.updateHpBar) {
        SmoothHpBar(panel, panel.enemy.hpPercent);
      }
    }
  }

  void FixedUpdate() {
    if (!battleActive || !battleWaiting || delaying || paused || pausedForTriggers) { return; }
    // These values must be TRUE to continue EXCEPT delaying
    battleWaiting = false;

    // check status effects

    if (combatants.All(x => x.isDead && !x.isPlayer)) return;

    currentCombatant.mpCurrent += currentCombatant.mpRegen;
    UpdateUi();

    if (currentCombatant.isPlayer) {
      // Debug.Log("Player turn");
      // PLAYER TURN
      var hero = currentCombatant as Hero;
      var panel = heroPanels.Where(hp => hp.hero == hero).Single();
      MovePanel(panel, true);
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
    actionMenu.gameObject.SetActive(false);
    shiftMenu.gameObject.SetActive(false);
    // check for battle end

    UpdatePlayerTarget();

    CountdownTicks();

  }

  private void UpdateUi() {
    for (var i = 0; i < heroes.Count; i++) {
      var panel = heroPanels.Where(hp => hp.hero == heroes[i]).Single();
      if (panel.hero.armorCurrent <= 0 && !panel.hero.isArmorBroke) {
        panel.hero.isArmorBroke = true;
        ShakePanel(panel, SHAKE_INTENSITY * 1.5f, 1f);
        AudioManager.instance.PlaySfx("damage02");
        Debug.Log("Armor Break!");
        var position = panel.gameObject.transform.position;
        position.y += panel.GetComponent<RectTransform>().rect.height;
        Instantiate(popup, position, panel.gameObject.transform.rotation, canvas.transform).DisplayMessage("Armor Break!", battleSpeed * .8f, Color.white, false);
        StartCoroutine(DoFlashImage(panel.image, Color.red));
        StartCoroutine(DoPause());
      }
      panel.currentJobName.text = panel.hero.currentJob.name.ToUpper();
      panel.jobColor.color = panel.hero.currentJob.jobColor;
      panel.jobIcon.sprite = panel.hero.currentJob.jobIcon;
      panel.hero.martialDefense = panel.hero.currentJob.martialDefense;
      panel.hero.etherDefense = panel.hero.currentJob.etherDefense;
      if (!heroes[i].buffs.Contains(panel.hero.currentJob.trait)) {
        panel.hero.buffs.Add(panel.hero.currentJob.trait);
      }
      panel.updateHpBar = true;
      panel.currentHp.text = panel.hero.hpCurrent.ToString();
      var crystals = panel.hero.mpCurrent / 10;
      for (var m = 0; m < panel.crystals.Length; m++) {
        if (m >= crystals) {
          panel.crystals[m].gameObject.SetActive(false);
          continue;
        }
        panel.crystals[m].gameObject.SetActive(true);
      }
      var shields = panel.hero.armorCurrent / 10;
      for (var m = 0; m < panel.shields.Length; m++) {
        if (m >= shields) {
          panel.shields[m].gameObject.SetActive(false);
          continue;
        }
        panel.shields[m].gameObject.SetActive(true);
      }

      foreach(var buff in heroes[i].buffs) {
        // if (buff.name == panel.buffs.)
      }
    }

    for(var i = 0; i < enemies.Count; i++) {
      var panel = enemyPanels.Where(ep => ep.enemy == enemies[i]).Single();
      if (panel.enemy.armorCurrent <= 0 && !panel.enemy.isArmorBroke) {
        panel.enemy.isArmorBroke = true;
        ShakePanel(panel, SHAKE_INTENSITY * 1.5f, 1f);
        AudioManager.instance.PlaySfx("damage02");
        Debug.Log("Armor Break!");
        var popupText = Instantiate(popup, panel.transform, false);
        popupText.transform.localPosition += new Vector3(0f, 75f, 0f);
        popupText.DisplayMessage("Armor Break!", battleSpeed, Color.white, false);
        StartCoroutine(DoFlashImage(panel.image, Color.red));
        StartCoroutine(DoPause());
      }
      if (panel.enemy.isDead) {
        panel.gameObject.SetActive(false);
      } else {
        panel.updateHpBar = true;
        var crystals = panel.enemy.mpCurrent / 10;
        for (var m = 0; m < panel.crystals.Length; m++) {
          if (m >= crystals) {
            panel.crystals[m].gameObject.SetActive(false);
            continue;
          }
          panel.crystals[m].gameObject.SetActive(true);
        }
        var shields = panel.enemy.armorCurrent / 10;
        for (var m = 0; m < panel.shields.Length; m++) {
          if (m >= shields) {
            panel.shields[m].gameObject.SetActive(false);
            continue;
          }
          panel.shields[m].gameObject.SetActive(true);
        }
      }
    }
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
      shiftMenu.traitColor.color = hero.currentJob.jobColor;
      shiftMenu.traitName.text = hero.currentJob.trait.name;
      shiftMenu.traitIcon.sprite = hero.currentJob.trait.sprite;
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
      shiftMenu.nameR.text = jobR.name;
      shiftMenu.jobIconR.sprite = jobR.jobIcon;
      shiftMenu.colorR.color = jobR.jobColor;
      shiftMenu.jobIdR = jobRIndex;
    }
    else {
      shiftMenu.gameObject.SetActive(false);
    }
    actionMenu.gameObject.SetActive(true);

  }

  private void ShakePanel(Panel panel, float intensity, float duration = 0.5f) {
    panel.shaker.Shake(intensity, duration * battleSpeed);
  }

  public void UpdatePlayerTarget(Enemy enemy = null) {
    // Debug.Log("Updating player target");
    if (enemy != null) {
      playerTarget = enemy;
    } else if (playerTarget.isDead) {
      playerTarget = combatants.Where(c => !c.isPlayer && !c.isDead).Cast<Enemy>().First();
    }

    foreach (var panel in enemyPanels) {
      if (panel.enemy == playerTarget) {
        panel.targetCursor.GetComponentInParent<Button>().Select();
      } else {
        panel.targetCursor.gameObject.SetActive(true);
      }
    }
  }

  private void CalculateSpeedTicks(Unit combatant, float delay) {
    var inverse = Mathf.Pow((1 + INITIATIVE_GROWTH), combatant.speed);
    var result = 1000 / inverse;
    result *= delay;
    combatant.ticks = (int)result;
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
        if (!combatant.isDead && combatant.ticks == 0) {
          currentCombatant = combatant;
          battleWaiting = true;
          return;
        } else {
          // Debug.Log(combatant.name + ": " + combatant.ticks);
          combatant.ticks--;
        }
      }
    }
  }

  public void ClickActionButton(int buttonId) {
    Action actionToExecute = null;
    var actionName = actionMenu.actionButtons[buttonId].nameText.text;
    var hero = currentCombatant as Hero;
    foreach(var action in hero.currentJob.actions) {
      if (action.name == actionName) {
        actionToExecute = action;
        break;
      }
    }
    if (actionToExecute != null) {
      if (actionToExecute.mpCost > hero.mpCurrent) {
        var panel = heroPanels.Where(hp => hp.hero == currentCombatant).Single();
        var position = panel.gameObject.transform.position;
        position.y += panel.GetComponent<RectTransform>().rect.height;
        Instantiate(popup, position, panel.gameObject.transform.rotation, panel.transform).DisplayMessage("Not enough Mana! " + hero.mpCurrent, battleSpeed * .8f, Color.white, false);
        return;
      }
      HeroAction(actionToExecute);
    } else {
      Debug.Log("action is null!");
    }
  }

  public void ClickShiftButton(int buttonId) {
    StartCoroutine(DoShiftJobs(buttonId));
  }

  public IEnumerator DoShiftJobs(int jobId) {
    var hero = currentCombatant as Hero;
    SlideHeroMenus(hero, false);
    hero.currentJob = hero.jobs[jobId];
    yield return new WaitForSeconds(battleSpeed);

    SlideHeroMenus(hero, true);
    UpdateUi();
  }

  public void HeroAction(Action action) {
    // actionMenu.gameObject.SetActive(false);
    // shiftMenu.gameObject.SetActive(false);
    SlideHeroMenus(currentCombatant as Hero, false);
    Debug.Log("Executing: " + action.name);
    StartCoroutine(DoHeroAction(action));
  }

  public IEnumerator DoHeroAction(Action action, Hero hero = null, Enemy target = null, bool nextTurn = true) {
    if (target != null && target.isDead) yield break;
    var panel = heroPanels.Where(hp => hp.hero == (hero ?? currentCombatant)).Single();
    panel.hero.mpCurrent -= action.mpCost;
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
      HandleActionTarget(action, hero, target);
      if (action.hits > 1) {
        yield return new WaitForSeconds(battleSpeed / 3f);
      }
      if (action.additionalDamage.Count > 0) {
        foreach (var add in action.additionalDamage) {
          yield return new WaitForSeconds(battleSpeed / 3f);
          HeroDealDamage(add, target ?? playerTarget, hero);
        }
      }
    }

    CalculateSpeedTicks(currentCombatant, action.delay);
    if (panel.panelMoved) {
      MovePanel(panel, false);
    }
    if (nextTurn) {
      Debug.Log("DoDelay");
      StartCoroutine(DoDelay());
      NextTurn();
    }
  }

  private void HandleActionTarget(Action action, Hero hero = null, Enemy target = null) {
    var enemyPanel = enemyPanels.Where(ep => ep.enemy == (target ?? playerTarget)).Single();
    var dealsDamage = false;
    switch (action.targetType) {
      case TargetTypes.OneEnemy:
        dealsDamage = action.Execute(currentCombatant, playerTarget);
        if (dealsDamage) {
          HeroDealDamage(action, target ?? playerTarget, hero);
        }
        break;
      case TargetTypes.AllEnemies:
        dealsDamage = action.Execute(currentCombatant, playerTarget);
        if (dealsDamage) {
          foreach(var panel in enemyPanels.Where(ep => ep.gameObject.activeInHierarchy)) {
            HeroDealDamage(action, target ?? panel.enemy, hero);
            if (action.additionalDamage.Count > 0) {
              foreach (var add in action.additionalDamage) {
                HeroDealDamage(add, target ?? playerTarget, hero);
              }
            }
          }
        }
        break;
      case TargetTypes.EntireParty:
        break;
      case TargetTypes.OneAlly:
        break;
      case TargetTypes.OtherAllies:
        break;
      case TargetTypes.Self:
        break;
      case TargetTypes.SelfOrAlly:
        break;
      default:
        break;
    }
    // BUFFS
    
    if (action.buffs.Count > 0) {
      foreach(var buff in action.buffs) {
        Debug.Log("Added buff: " + buff.name);
        if (!currentCombatant.buffs.Contains(buff)) {
          currentCombatant.buffs.Add(buff);
        }
      }
    }
  }

  public void HeroDealDamage(Action action, Enemy defender, Hero hero = null) {
    var attacker = hero ?? currentCombatant as Hero;
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
    if (critRoll <= attacker.crit) {
      isCrit = true;
    }

    var damage = CalculateDamage(attacker, defender, action, isCrit);

    var panel = enemyPanels.Where(ep => ep.enemy == defender).Single();
    var position = panel.image.transform.position;

    Instantiate(damagePopup, position, panel.gameObject.transform.rotation, canvas.transform).DisplayMessage(damage, battleSpeed * 0.8f, color, isCrit, true);
    ShakePanel(panel, SHAKE_INTENSITY);

    UpdateUi();
  }

  public void ExecuteActionEffects(Action action) {
    AudioManager.instance.PlaySfx(action.sfxName);
  }

  public void EnemyAction() {
    var panel = enemyPanels.Where(ep => ep.enemy == currentCombatant).Single();
    StartCoroutine(DoFlashImage(panel.image, Color.clear));

    var enemy = currentCombatant as Enemy;

    var rand = Random.Range(0, enemy.actions.Count);

    var action = enemy.actions[rand];

    // Debug.Log("Executing: " + action.name);
    StartCoroutine(DoEnemyAction(action));
  }

  private IEnumerator DoEnemyAction(Action action) {
    yield return new WaitForSeconds(battleSpeed / 5f);
    var panel = enemyPanels.Where(ep => ep.enemy == currentCombatant).Single();
    var position = panel.gameObject.transform.localPosition;
    // Debug.Log("Enemy pos: " + position);
    position.y += panel.GetComponent<RectTransform>().rect.height * 2;

    var popupText = Instantiate(popup, panel.transform, false);
    popupText.transform.localPosition += new Vector3(0f, 75f, 0f);
    popupText.DisplayMessage(action.name, battleSpeed * 0.8f, Color.white, false);

    panel.enemy.mpCurrent -= action.mpCost;
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
      foreach(var hero in heroes) {
        for (var h = 0; h < action.hits; h++){
          while (paused || delaying || pausedForTriggers) {
            yield return new WaitForEndOfFrame();
          }
          ExecuteActionEffects(action);
          EnemyDealDamage(action, hero);
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
    CalculateSpeedTicks(currentCombatant, action.delay);
    Debug.Log("Enemy turn done.");
    NextTurn();
  }

  private void EnemyDealDamage(Action action, Hero target = null) {
    // Debug.Log("Enemy dealing damage to " + enemyTarget.heroName);
    var attacker = currentCombatant as Enemy;
    var defender = target ?? enemyTarget;
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
    Debug.Log("Crit Roll: " + critRoll + "/" + attacker.crit);
    if (critRoll <= attacker.crit) {
      isCrit = true;
    }

    var damage = CalculateDamage(attacker, defender, action, isCrit);

    var panel = heroPanels.Where(hp => hp.hero == defender).Single();
    var position = panel.gameObject.transform.position + new Vector3(0f, 50f, 0f);
    Instantiate(damagePopup, position, panel.gameObject.transform.rotation, canvas.transform).DisplayMessage(damage, battleSpeed * 0.8f, color, isCrit, true);
    ShakePanel(panel, SHAKE_INTENSITY);

    var onHitEffects = defender.buffs.Where(b => b.trigger == Triggers.BeingHit).ToList();
    onHitEffects.AddRange(defender.debuffs.Where(b => b.trigger == Triggers.BeingHit).ToList());
    if (onHitEffects.Count > 0) {
        StartCoroutine(DoHandleStatusEffects(onHitEffects, defender));
    }

    UpdateUi();
  }

  private string CalculateDamage(Unit attacker, Unit defender, Action action, bool isCrit) {
    var power = 0f;

    if (action.powerType == PowerTypes.Attack) {
      power = attacker.attack;
    }
    else if (action.powerType == PowerTypes.Willpower) {
      power = attacker.willpower;
    }
    else {
      Debug.LogError("Missing power type!");
    }

    var damage = power;
    var potency = action.potency;

    if (isCrit) {
      damage += attacker.surge;
    }
    
    // TODO: Fix
    if (action.name == "Mana Bolt") {
      potency *= defender.mpCurrent;
      Debug.Log("Mana Bolt potency: " + potency);
    } else if (action.name == "Disintegrate") {
      potency *= (1 + (1 - defender.hpPercent));
    }

    if (action.splitDamage) {
      potency /= enemies.Where(e => !e.isDead).Count();
    }

    damage *= potency;
    var defense = 0f;

    if (action.damageType == DamageTypes.Martial) {
      defense = (defender.isArmorBroke ? 0 : defender.martialDefense);
    }
    else if (action.damageType == DamageTypes.Ether) {
      defense = (defender.isArmorBroke ? 0 : defender.etherDefense);
    }
    // Debug.Log("Damage before def: " + damage + " def: " + defense);

    damage = (damage * (1f - defense));

    if (defender.isArmorBroke) {
      damage *= attacker.breakBonus;
    }

    // Debug.Log("Damage after def: " + damage);

    defender.hpCurrent -= (int)damage;
    if (defender.hpCurrent < 0) {
      defender.hpCurrent = 0;
    }

    if (!defender.isArmorBroke && action.removesArmor) {
      defender.armorCurrent -= 10;
      Debug.Log("Current Armor: " + defender.armorCurrent);
      if (defender.armorCurrent <= 0) {
        defender.armorCurrent = 0;
      }
    }
    // Debug.Log(defender.name + "'s HP: " + defender.hpCurrent);

    var result = ((int)damage).ToString("N0");
    if (isCrit) {
      result += "!";
    }
    return result;
  }

  private IEnumerator DoHandleStatusEffects(List<StatusEffect> effects, Unit unit) {
    if (effects.Count == 0) yield break;
    pausedForTriggers = true;
    for(var i = 0; i < effects.Count; i++) {
      while (paused) {
        yield return new WaitForEndOfFrame();
      }
      if (effects[i].name == "Counterattack") {
        yield return new WaitForSeconds(battleSpeed * 0.5f);
        var action = Instantiate(Resources.Load<Action>("Actions/Knight/" + "Counterattack"));
        if (effects.Where(e => e.name == "Riposte").Count() > 0) {
          action.additionalDamage.Add(Resources.Load<Action>("Actions/Knight/" + "RiposteCounter"));
        }
        StartCoroutine(DoHeroAction(action, unit as Hero, currentCombatant as Enemy, false));
      }
      yield return new WaitForSeconds(battleSpeed * 0.5f);
      if (i == effects.Count - 1) {
        pausedForTriggers = false;
      }
    }
  }

  private IEnumerator DoFlashImage(Image image, Color color) {
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

  public void ShowTooltip() {
    tooltip.gameObject.SetActive(true);
  }
  public void HideTooltip() {
    tooltip.gameObject.SetActive(false);
  }

}
