﻿using UnityEngine;

[CreateAssetMenu(fileName = "StatusEffect", menuName = "StatusEffect")]
public class StatusEffect : ScriptableObject {
  
  public string effectName;
  public string description;
  public int duration;
  public bool fadesOnCasterTrigger;
  public Sprite sprite;
  public StatusEffectTypes statusEffectType;
  public TargetTypes targetType;
  public TriggerTypes activationTrigger;
  public TriggerTypes fadeTrigger;
  public string actionNameToTrigger;
  public Action actionToTrigger;
}

public enum StatusEffectTypes {
  Buff,
  Debuff,
  Trait
}

public enum TriggerTypes {
  StartOfTurn,
  EndOfTurn,
  OnBeingHit,
  OnBeingAttacked,
  OnHit,
  OnAttack,
  AfterAttack,
  OnShift,
  OnStagger,
  OnShiftOrStagger,
  InstantOrConstant,
  AfterBeingAttacked
}
