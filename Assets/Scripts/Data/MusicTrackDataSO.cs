using UnityEngine;

[CreateAssetMenu(fileName = "MusicTrack_", menuName = "Game/Music Track")]
public class MusicTrackDataSO : ScriptableObject
{
    [Header("Info")]
    [SerializeField] private string musicName = "Burung Kakak Tua";
    [SerializeField] private string stageSceneName;

    [Header("Visual")]
    [SerializeField] private Sprite selectedSprite;
    [SerializeField] private Sprite unselectedSprite;

    public string MusicName => musicName;
    public string StageSceneName => stageSceneName;
    public Sprite SelectedSprite => selectedSprite;
    public Sprite UnselectedSprite => unselectedSprite;
}