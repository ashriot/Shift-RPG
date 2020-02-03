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
