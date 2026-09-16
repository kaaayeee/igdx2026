using UnityEngine;

[CreateAssetMenu(fileName = "MusicTrack_", menuName = "Game/Music Track")]
public class MusicTrackDataSO : ScriptableObject
{
    [Header("Info")]
    [SerializeField] private string musicName = "Burung Kakak Tua";
    [SerializeField] private SceneReference stageScene;

    [Header("Visual")]
    [SerializeField] private Sprite selectedSprite;
    [SerializeField] private Sprite unselectedSprite;

    public string MusicName => musicName;
    public SceneReference StageScene => stageScene;
    public Sprite SelectedSprite => selectedSprite;
    public Sprite UnselectedSprite => unselectedSprite;
}