using UnityEngine.UI;

public class EnemyPanel : Panel {

  public Image targetCursor;

  public void OnClick() {
    BattleManager.instance.UpdatePlayerTarget(this);
  }
}
