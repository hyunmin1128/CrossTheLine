using UnityEngine;

namespace CrossTheLine.Cana
{
    public sealed class CanaTeleportEffect : MonoBehaviour
    {
        public Animator animator;
        [Min(0.01f)] public float playbackSpeed = 2.5f;

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            animator.speed = playbackSpeed;
            animator.Play(0, 0, 0);
            animator.Update(0);
        }

        private void Update()
        {
            animator.speed = playbackSpeed;
            if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1)
                Destroy(gameObject);
        }
    }
}
