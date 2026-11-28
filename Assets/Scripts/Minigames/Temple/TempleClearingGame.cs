using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

namespace CrossTheLine.Minigames.Temple
{
    public sealed class TempleClearingGame : MonoBehaviour
    {
        public enum Role { Cow, Sheep, Dove, Vendor, MoneyChanger, Scribe, Pharisee, HighPriest }
        public enum Phase { Introduction, Active, Intermission, Failed, Complete }

        [Serializable]
        public sealed class Wave
        {
            [Min(1)] public int sets = 1;
            public Role[] additionalPeople;
            [Min(1)] public float timeLimit = 150;
            [Min(0)] public float lastArrival = 105;
        }

        [Serializable]
        public sealed class Appearance
        {
            public Sprite[] sprites;
            [Min(1)] public int resistance = 2;
            [Min(.1f)] public float exitSpeed = 2;
        }

        public Wave[] waves;
        public Appearance[] appearances;
        public Transform player, exit, crowdRoot;
        public SpriteRenderer playerArt;
        public TempleCrowdActor actorPrefab;
        public Rect playBounds = new Rect(-7.5f,-3.6f,15,4.8f);
        public TMP_Text heading, status, message;
        [Min(.1f)] public float moveSpeed = 4;
        [Min(.1f)] public float attackCooldown = .55f;
        [Min(0)] public float strikeDelay = .16f;
        [Min(.1f)] public float attackRange = 2;
        public Animator attackAnimator;
        public AudioSource attackAudio;
        public AudioClip attackClip;

        public Phase State { get; private set; }
        public int WaveIndex { get; private set; }
        public int Remaining => arrivals.Count - spawned + crowd.Count;
        public float WaveElapsed { get; private set; }
        readonly List<Role> arrivals = new();
        readonly List<TempleCrowdActor> crowd = new();
        int spawned;
        float transition, cooldown, untilStrike = -1;
        Vector2 facing = Vector2.right, strikeFacing;

        void Start() => Refresh();

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (State == Phase.Introduction && keyboard.enterKey.wasPressedThisFrame) Begin();
            if ((State == Phase.Failed || State == Phase.Complete) && keyboard.rKey.wasPressedThisFrame) Begin();
            Vector2 move = new Vector2(
                (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0),
                (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0));
            Tick(Time.deltaTime, move, keyboard.spaceKey.wasPressedThisFrame);
        }

        public void Begin()
        {
            foreach (var actor in crowd)
            {
                if (!actor) continue;
                actor.gameObject.SetActive(false);
                Destroy(actor.gameObject);
            }
            crowd.Clear();
            WaveIndex = 0;
            player.position = new Vector3(-5,-2);
            facing = Vector2.right;
            StartWave();
        }

        void StartWave()
        {
            arrivals.Clear();
            var wave = waves[WaveIndex];
            for (int i = 0; i < wave.sets; i++)
                arrivals.AddRange(new[]{ Role.Cow, Role.Sheep, Role.Dove });
            for (int i = 0; i < 3; i++) arrivals.Insert(i * 2, Role.Vendor);
            if (wave.additionalPeople != null) arrivals.AddRange(wave.additionalPeople);
            spawned = 0;
            WaveElapsed = cooldown = 0;
            untilStrike = -1;
            State = Phase.Active;
            SpawnArrivals();
            Refresh();
        }

        public void Tick(float dt, Vector2 move, bool attack)
        {
            if (State == Phase.Intermission)
            {
                if ((transition -= dt) <= 0) { WaveIndex++; StartWave(); }
                return;
            }
            if (State != Phase.Active) return;

            WaveElapsed += dt;
            cooldown = Mathf.Max(0, cooldown - dt);
            move = Vector2.ClampMagnitude(move, 1);
            if (move.sqrMagnitude > .01f)
            {
                facing = move.normalized;
                if (Mathf.Abs(move.x) > .01f) playerArt.flipX = move.x < 0;
            }
            var position = (Vector2)player.position + move * (moveSpeed * dt);
            position.x = Mathf.Clamp(position.x, playBounds.xMin, playBounds.xMax);
            position.y = Mathf.Clamp(position.y, playBounds.yMin, playBounds.yMax);
            player.position = position;
            playerArt.sortingOrder = Mathf.RoundToInt(-position.y * 20);

            if (attack && cooldown <= 0 && untilStrike < 0)
            {
                cooldown = Mathf.Max(attackCooldown, strikeDelay);
                untilStrike = strikeDelay;
                strikeFacing = facing;
                if (attackAnimator) attackAnimator.SetTrigger("Attack");
            }
            if (untilStrike >= 0 && (untilStrike -= dt) <= 0)
            {
                untilStrike = -1;
                if (attackAudio && attackClip) attackAudio.PlayOneShot(attackClip);
                foreach (var actor in crowd)
                {
                    Vector2 offset = (Vector2)(actor.transform.position - player.position);
                    if (offset.sqrMagnitude <= attackRange * attackRange &&
                        (offset.sqrMagnitude < .16f || Vector2.Dot(strikeFacing, offset.normalized) >= .2f))
                        actor.ApplyPressure();
                }
            }

            for (int i = crowd.Count - 1; i >= 0; i--)
                if (crowd[i].Advance(dt, exit.position))
                {
                    Destroy(crowd[i].gameObject);
                    crowd.RemoveAt(i);
                }
            SpawnArrivals();
            if (Remaining == 0)
            {
                State = WaveIndex + 1 == waves.Length ? Phase.Complete : Phase.Intermission;
                transition = 2;
            }
            else if (WaveElapsed >= waves[WaveIndex].timeLimit) State = Phase.Failed;
            Refresh();
        }

        void SpawnArrivals()
        {
            float interval = waves[WaveIndex].lastArrival / Mathf.Max(1, arrivals.Count - 2);
            while (spawned < arrivals.Count && WaveElapsed >= Mathf.Max(0, spawned - 1) * interval)
            {
                Role role = arrivals[spawned];
                var appearance = appearances[(int)role];
                int slot = spawned % 6;
                float x = Mathf.Lerp(playBounds.xMin + 1, playBounds.xMax - 2, (slot % 3) * .5f);
                float y = Mathf.Lerp(playBounds.yMin + .5f, playBounds.yMax - .5f, (slot / 3) * .75f);
                var actor = Instantiate(actorPrefab, new Vector3(x,y), Quaternion.identity, crowdRoot);
                actor.name = role.ToString();
                actor.Setup(appearance, spawned);
                crowd.Add(actor);
                spawned++;
            }
        }

        void Refresh()
        {
            heading.text = State == Phase.Introduction ? "Temple Courtyard" : $"Wave {WaveIndex + 1} / {waves.Length}";
            status.text = State == Phase.Active ? $"Remaining: {Remaining}     Time: {Mathf.CeilToInt(Mathf.Max(0, waves[WaveIndex].timeLimit - WaveElapsed))}s" : "";
            message.text = State switch
            {
                Phase.Introduction => "Move: WASD / Arrow Keys    Pressure: Space    Start: Enter",
                Phase.Intermission => "Courtyard cleared. Next wave incoming.",
                Phase.Failed => "Time is up. Press R to retry.",
                Phase.Complete => "All waves cleared. Press R to play again.",
                _ => spawned < arrivals.Count && crowd.Count == 0 ? "More arrivals incoming..." : ""
            };
        }
    }
}
