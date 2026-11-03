using System;
using UnityEngine;

namespace CrossTheLine
{
    [Serializable] public class StoryBook { public Chapter[] chapters; }
    [Serializable] public class Chapter
    {
        public string title, location, biome, scripture, introduction, ending;
        public int minutes;
        public StoryStep[] steps;
        public string[] discoveries;
    }
    [Serializable] public class StoryStep
    {
        public string actor, person, objective, kind;
        public string[] dialogue;
        public string[] choices;
        public int answer;
        public string hint;
    }
    [Serializable] public class JourneySave
    {
        public int version = 1, chapter, step, unlocked;
        public float seconds;
        public bool finished;
        public string[] discoveries = Array.Empty<string>();
    }
}
