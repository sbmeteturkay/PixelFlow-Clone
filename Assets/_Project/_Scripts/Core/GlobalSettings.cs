using System;
using PrimeTween;
using UnityEngine;

public class GlobalSettings : MonoBehaviour
{
    private void Awake()
    {
        PrimeTweenConfig.warnEndValueEqualsCurrent = false;
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
    }
}
