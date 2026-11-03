using System.Linq;
using UnityEngine;
namespace CrossTheLine {
[DefaultExecutionOrder(100)]
public sealed class DiscipleWalking : MonoBehaviour {
 public int discipleIndex;
 SpriteRenderer body;Sprite[] frames;Sprite idle;Transform tracked;Vector3 previous;float phase,stationaryTime,height;bool centered,walking;JourneyGame game;
 public bool Walking=>walking;
 public void Initialize(int index,Transform root,bool center=false){discipleIndex=index;tracked=root;centered=center;body=GetComponent<SpriteRenderer>();idle=body.sprite;height=idle.bounds.size.y*Mathf.Abs(body.transform.localScale.y);var view=JourneyAssets.Load<GameObject>("CrossTheLine/Characters/Disciple"+index.ToString("00"))?.GetComponent<JourneyCharacterView>();frames=view&&view.walkFrames.Length==8&&view.walkFrames.All(s=>s)?view.walkFrames:JourneySpriteAssets.LoadAll("CrossTheLine/DiscipleWalkMotion"+(index/4)).Where(s=>s.name.StartsWith("Disciple"+index.ToString("00")+"_")).OrderBy(s=>s.name).ToArray();if(view&&view.idle)idle=view.idle;previous=root.position;game=GetComponentInParent<JourneyGame>();}
 void OnEnable(){if(tracked)previous=tracked.position;stationaryTime=0;walking=false;phase=0;}
 void LateUpdate(){if(!body||!tracked||frames==null||frames.Length!=8)return;Advance(tracked.position,Time.deltaTime,game&&(game.Mode=="menu"||game.Mode=="dialogue"||game.Mode=="chapter_end"));}
 // Use speed, not distance per rendered frame: small high-FPS steps are still walking.
 public void Advance(Vector3 position,float dt,bool blocked){
  Vector3 delta=position-previous;previous=position;
  bool teleported=delta.sqrMagnitude>=4;
  bool moved=dt>0&&delta.sqrMagnitude>Mathf.Pow(.025f*dt,2)&&!teleported;
  if(blocked||teleported){walking=false;stationaryTime=0;phase=0;}
  else if(moved){walking=true;stationaryTime=0;if(Mathf.Abs(delta.x)>.025f*dt)body.flipX=delta.x<0;}
  else {stationaryTime+=dt;if(stationaryTime>=.08f)walking=false;}
  if(walking&&!blocked)phase=(phase+dt*12)%8;else if(!walking)phase=0;
  var sprite=walking?frames[(int)phase]:idle;body.sprite=sprite;
  float scale=height/sprite.bounds.size.y;body.transform.localScale=Vector3.one*scale;
  if(!centered){var visible=JourneySpriteAssets.VisibleBounds(sprite);body.transform.localPosition=new Vector3((body.flipX?visible.center.x:-visible.center.x)*scale,-visible.min.y*scale,0);}
 }
}
}
