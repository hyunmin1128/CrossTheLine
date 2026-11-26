using UnityEngine;

namespace CrossTheLine.Minigames
{
    public sealed partial class FastingTopDownGame
    {
        public const int MaximumShields=3;
        public int ShieldCount{get;private set;}
        public bool Invulnerable=>grace>0||waterGrace>0;
        public float InvulnerabilityRemaining=>Mathf.Max(grace,waterGrace);
        FastingShieldOrbit shieldOrbit;

        void EnsureShieldVisuals()
        {
            if(shieldOrbit)return;
            shieldOrbit=gameObject.AddComponent<FastingShieldOrbit>();
            shieldOrbit.Initialize(this);
        }
        void CollectShield(FastingTopDownEntity entity)
        {
            EnsureShieldVisuals();shieldOrbit.UsePickupArtwork(entity);
            int before=ShieldCount;ShieldCount=Mathf.Min(MaximumShields,ShieldCount+1);
            if(playerAura&&ShieldCount>before)playerAura.AcquireShield();
            Message=$"믿음의 방패 {ShieldCount}/{MaximumShields} · 다음 피해 방어";
            shieldOrbit.Refresh();Refresh();
        }
        void ResetShields(){ShieldCount=0;if(shieldOrbit)shieldOrbit.Clear();if(playerAura)playerAura.Clear();}
        void BreakShield()
        {
            AudioPlayback.PlayEffect(shieldBreak,shieldBreakVolume);EnsureShieldVisuals();shieldOrbit.Break(ShieldCount-1);ShieldCount--;
            if(playerAura)playerAura.BreakShield();if(hitVignette)hitVignette.Play(true);
            // One shared timer controls both white flashing and rejection of every hit.
            grace=Mathf.Max(settings.hitGrace,FastingShieldBreakGraphic.MinimumProtectionSeconds);
            RefreshInvulnerability();shieldOrbit.Refresh();
            Message=$"믿음의 방패 파괴 · 남은 방패 {ShieldCount}/{MaximumShields} · 점멸 중 무적";Refresh();
        }
    }
}
