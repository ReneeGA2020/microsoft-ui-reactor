// C# port of Meta's Yoga layout engine Config.
// Ported from yoga/config/Config.h, yoga/config/Config.cpp

using Duct.Flex;

namespace Duct.Layout;

/// <summary>
/// Configuration for the Yoga layout engine. Controls experimental features,
/// errata modes, point scale factor, and web defaults.
/// </summary>
public sealed class YogaConfig
{
    private bool _useWebDefaults;
    private YogaErrata _errata = YogaErrata.None;
    private float _pointScaleFactor = 1.0f;
    private uint _version;
    private readonly bool[] _experimentalFeatures = new bool[2]; // ExperimentalFeature count

    private static readonly YogaConfig s_default = new();

    public static YogaConfig Default => s_default;

    public bool UseWebDefaults
    {
        get => _useWebDefaults;
        set => _useWebDefaults = value;
    }

    public float PointScaleFactor
    {
        get => _pointScaleFactor;
        set
        {
            if (_pointScaleFactor != value)
            {
                _pointScaleFactor = value;
                _version++;
            }
        }
    }

    public uint Version => _version;

    public void SetExperimentalFeatureEnabled(YogaExperimentalFeature feature, bool enabled)
    {
        int idx = (int)feature;
        if (_experimentalFeatures[idx] != enabled)
        {
            _experimentalFeatures[idx] = enabled;
            _version++;
        }
    }

    public bool IsExperimentalFeatureEnabled(YogaExperimentalFeature feature)
    {
        return _experimentalFeatures[(int)feature];
    }

    public void SetErrata(YogaErrata errata)
    {
        if (_errata != errata)
        {
            _errata = errata;
            _version++;
        }
    }

    public void AddErrata(YogaErrata errata)
    {
        if (!HasErrata(errata))
        {
            _errata |= errata;
            _version++;
        }
    }

    public void RemoveErrata(YogaErrata errata)
    {
        if (HasErrata(errata))
        {
            _errata &= ~errata;
            _version++;
        }
    }

    public YogaErrata Errata => _errata;

    public bool HasErrata(YogaErrata errata) => (_errata & errata) != YogaErrata.None;

    /// <summary>
    /// Whether changing from oldConfig to newConfig would invalidate cached layouts.
    /// </summary>
    public static bool ConfigUpdateInvalidatesLayout(YogaConfig oldConfig, YogaConfig newConfig)
    {
        return oldConfig._errata != newConfig._errata ||
               oldConfig._pointScaleFactor != newConfig._pointScaleFactor ||
               oldConfig._useWebDefaults != newConfig._useWebDefaults ||
               !ExperimentalFeaturesEqual(oldConfig, newConfig);
    }

    private static bool ExperimentalFeaturesEqual(YogaConfig a, YogaConfig b)
    {
        for (int i = 0; i < a._experimentalFeatures.Length; i++)
        {
            if (a._experimentalFeatures[i] != b._experimentalFeatures[i])
                return false;
        }
        return true;
    }
}
