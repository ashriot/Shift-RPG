using UnityEngine;
using UnityEngine.UI;

public class SkillsPanel : MonoBehaviour {

  public Hero currentHero;

  public Image tooltipImage;
  public Text tooltipHeaderText, tooltipDescText, tooltipXpCost, tooltipButtonText;

  public Image leftJobIcon, currentJobIcon, rightJobIcon;
  public Text className, jobName;

  public Color jobColor;

  public SkillButton[] skillButtons;
  
  void Start() {
    
  }

  public void Initialize() {

  }

}
