using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(BattleManager))]
public class BattleManagerEditor : Editor {

    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        BattleManager battleManager = (BattleManager)target;
        if(GUILayout.Button("Start Battle")) {
          var enemyName = "Goblin";
          var newEnemy = Instantiate(Resources.Load<Enemy>("Enemies/" + enemyName));
          battleManager.enemyLoadList.Add(newEnemy);
          battleManager.InitializeBattle();
        }
    }
}