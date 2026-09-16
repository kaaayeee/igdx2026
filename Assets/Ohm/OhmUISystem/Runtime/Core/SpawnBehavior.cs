namespace Ohm.UISystem
{
    /// <summary>When a registered UI prefab is instantiated by the UIManager.</summary>
    // Values are explicit and must not be renumbered — they are serialized on every UI prefab.
    // 0 was the removed BakeInEditor; baking is now done by listing a scene instance on a
    // UIBakedHandler. UIBase.OnValidate heals prefabs still holding 0.
    public enum SpawnBehavior
    {
        PrewarmOnAwake = 1,
        LazyLoad = 2
    }
}
