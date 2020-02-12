using UnityEngine;

public class AudioManager : MonoBehaviour {

  public static AudioManager instance;

  public AudioSource[] bgms;
  public AudioSource[] sfxs;

  private void Awake() {
    if (instance == null) {
      instance = this;
    }
    else {
      Destroy(gameObject);
      return;
    }
    DontDestroyOnLoad(gameObject);
  }

  public void PlayBgm(string name) {
    Debug.Log("Playing BGM: " + name);
    if (name == "battle-conflict") {
      PlayBgmId(0);
    } else {
      Debug.LogError("BGM named '" + name + "' does not exist!");
    }
  }

  public void PlaySfx(string name) {
    if (name == "sword-unsheathe") {
      sfxs[0].Play();
    } else if (name == "spell") {
      sfxs[1].Play();
    } else if (name == "end_turn") {
      sfxs[2].Play();
    } else if (name == "damage02") {
      sfxs[3].Play();
    } else if (name == "click") {
      sfxs[4].Play();
    } else if (name == "cancel") {
      sfxs[5].Play();
    } else if (name == "shatter") {
      sfxs[6].Play();
    } else if (name == "swish_02") {
      sfxs[7].Play();
    } else if (name == "swish_04") {
      sfxs[8].Play();
    } else if (name == "fwoosh") {
      sfxs[9].Play();
    } else if (name == "trap_00") {
      sfxs[10].Play();
    } else if (name == "trap_02") {
      sfxs[11].Play();
    } else if (name == "swing3") {
      sfxs[12].Play();
    } else if (name == "spell_00") {
      sfxs[13].Play();
    } else if (name == "knife-stab-a1") {
      sfxs[14].Play();
    } else if (name == "pulling-out-knife-a1") {
      sfxs[15].Play();
    } else if (name == "knife-stab-a5") {
      sfxs[16].Play();
    } else if (name == "win1") {
      sfxs[17].Play();
    } else if (name == "spell_weird") {
      sfxs[18].Play();
    } else {
      // Debug.LogError("SFX named '" + name + "' does not exist!");
    }
  }

    public void PlayBgmId(int id) {
      if (!bgms[id].isPlaying) {
        StopMusic();
        if (id < bgms.Length) {
          bgms[id].volume = 0.2f;
          bgms[id].Play();
        }
      }
    }

    public void StopMusic() {
        for (var i = 0; i < bgms.Length; i++) {
            bgms[i].Stop();
        }
    }

    public void Mute(string name) {
        if (name == "blocked") {
            sfxs[1].mute = true;
        }
    }

    public void UnMute(string name) {
        if (name == "blocked") {
            sfxs[1].Stop();
            sfxs[1].mute = false;
        }
    }
}
