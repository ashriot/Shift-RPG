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

  [Header("Prefabs")]
  public PopupText popup;
  public PopupText damageText;
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
  public bool delaying;
  public List<Unit> combatants = new List<Unit>();
  public Unit currentCombatant;
  public Enemy playerTarget;
  public Hero enemyTarget;

  [Header("Text colors")]
  public Color red;
  public Color blue;
  public Color yellow;

  private const float INITIATIVE_GROWTH = .05f;

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
    // TEMPORARY BATTLE START
    var enemyName = "Bat";
    var newEnemy = Instantiate(Resources.Load<Enemy>("Enemies/" + enemyName));
    enemyLoadList.Add(newEnemy);
    var newEnemy2 = Instantiate(Resources.Load<Enemy>("Enemies/" + enemyName));
    enemyLoadList.Add(newEnemy2);
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
      panel.hpFillImage.fillAmount = panel.hero.hpPercent;
      panel.currentHp.text = panel.hero.hp.ToString();
      panel.currentArmor.text = (panel.hero.armorCurrent / 10).ToString();
      var crystals = panel.hero.mpCurrent / 10;
      for (var m = 0; m < panel.crystals.Length; m++) {
        if (m >= crystals) {
          panel.crystals[m].gameObject.SetActive(false);
          continue;
        }
        panel.crystals[m].gameObject.SetActive(true);
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
        panel.currentArmor.text = panel.enemy.armorCurrent.ToString();
        panel.currentMp.text = panel.enemy.mpCurrent.ToString();
      }
    }

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
    if (!battleActive || !battleWaiting || delaying) { return; }
    // These values must be TRUE to continue EXCEPT delaying
    battleWaiting = false;

    // check status effects

    if (combatants.All(x => x.isDead && !x.isPlayer)) return;

    if (currentCombatant.isPlayer) {
      // Debug.Log("Player turn");
      // PLAYER TURN
      var hero = currentCombatant as Hero;
      var panel = heroPanels.Where(hp => hp.hero == hero).Single();
      hero.mpCurrent += 10;
      UpdateUi();
      MoveHeroPanel(panel, true);
      SlideHeroMenus(hero);

    } else {
      // Debug.Log("Enemy turn");
      // ENEMY TURN
      var taunters = heroes.Where(h => h.isTaunting).ToList();
      if (taunters.Count > 0) {
        enemyTarget = taunters.First();
      } else {
        // random target
        var random = Random.Range(0, heroes.Count);
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
      panel.currentJobName.text = panel.hero.currentJob.name;
      panel.updateHpBar = true;
      panel.currentHp.text = panel.hero.hpCurrent.ToString();
      panel.currentArmor.text = panel.hero.armorCurrent.ToString();
      var crystals = panel.hero.mpCurrent / 10;
      for (var m = 0; m < panel.crystals.Length; m++) {
        if (m >= crystals) {
          panel.crystals[m].gameObject.SetActive(false);
          continue;
        }
        panel.crystals[m].gameObject.SetActive(true);
      }

      foreach(var buff in heroes[i].buffs) {
        // if (buff.name == panel.buffs.)
      }
    }

    for(var i = 0; i < enemies.Count; i++) {
      var panel = enemyPanels.Where(ep => ep.enemy == enemies[i]).Single();
      panel.updateHpBar = true;
      panel.currentArmor.text = panel.enemy.armorCurrent.ToString();
      panel.currentMp.text = panel.enemy.mpCurrent.ToString();
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

  private void MoveHeroPanel(Panel panel, bool up) {
    // Debug.Log("Starting pos: " + panel.transform.localPosition);
    var distance = 40f * (up ? 1f : -1f);
    panel.Move(distance, 0.25f * battleSpeed);
  }

  private void SlideHeroMenus(Hero hero) {
    DisplayHeroMenus(hero);
    var endPos = actionMenu.transform.position + new Vector3(0f, 160f, 0f);
    StartCoroutine(DoMoveTo(actionMenu.transform, actionMenu.gameObject.transform.position, endPos, battleSpeed / 12f));
  }

  private IEnumerator DoMoveTo(Transform tform, Vector3 startPos, Vector3 endPos, float time) {
    float elapsedTime = 0;
    var startSize = shiftMenu.GetComponent<RectTransform>().sizeDelta;
    var endSize = new Vector2(1250f, startSize.y);
    // Debug.Log("Move to: " + startSize + " -> " + endSize);

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

    if (hero.jobs.Length > 1) {
      shiftMenu.GetComponent<RectTransform>().sizeDelta = shiftMenu.initialSize;
      shiftMenu.gameObject.SetActive(true);
      shiftMenu.nameL.text = hero.jobs[1].name;
      shiftMenu.jobIconL.sprite = hero.jobs[1].jobIcon;
      shiftMenu.colorL.color = hero.jobs[1].jobColor;
      if (hero.jobs.Length == 2) {
        shiftMenu.nameR.text = hero.jobs[1].name;
        shiftMenu.jobIconR.sprite = hero.jobs[1].jobIcon;
        shiftMenu.colorR.color = hero.jobs[1].jobColor;
      }
      else {
        shiftMenu.nameR.text = hero.jobs[2].name;
        shiftMenu.jobIconR.sprite = hero.jobs[2].jobIcon;
        shiftMenu.colorR.color = hero.jobs[2].jobColor;
      }
    }
    else {
      shiftMenu.gameObject.SetActive(false);
    }
    actionMenu.gameObject.SetActive(true);

  }

  private void ShakePanel(Panel panel, float intensity) {
    panel.shaker.Shake(intensity, 0.5f * battleSpeed);
  }

  private void UpdatePlayerTarget() {
    if (playerTarget == null) {
      playerTarget = combatants.Where(c => !c.isPlayer).Cast<Enemy>().First();
    }

    foreach (var panel in enemyPanels) {
      if (panel.enemy == playerTarget) {
        panel.targetCursor.gameObject.SetActive(true);
      } else {
        panel.targetCursor.gameObject.SetActive(false);
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
        Instantiate(popup, position, panel.gameObject.transform.rotation, panel.transform).DisplayMessage("Not enough Mana!", battleSpeed * .8f, Color.white, false);
        return;
      }
      HeroAction(actionToExecute);
    } else {
      Debug.Log("action is null!");
    }
  }

  public void HeroAction(Action action) {
    actionMenu.gameObject.SetActive(false);
    shiftMenu.gameObject.SetActive(false);
    Debug.Log("Executing: " + action.name);
    StartCoroutine(DoHeroAction(action));
  }

  public IEnumerator DoHeroAction(Action action) {
    var panel = heroPanels.Where(hp => hp.hero == currentCombatant).Single();
    panel.hero.mpCurrent -= action.mpCost;
    UpdateUi();

    var position = panel.gameObject.transform.position;
    position.y += panel.GetComponent<RectTransform>().rect.height;
    
    Instantiate(popup, position, panel.gameObject.transform.rotation, canvas.transform).DisplayMessage(action.name, battleSpeed * .8f, Color.white, false);
    yield return new WaitForSeconds(battleSpeed / 4f);

    ExecuteActionEffects(action);

    yield return new WaitForSeconds(battleSpeed / 4f);

    HandleActionTarget(action);

    CalculateSpeedTicks(currentCombatant, action.delay);
    MoveHeroPanel(panel, false);
    StartCoroutine(DoDelay());
    NextTurn();
  }

  private void HandleActionTarget(Action action) {
    var enemyPanel = enemyPanels.Where(ep => ep.enemy == playerTarget).Single();
    var dealsDamage = false;
    switch (action.targetType) {
      case TargetTypes.OneEnemy:
        dealsDamage = action.Execute(currentCombatant, playerTarget);
        if (dealsDamage) {
          HeroDealDamage(action, playerTarget);
          ShakePanel(enemyPanel, 30f);
        }
        break;
      case TargetTypes.AllEnemies:
        dealsDamage = action.Execute(currentCombatant, playerTarget);
        if (dealsDamage) {
          foreach(var panel in enemyPanels.Where(ep => ep.gameObject.activeInHierarchy)) {
            HeroDealDamage(action, panel.enemy);
            ShakePanel(panel, 30f);
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
        if (action.buffs.Count > 0) {
          foreach(var buff in action.buffs) {
            currentCombatant.buffs.Add(buff);
          }
        }
        break;
      case TargetTypes.SelfOrAlly:
        break;
      default:
        break;
    }
  }

  public void HeroDealDamage(Action action, Enemy defender) {
    var attacker = currentCombatant as Hero;

    var color = Color.white;
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
    if (action.splitDamage) {
      damage *= action.potency / enemies.Where(e => !e.isDead).Count();
    } else {
      damage *= action.potency;
    }
    var defense = 0f;

    if (action.damageType == DamageTypes.Martial) {
      color = red;
      defense = defender.martialDefense;
    }
    else if (action.damageType == DamageTypes.Ether) {
      color = blue;
      defense = defender.etherDefense;
    } else if (action.damageType == DamageTypes.Piercing) {
      color = yellow;
    }

    // Debug.Log("Damage before def: " + damage + " def: " + defense);

    damage = (damage * (1f - defense));

    // Debug.Log("Damage after def: " + damage);

    defender.hpCurrent -= (int)damage;
    if (defender.hpCurrent < 0) {
      defender.hpCurrent = 0;
    }
    // Debug.Log(defender.name + "'s HP: " + defender.hpCurrent);

    var panel = enemyPanels.Where(ep => ep.enemy == defender).Single();
    var position = panel.image.transform.position;

    Instantiate(damageText, position, panel.gameObject.transform.rotation, canvas.transform).DisplayMessage(damage.ToString("N0"), battleSpeed * 0.8f, color);

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

    Debug.Log("Executing: " + action.name);
    StartCoroutine(DoEnemyAction(action));
  }

  private IEnumerator DoEnemyAction(Action action) {
    yield return new WaitForSeconds(battleSpeed / 5f);
    var panel = enemyPanels.Where(ep => ep.enemy == currentCombatant).Single();
    var position = panel.gameObject.transform.localPosition;
    Debug.Log("Enemy pos: " + position);
    position.y += panel.GetComponent<RectTransform>().rect.height * 2;

    var popupText = Instantiate(popup, panel.transform, false);
    popupText.transform.localPosition += new Vector3(0f, 75f, 0f);
    popupText.DisplayMessage(action.name, battleSpeed * 0.8f, Color.white, false);
    yield return new WaitForSeconds(battleSpeed / 2f);

    ExecuteActionEffects(action);

    EnemyDealDamage(action);
    var heroPanel = heroPanels.Where(hp => hp.hero == enemyTarget).Single();
    ShakePanel(heroPanel, 30f);
    yield return new WaitForSeconds(battleSpeed);

    CalculateSpeedTicks(currentCombatant, action.delay);
    // Debug.Log("Enemy turn done.");
    NextTurn();
  }

  private void EnemyDealDamage(Action action) {
    // Debug.Log("Enemy dealing damage to " + enemyTarget.heroName);
    var attacker = currentCombatant as Enemy;
    var defender = enemyTarget;
    var color = Color.white;
    var power = 0f;

    if (action.powerType == PowerTypes.Attack) {
      power = attacker.attack;
    } else if (action.powerType == PowerTypes.Willpower){
      power = attacker.willpower;
    } else {
      Debug.LogError("Missing power type!");
    }
    
    var damage = power;
    damage *= action.potency;
    var defense = 0f;

    if (action.damageType == DamageTypes.Martial) {
      color = red;
      defense = defender.currentJob.martialDefense;
    } else if (action.damageType == DamageTypes.Ether) {
      color = blue;
      defense = defender.currentJob.etherDefense;
    }

    // Debug.Log("Damage before def: " + damage + " def: " + defense);

    damage = (damage * (1f - defense));

    // Debug.Log("Damage after def: " + damage);

    defender.hpCurrent -= (int)damage;
    if (defender.hpCurrent < 0) {
      defender.hpCurrent = 0;
    }
    // Debug.Log(defender.name + "'s HP: " + defender.hpCurrent);

    var panel = heroPanels.Where(hp => hp.hero == defender).Single();
    var position = panel.gameObject.transform.position;
    Instantiate(damageText, position, panel.gameObject.transform.rotation, canvas.transform).DisplayMessage(damage.ToString("N0"), battleSpeed * 0.8f, color);

    UpdateUi();
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

}
