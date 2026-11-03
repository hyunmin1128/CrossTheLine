using UnityEngine;
namespace CrossTheLine
{
 public sealed partial class DesertTemptation
 {
  bool staffCollected; Vector3 fastingStart,bodyScale; Vector3 bodyPosition; float weather;
  public bool StaffCollected=>staffCollected;
  public bool IsCold=>weather>.55f;
  void ResetWilderness()
  {
   staffCollected=false;weather=0;bodyScale=jesusRenderer.transform.localScale;bodyPosition=jesusRenderer.transform.localPosition;
   staff.gameObject.SetActive(true);staff.position=locations[0].entrance.position+new Vector3(3,.08f,0);staff.rotation=Quaternion.Euler(0,0,78);
   SetScrollHud("광야로 향하는 길","D / →  ·  앞에 떨어진 나뭇가지로 다가가 E를 누르세요");
  }
  void HoldStaff()
  {
   if(!staffCollected)return;staff.position=jesus.position+new Vector3(.46f,.72f,0);staff.rotation=Quaternion.identity;
  }
  void BeginFasting()
  {
   CurrentStage=Stage.Fasting;clock=0;fastingStart=jesus.position;film.SetActive(true);game.BaptismCinema(true);game.BaptismWalk(false,1);
  }
  void TickFasting(float dt)
  {
   float t=Mathf.Clamp01(clock/scrolling.fastingSeconds);
   FastingDay=Mathf.Clamp(Mathf.FloorToInt(t*40)+1,1,40);
   weather=(1-Mathf.Cos(clock*2.2f))*.5f;
   land.color=Color.Lerp(new Color(1,.91f,.73f),new Color(.23f,.29f,.46f),weather);
   caption.text="40일의 금식 기도 · "+FastingDay+"일째\n"+(IsCold?"차가운 밤 · 추위 속에서도 기도하셨습니다.":"뜨거운 낮 · 먹지 않고 광야의 길을 걸으셨습니다.");
   var end=locations[1].jesusMark.position;var travel=Vector3.Lerp(fastingStart,end,t);travel.y=RouteY(travel.x);jesus.position=travel;location=jesus.position.x>(locations[0].transform.position.x+locations[1].transform.position.x)*.5f?1:0;
   game.BaptismWalk(true,1);
   // Whole-pixel shivering keeps the feet on the authored path; no actor rotation.
   if(IsCold)jesusRenderer.transform.localPosition+=new Vector3(Mathf.Sin(clock*32)>0?1f/32:-1f/32,0,0);
   else jesusRenderer.color=new Color(1,.91f,.85f);
   HoldStaff();
   if(t<1)return;
   land.color=Color.white;jesusRenderer.color=Color.white;game.BaptismWalk(false,1);
   bodyScale=jesusRenderer.transform.localScale;bodyPosition=jesusRenderer.transform.localPosition;
   staff.position=jesus.position+new Vector3(.7f,.08f,0);staff.rotation=Quaternion.Euler(0,0,78);CurrentStage=Stage.Collapse;clock=0;caption.text="40일 후 · 주리시고 지치셨습니다.";
  }
  void TickCollapse()
  {
   // Lower the whole body toward its feet instead of spinning the sprite into the ground.
   float t=Mathf.SmoothStep(0,1,Mathf.Clamp01(clock/.8f));
   jesusRenderer.transform.localScale=new Vector3(bodyScale.x,bodyScale.y*Mathf.Lerp(1,.48f,t),bodyScale.z);
   jesusRenderer.transform.localPosition=new Vector3(bodyPosition.x,bodyPosition.y*Mathf.Lerp(1,.48f,t),bodyPosition.z);
   if(clock<1.6f)return;
   location=1;
   jesusRenderer.transform.localScale=bodyScale;jesusRenderer.transform.localPosition=bodyPosition;game.BaptismWalk(false,1);RevealTrial();
  }
 }
}

