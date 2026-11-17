using UnityEngine;

namespace CrossTheLine.Minigames
{
    /// <summary>Projects the marked desert edges and HUD edges into the player's movement space.</summary>
    [DefaultExecutionOrder(200)]
    public sealed class FastingPlayArea : MonoBehaviour
    {
        public FastingTopDownGame game;
        public RectTransform header,statusPanel;
        public SpriteRenderer background;
        public Vector2 horizontalPixelEdges=new Vector2(52,282);
        public float backgroundPixelWidth=320,visualClearance=.025f;
        public Rect visibleSpriteRect=new Rect(-.5f,-.91f,1,1.79f);
        readonly Vector3[] corners=new Vector3[4];

        float UIEdge(RectTransform rect,bool top,float fallback)
        {
            if(!rect)return fallback;rect.GetWorldCorners(corners);var canvas=rect.GetComponentInParent<Canvas>();var canvasRect=canvas?(RectTransform)canvas.rootCanvas.transform:null;
            float edge=top?float.NegativeInfinity:float.PositiveInfinity;
            foreach(var corner in corners)
            {
                float y;
                if(canvasRect&&canvas.renderMode!=RenderMode.WorldSpace)
                {
                    var local=canvasRect.InverseTransformPoint(corner);var view=game.gameCamera.rect;
                    float screenY=Mathf.InverseLerp(canvasRect.rect.yMin,canvasRect.rect.yMax,local.y);y=(screenY-view.y)/Mathf.Max(.001f,view.height);
                }
                else y=game.gameCamera.WorldToViewportPoint(corner).y;
                edge=top?Mathf.Max(edge,y):Mathf.Min(edge,y);
            }
            return Mathf.Clamp01(edge);
        }
        public Rect VisibleWorldArea
        {
            get
            {
                var camera=game.gameCamera;float halfHeight=camera.orthographicSize,halfWidth=halfHeight*camera.aspect;var center=camera.transform.position;
                // Use the authored orthographic size rather than transient render-time projection matrices.
                Vector3 low=new Vector3(center.x-halfWidth,center.y-halfHeight+2*halfHeight*UIEdge(statusPanel,true,.18f),0),high=new Vector3(center.x+halfWidth,center.y-halfHeight+2*halfHeight*UIEdge(header,false,.9f),0);
                if(background){var b=background.bounds;low.x=Mathf.Max(low.x,Mathf.Lerp(b.min.x,b.max.x,horizontalPixelEdges.x/backgroundPixelWidth));high.x=Mathf.Min(high.x,Mathf.Lerp(b.min.x,b.max.x,horizontalPixelEdges.y/backgroundPixelWidth));}
                return Rect.MinMaxRect(low.x,low.y,high.x,high.y);
            }
        }
        public Rect RootArea
        {
            get
            {
                var area=VisibleWorldArea;var body=game.playerBody.transform;Vector3 a=body.TransformPoint(visibleSpriteRect.min),b=body.TransformPoint(visibleSpriteRect.max),root=game.player.position;
                // Hit animations move/shrink only the artwork; they must not move the walkable rectangle.
                if(game.hitFeedback&&game.hitFeedback.Active&&game.hitFeedback.TryGetRestPose(out var restPosition,out var restScale))
                {
                    Vector3 la=restPosition+body.localRotation*Vector3.Scale(restScale,(Vector3)visibleSpriteRect.min),lb=restPosition+body.localRotation*Vector3.Scale(restScale,(Vector3)visibleSpriteRect.max);
                    a=body.parent?body.parent.TransformPoint(la):la;b=body.parent?body.parent.TransformPoint(lb):lb;
                }
                Vector2 min=Vector2.Min(a,b)-(Vector2)root,max=Vector2.Max(a,b)-(Vector2)root;
                float left=area.xMin-min.x+visualClearance,right=area.xMax-max.x-visualClearance,bottom=area.yMin-min.y+visualClearance,top=area.yMax-max.y-visualClearance;
                if(right<left)left=right=(left+right)*.5f;if(top<bottom)bottom=top=(top+bottom)*.5f;return Rect.MinMaxRect(left,bottom,right,top);
            }
        }
        public Vector2 Clamp(Vector2 position){var area=RootArea;return new Vector2(Mathf.Clamp(position.x,area.xMin,area.xMax),Mathf.Clamp(position.y,area.yMin,area.yMax));}
        public bool Contains(Vector2 position){var area=RootArea;return position.x>=area.xMin-.0001f&&position.x<=area.xMax+.0001f&&position.y>=area.yMin-.0001f&&position.y<=area.yMax+.0001f;}
        void LateUpdate(){if(game&&game.Running&&game.player)game.player.position=Clamp(game.player.position);}
        void OnDrawGizmosSelected(){if(!game||!game.gameCamera||!game.playerBody)return;var area=VisibleWorldArea;Gizmos.color=Color.cyan;Gizmos.DrawWireCube(area.center,area.size);}
    }
}
