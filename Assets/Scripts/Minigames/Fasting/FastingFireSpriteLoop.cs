using UnityEngine;
namespace CrossTheLine.Minigames
{
    public sealed class FastingFireSpriteLoop : MonoBehaviour
    {
        public SpriteRenderer target;
        public Sprite[] frames;
        public float framesPerSecond=8;
        void Update(){if(target&&frames!=null&&frames.Length>0){target.sprite=frames[Mathf.FloorToInt(Time.time*framesPerSecond)%frames.Length];target.transform.localPosition=-target.sprite.bounds.center*target.transform.localScale.x;}}
    }
}
