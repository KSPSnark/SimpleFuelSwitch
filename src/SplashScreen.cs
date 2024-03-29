﻿using System.Collections.Generic;
using UnityEngine;

namespace SimpleFuelSwitch
{
    [KSPAddon(KSPAddon.Startup.Instantly, false)]
    class SplashScreen : MonoBehaviour
    {
        private static float MAX_TIP_TIME = 4; // seconds

        private static readonly string[] NEW_TIPS =
        {
            "Switching Fuel...",
        };

        /// <summary>
        /// Snark's sneaky little way of thanking various users of this mod for helpful contributions.
        /// </summary>
        private static readonly string[] THANK_USERS =
        {
            "TheKurgan",        // bug report for hang-on-startup
            "4x4cheesecake",    // bug report for outdated engineer's report
            "DMagic",           // supplying a bug solution
            "Tyko",             // helping improve documentation
            "JPLRepo",          // advice when tracking down KSP bug
            "theJesuit",        // suggesting variant-switching feature
            "RealKerbal3x",     // feature idea: allow "no resources" as a switchable option
            "Behemot",          // pointing out an issue with hidden fuel tanks in default config
            "DStaal",           // helpful suggestions
            "theonegalen",      // nice suggestion about LFO tank variants
            "ExtremeSquared",   // bug report for v1.1.2
            "Tonka Crash",      // bug report for v1.3
            "JPLRepo",          // helpful bug-fixing advice
            "Sqeep91",          // helpfully providing a German translation
            "VTOL Frog",        // spotted that I missed the big-S wing strake
            "Krazy1",           // useful bug report about mod blowing up when misconfigured
            "Vspace"            // helpfully providing a French translation
        };

        internal void Awake()
        {
            LoadingScreen.LoadingScreenState state = FindLoadingScreenState();
            if (state != null)
            {
                InsertTips(state);
                if (state.tipTime > MAX_TIP_TIME) state.tipTime = MAX_TIP_TIME;
            }
        }

        /// <summary>
        /// Finds the loading screen where we want to tinker with the tips,
        /// or null if there's no suitable candidate.
        /// </summary>
        /// <returns></returns>
        private static LoadingScreen.LoadingScreenState FindLoadingScreenState()
        {
            if (LoadingScreen.Instance == null) return null;
            List<LoadingScreen.LoadingScreenState> screens = LoadingScreen.Instance.Screens;
            if (screens == null) return null;
            for (int i = 0; i < screens.Count; ++i)
            {
                LoadingScreen.LoadingScreenState state = screens[i];
                if ((state != null) && (state.tips != null) && (state.tips.Length > 1)) return state;
            }
            return null;
        }

        /// <summary>
        /// Insert our list of tips into the specified loading screen state.
        /// </summary>
        /// <param name="state"></param>
        private static void InsertTips(LoadingScreen.LoadingScreenState state)
        {
            List<string> tipsList = new List<string>();
            tipsList.AddRange(state.tips);
            tipsList.AddRange(NEW_TIPS);
            int numThanks = 1 + (int)Mathf.Sqrt(THANK_USERS.Length);
            System.Random random = new System.Random(System.DateTime.UtcNow.Second);
            for (int i = 0; i < numThanks; ++i)
            {
                tipsList.Add(string.Format("Thanking {0}...", THANK_USERS[random.Next(THANK_USERS.Length)]));
            }
            state.tips = tipsList.ToArray();
        }
    }
}