using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WakingSightRules
{
    public static Dictionary<WakingSightMode, bool> isEnabled; // = DefaultRules();

    public static Dictionary<WakingSightMode, bool> DefaultRules()
    {
        Dictionary<WakingSightMode, bool> rules = new Dictionary<WakingSightMode, bool>();
        foreach (WakingSightMode mode in enum.GetValues(WakingSightMode))
        {
            rules.Add(mode, false);
        }

        rules.Add(WakingSightMode.Default);

        return rules;
    }
}

public enum WakingSightMode : int
{
    Default,
    One,
    Two,
    Three,
    Four,
    Final
}