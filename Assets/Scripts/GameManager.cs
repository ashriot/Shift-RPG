using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {

  public static GameManager instance;

  public List<Hero> heroes;
  public List<Hero> heroRefs;

  private void Awake() {
    instance = this;
    DontDestroyOnLoad(gameObject);
  }

  private void Start() {
    foreach(var hero in heroRefs) {
      heroes.Add(Instantiate(hero));
    }
  }


}
