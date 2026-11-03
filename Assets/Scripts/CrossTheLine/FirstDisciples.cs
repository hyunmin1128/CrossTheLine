using UnityEngine;
using UnityEngine.InputSystem;
namespace CrossTheLine {
public sealed class FirstDisciples:MonoBehaviour {
 public bool IsCinematic=>false; public int SceneStep{get;private set;}
 public Transform Andrew=>andrew;public Transform Simon=>simon;public Transform Philip=>philip;
 public Vector2 entrance=new Vector2(-8,-2.8f),andrewPosition=new Vector2(-3,-2.8f),peterPosition=new Vector2(3,-2.8f),philipPosition=new Vector2(9,-2.8f);
 JourneyGame game;Transform player,andrew,simon,philip;bool busy,initialized;Vector3 andrewHome,peterHome,philipHome;
 public void Initialize(JourneyGame g,Transform p,SpriteRenderer r,Material m,Sprite square,int step){game=g;player=p;
  var settings=g.GetComponent<FirstDisciplesRoadSettings>();if(settings){entrance=settings.entrance;andrewPosition=settings.andrew;peterPosition=settings.peter;philipPosition=settings.philip;}
  if(settings&&settings.layoutPrefab){var layout=Instantiate(settings.layoutPrefab,transform);andrew=layout.transform.Find("안드레");simon=layout.transform.Find("베드로");philip=layout.transform.Find("빌립");PrepareCast();ConfigureStep(step);g.BaptismCinema(false);return;}
  var background=new GameObject("RoadsideVillageBackground").AddComponent<SpriteRenderer>();background.transform.SetParent(transform,false);
  background.sprite=JourneySpriteAssets.Load("CrossTheLine/PixelScenery/SceneVillage");background.sharedMaterial=m;background.sortingOrder=-30;
  background.transform.localPosition=new Vector3(2,0,0);background.transform.localScale=new Vector3(36/background.sprite.bounds.size.x,18/background.sprite.bounds.size.y,1);
  andrew=Actor("안드레",g.disciples[1],m);simon=Actor("베드로",g.disciples[0],m);philip=Actor("빌립",g.disciples[4],m);
  andrew.localPosition=andrewPosition;simon.localPosition=peterPosition;philip.localPosition=philipPosition;PrepareCast();ConfigureStep(step);g.BaptismCinema(false);
 }
 void PrepareCast(){andrewHome=andrew.localPosition;peterHome=simon.localPosition;philipHome=philip.localPosition;var cast=new[]{andrew,simon,philip};int[] indices={1,0,4};for(int i=0;i<cast.Length;i++)cast[i].GetComponentInChildren<SpriteRenderer>().gameObject.AddComponent<DiscipleWalking>().Initialize(indices[i],cast[i]);}
 Transform Actor(string name,Sprite sprite,Material m){var root=new GameObject(name).transform;root.SetParent(transform,false);var r=new GameObject("Sprite").AddComponent<SpriteRenderer>();r.transform.SetParent(root,false);r.sprite=sprite;r.sharedMaterial=m;r.sortingOrder=50;r.flipX=true;var b=JourneySpriteAssets.VisibleBounds(sprite);float scale=1.85f/b.size.y;r.transform.localScale=Vector3.one*scale;r.transform.localPosition=new Vector3(b.center.x*scale,-b.min.y*scale,0);return root;}
 public void ConfigureStep(int step){SceneStep=Mathf.Clamp(step,0,2);busy=false;
  // Preserve position after each conversation; resume saves just before their next encounter.
  if(!initialized){player.localPosition=SceneStep==0?(Vector3)entrance:new Vector3((SceneStep==1?simon.localPosition.x:philip.localPosition.x)-3,entrance.y,0);if(SceneStep>0)andrew.localPosition=andrewHome+Vector3.down*2.1f;if(SceneStep>1)simon.localPosition=peterHome+Vector3.down*2.1f;initialized=true;}
  game.MeetingWalk(false,1);UpdateHud();}
 Transform Target=>SceneStep==0?andrew:SceneStep==1?simon:philip;
 void UpdateHud(){game.MeetingHud(new[]{"오른쪽 길가의 안드레 만나기","오른쪽으로 걸어 베드로 만나기","오른쪽으로 걸어 빌립 만나기"}[SceneStep],Vector2.Distance(player.localPosition,Target.localPosition)<1.5f?"E · 이야기 나누기":"A / D · ← / → 이동    E · 대화");}
 public void Tick(float dt,bool canMove,bool press){var k=Keyboard.current;float direction=k==null?0:(k.dKey.isPressed||k.rightArrowKey.isPressed?1:0)-(k.aKey.isPressed||k.leftArrowKey.isPressed?1:0);MoveOnRoad(dt,direction,canMove,press);}
 public void MoveOnRoad(float dt,float dx,bool canMove,bool press){if(!canMove||busy)return;bool clearing=false;if(SceneStep>0)clearing|=StepAside(andrew,andrewHome,dt);if(SceneStep>1)clearing|=StepAside(simon,peterHome,dt);if(clearing)dx=0;var p=player.localPosition;float before=p.x;p.x=Mathf.Clamp(p.x+dx*3.6f*dt,-9,Target.localPosition.x-1.05f);p.y=entrance.y;player.localPosition=p;game.MeetingWalk(Mathf.Abs(before-p.x)>.00001f,dx);UpdateHud();if(press)Interact();}
 bool StepAside(Transform actor,Vector3 home,float dt){var destination=home+Vector3.down*2.1f;actor.localPosition=Vector3.MoveTowards(actor.localPosition,destination,dt*3.6f);return Vector3.Distance(actor.localPosition,destination)>.01f;}
 public bool ClearExit(float dt)=>!StepAside(philip,philipHome,dt);
 public void Interact(){if(busy||Vector2.Distance(player.localPosition,Target.localPosition)>=1.5f)return;busy=true;game.MeetingTalk(()=>game.BaptismAdvance());}
 public void UpdateCamera(){float x=Mathf.Clamp(player.localPosition.x+3,0,4);game.journeyCamera.transform.position=new Vector3(x,0,-10);game.journeyCamera.orthographicSize=5.625f;}
 public void Shutdown(){}
}
}

