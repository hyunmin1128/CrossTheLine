using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrossTheLine
{
    // Scene-owned stage objects replace the former Resources prefab bank.
    public sealed class JourneySceneStages : MonoBehaviour
    {
        [Serializable] public class Stage { public int chapter, step; public GameObject root; }
        public List<Stage> stages = new List<Stage>();
    }
}
