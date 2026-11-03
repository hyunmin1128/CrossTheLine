using UnityEngine;

namespace CrossTheLine
{
    public sealed class EntryTownResident : MonoBehaviour
    {
        public enum WelcomeKind { Bread, Cloth, RaisedHands, Applause, Palm, Child }
        public WelcomeKind Kind {get;private set;}
        public bool Welcoming => arrival>=duration;
        public bool Approaching => arrival>=0&&arrival<duration;
        public Vector3 WorkPlace => start;
        public Vector3 Roadside => target;
        SpriteRenderer body,shadow; Sprite work,walk,cheer; Vector3 start,target; float arrival=-1,duration,height,phase; int index;
        public void Initialize(int id,int kind,Sprite idle,Sprite stride,Sprite welcome,Vector3 place,Vector3 roadside,float size,Material material,Sprite dot)
        {
            index=id;Kind=(WelcomeKind)kind;work=idle;walk=stride;cheer=welcome;start=place;target=roadside;height=size;duration=kind==5?.85f:kind==3?1.65f:1.15f+(id%3)*.13f;body=transform.GetChild(0).GetComponent<SpriteRenderer>();
            var o=new GameObject("FootShadow");o.transform.SetParent(transform,false);o.transform.localPosition=new Vector3(0,.025f,.1f);o.transform.localScale=new Vector3(.58f,.055f,1);shadow=o.AddComponent<SpriteRenderer>();shadow.sprite=dot;shadow.sharedMaterial=material;shadow.color=new Color(.24f,.17f,.10f,.19f);shadow.sortingOrder=id<9?39:64;
            Reset(false);
        }
        public void Reset(bool greeted){arrival=greeted?duration:-1;phase=index*.37f;transform.position=greeted?target:start;Pose(greeted?cheer:work,greeted);body.color=Color.white;shadow.color=new Color(.24f,.17f,.10f,.19f);}
        void Pose(Sprite sprite,bool greeting)
        {
            body.sprite=sprite;
            // Hands and held objects add height above the head; preserve the person's body size.
            float fullHeight=height*(greeting?(Kind==WelcomeKind.Cloth?1.12f:Kind==WelcomeKind.Palm?1.28f:1):1);
            float scale=fullHeight/sprite.bounds.size.y;body.transform.localScale=Vector3.one*scale;body.transform.localPosition=new Vector3(-sprite.bounds.center.x*scale,-sprite.bounds.min.y*scale);body.sortingOrder=index<9?45:68;body.transform.localRotation=Quaternion.identity;
        }
        public void Tick(float dt,float jesusX,bool townHearsWelcome,bool quiet)
        {
            phase+=dt;
            if(arrival<0&&(townHearsWelcome||jesusX>=target.x-4.8f)&&phase>index*.37f+(index%3)*.18f)arrival=0;
            if(arrival>=0&&arrival<duration)
            {
                arrival=Mathf.Min(duration,arrival+dt);float t=Mathf.SmoothStep(0,1,arrival/duration);transform.position=Vector3.Lerp(start,target,t);
                Pose((int)(phase*6)%2==0?work:walk,false);
                body.flipX=target.x<start.x;
            }
            else
            {
                Pose(Welcoming?cheer:work,Welcoming);body.flipX=jesusX<transform.position.x;
                // Foot position stays fixed. Each person responds with a different gentle cadence.
                if(Welcoming&&!quiet)body.transform.localRotation=Quaternion.Euler(0,0,Mathf.Sin(phase*(Kind==WelcomeKind.Child?3.8f:2.1f)+index)* (Kind==WelcomeKind.Cloth?2.2f:1.0f));
            }
        }
    }
}
