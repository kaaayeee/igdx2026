using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [System.Serializable]
    public struct TestingStruct
    {
        [Header("ReadOnlyIf Demo")]
        // Default (invert=false): customPosition is read-only when useCustomPosition is TRUE
        // With invert=true: customPosition is read-only when useCustomPosition is FALSE (editable when TRUE)
        public bool useCustomPosition;

        [ReadOnlyIf("useCustomPosition", true)]
        public Vector3 customPosition;
        [SerializeField, ReadOnlyIf("useCustomPosition", false)]
        private Vector3 customPositionPrivate;
    }
    public TestingStruct testingStruct;
    public TestingStruct testingStruct2;
    public string playerName = "Hero";
    [FoldoutGroup("Movement Settings")]
    [SerializeField,ReadOnly]private float moveSpeed = 5f;

    [FoldoutGroup("Movement Settings")]
    public float jumpForce = 7f;

    [FoldoutGroup("Combat Settings")]
    public int attackPower = 10;

    [FoldoutGroup("Combat Settings")]
    public float attackCooldown = 1.5f;

    [FoldoutGroup("Inventory")]
    public string[] items;

    public bool showDebug;

    // [Header("ReadOnlyIf Demo")]
    // // Default (invert=false): customPosition is read-only when useCustomPosition is TRUE
    // // With invert=true: customPosition is read-only when useCustomPosition is FALSE (editable when TRUE)
    // public bool useCustomPosition;

    // [ReadOnlyIf("useCustomPosition", invert: true)]
    // public Vector3 customPosition;
    // [SerializeField, ReadOnlyIf("useCustomPosition", invert: false)]
    // private Vector3 customPositionPrivate;

    // Default: secretCode is read-only when isLocked is TRUE
    public bool isLocked;

    [ReadOnlyIf("isLocked")]
    public string secretCode = "1234";

    [Header("SerializeFieldIf Demo")]
    // Default (invert=false): advancedDamageMultiplier is hidden when showAdvanced is TRUE, visible when FALSE
    // With invert=true: visible when showAdvanced is TRUE, hidden when FALSE
    public bool showAdvanced;

    [SerializeFieldIf("showAdvanced", invert: true)]
    public float advancedDamageMultiplier = 1.5f;

    // Default: overrideValue is hidden when enableOverride is TRUE
    public bool enableOverride;

    [SerializeFieldIf("enableOverride")]
    public int overrideValue = 100;
}
