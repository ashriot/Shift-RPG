﻿using System.Collections;
using UnityEngine;

public class Shaker : MonoBehaviour { 

  private Vector3 initialPos;

  private bool isShaking;

  private float intensity;
  private float shakeLifetime = 0f;

  private void Update() {
    if (shakeLifetime > 0 && !isShaking) {
      StartCoroutine(DoShake());
    }
      
  }

  public void Shake(float intensity, float duration) {
    this.intensity = intensity;
    initialPos = transform.localPosition;
    if (duration > 0) {
        shakeLifetime += duration;
      }
  }

  private IEnumerator DoShake() {
    isShaking = true;

    var startTime = Time.realtimeSinceStartup;
    while (Time.realtimeSinceStartup < startTime + shakeLifetime) {
      var randomPoint = new Vector3(Random.Range(-1f, 1f) * intensity, Random.Range(-1f, 1f) * intensity, initialPos.z);
      Debug.Log("Current Loc: " + transform.localPosition + " " + "Random Pt: " + randomPoint);
      transform.localPosition = randomPoint;
      yield return null;
    }

    shakeLifetime = 0f;
    transform.localPosition = initialPos;
    isShaking = false;
  }
}
