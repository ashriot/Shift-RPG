using UnityEngine.UI;

public class EnemyPanel : Panel {

  public Image targetCursor;
  public Enemy enemy;

  public void OnClick() {
    BattleManager.instance.UpdatePlayerTarget(enemy);
  }
}
