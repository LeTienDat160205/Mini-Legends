using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MatchAnnouncerAudio : MonoBehaviour
{
    public static MatchAnnouncerAudio Instance { get; private set; }

    [SerializeField] private AudioSource source;
    [SerializeField] private AudioClip welcomeSound;
    [SerializeField] private AudioClip playerDeathSound;
    [SerializeField] private AudioClip enemyKillSound1;
    [SerializeField] private AudioClip enemyKillSound2;
    [SerializeField] private AudioClip turretDestroyedSound;
    [SerializeField] private AudioClip turretKilledPlayerSound;
    [SerializeField] private AudioClip victorySound;
    [SerializeField] private AudioClip defeatSound;

    private int killCount;

    private void Awake()
    {
        Instance = this;
        if (source == null) source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
    }

    private void Start() => Play(welcomeSound);

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void PlayPlayerDeath() => Play(playerDeathSound);
    public void PlayTurretDestroyed() => Play(turretDestroyedSound);
    public void PlayTurretKilledPlayer() => Play(turretKilledPlayerSound);
    public void PlayVictory() => Play(victorySound);
    public void PlayDefeat() => Play(defeatSound);

    public void PlayEnemyKill()
    {
        Play(killCount++ % 2 == 0 ? enemyKillSound1 : enemyKillSound2);
    }

    private void Play(AudioClip clip)
    {
        if (source != null && clip != null) source.PlayOneShot(clip);
    }
}
