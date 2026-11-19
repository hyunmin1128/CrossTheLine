using UnityEngine;
namespace CrossTheLine.Minigames
{
    [RequireComponent(typeof(FastingTopDownHazard))]
    public sealed class FastingReaperSweep : MonoBehaviour
    {
        public Color slashColor=new Color(1,.8f,.55f,.85f);
        public float slashWidth=.09f;
        FastingTopDownHazard hazard;LineRenderer[] trails;float clock;
        void Awake(){hazard=GetComponent<FastingTopDownHazard>();trails=new LineRenderer[3];for(int i=0;i<3;i++){
            var child=new GameObject("ScytheArc"+i);child.transform.SetParent(transform,false);var line=child.AddComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=25;line.sharedMaterial=hazard.body.sharedMaterial;line.sortingLayerID=hazard.body.sortingLayerID;line.sortingOrder=hazard.body.sortingOrder+2;line.widthMultiplier=slashWidth;trails[i]=line;
        }}
        void LateUpdate(){
            clock+=Time.deltaTime;float width=hazard.size.x;
            for(int j=0;j<trails.Length;j++){
                var line=trails[j];line.enabled=!hazard.spent;var c=slashColor;c.a*=hazard.warning>0?.3f:.65f+.3f*Mathf.Sin(clock*12+j);line.startColor=c;line.endColor=c;
                for(int i=0;i<25;i++){float t=i/24f;float y=Mathf.Sin(t*Mathf.PI)*.55f+(j-1)*.16f+Mathf.Sin(clock*8)*.12f;line.SetPosition(i,new Vector3((t-.5f)*width,y,0));}
            }
            hazard.body.transform.localRotation=Quaternion.Euler(0,0,Mathf.Sin(clock*8)*18);
        }
    }
}
