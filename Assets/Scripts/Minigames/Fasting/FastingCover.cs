using UnityEngine;
namespace CrossTheLine.Minigames
{
    public sealed class FastingCover : MonoBehaviour
    {
        [Tooltip("Right-to-left wind: shelter is LEFT of the obstacle. Local feet coordinates.")]
        public Rect shelter=new Rect(-2.6f,-.35f,2.1f,.7f);
        public Rect solid=new Rect(-.3f,-.35f,.6f,.7f);
        public bool Protects(Vector2 feet)=>shelter.Contains(feet-(Vector2)transform.position);
        public bool Blocks(Vector2 feet)=>solid.Contains(feet-(Vector2)transform.position);
        void OnDrawGizmosSelected()
        {
            Gizmos.color=new Color(.2f,.9f,.7f,.35f);
            Gizmos.DrawCube(transform.position+(Vector3)shelter.center,new Vector3(shelter.width,shelter.height,.1f));
            Gizmos.color=new Color(1,.3f,.2f,.4f);
            Gizmos.DrawWireCube(transform.position+(Vector3)solid.center,new Vector3(solid.width,solid.height,.1f));
        }
    }
}
