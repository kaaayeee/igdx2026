using UnityEngine;
using System;

public static class GameEvent
{
    #region [Audio]
    public static Action<float> onValueChangeMaster;
    public static Action<float> onValueChangeBGM;
    public static Action<float> onValueChangeSFX;
    #endregion
}
