using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace CrossTheLine.Minigames
{
    public sealed class FastingReadability : MonoBehaviour
    {
        [Serializable] public sealed class CoverView { public FastingCover cover; public SpriteRenderer fill; public Transform frame; }
        public FastingMinigame game;
        public SpriteRenderer playerFeet;
        public Transform playerFeetFrame;
        public SpriteRenderer[] destinations;
        public SpriteRenderer[] directionArrows;
        public CoverView[] shelterViews;
        public TMP_Text movementText,coverText,damageText,exposureText;
        public Image exposureFill;
        public GameObject damagePanel;
        public Transform stageGate;
        public static void FitFrame(Transform frame)
        {
            if(!frame||frame.childCount<4)return;
            float sx=Mathf.Max(.001f,Mathf.Abs(frame.lossyScale.x)),sy=Mathf.Max(.001f,Mathf.Abs(frame.lossyScale.y));
            for(int i=0;i<4;i++){
                var edge=frame.GetChild(i);bool horizontal=i<2;
                edge.localPosition=horizontal?new Vector3(0,i==0?.5f:-.5f,0):new Vector3(i==2?.5f:-.5f,0,0);
                edge.localScale=horizontal?new Vector3(32,1/sy,1):new Vector3(1/sx,32,1);
            }
        }
        static readonly Vector2Int[] directions={Vector2Int.up,Vector2Int.right,Vector2Int.down,Vector2Int.left};
        public static Color SafeColor=>new Color(.15f,.95f,.62f,.7f);
        public static Color BlockedColor=>new Color(1,.22f,.18f,.75f);
        void LateUpdate(){Refresh();}
        public void Refresh()
        {
            if(!game||!game.player)return;
            bool safe=game.Sheltered;
            playerFeet.transform.position=game.player.position;
            playerFeet.transform.localScale=new Vector3(game.feetHitSize.x*32,game.feetHitSize.y*32,1);
            playerFeet.color=safe?SafeColor:new Color(.3f,.8f,1,.9f);
            playerFeetFrame.position=game.player.position;playerFeetFrame.localScale=new Vector3(game.feetHitSize.x,game.feetHitSize.y,1);
            FitFrame(playerFeetFrame);
            if(stageGate)stageGate.position=new Vector3(game.Gate+.75f,-6.1f,0);
            for(int i=0;i<destinations.Length;i++){
                var state=game.InspectStep(directions[i],out var target);
                bool show=state!=FastingMinigame.StepState.Moving&&state!=FastingMinigame.StepState.Inactive;
                destinations[i].gameObject.SetActive(show);if(!show)continue;
                destinations[i].transform.position=target;
                Color c=state==FastingMinigame.StepState.Available?SafeColor:state==FastingMinigame.StepState.Waiting?new Color(1,.8f,.13f,.75f):state==FastingMinigame.StepState.Obstacle?BlockedColor:new Color(.5f,.55f,.6f,.65f);
                destinations[i].color=c;if(directionArrows[i])directionArrows[i].color=Color.white;
            }
            foreach(var view in shelterViews){if(!view.cover)continue;
                var area=view.cover.shelter;view.fill.transform.localPosition=area.center;view.fill.transform.localScale=new Vector3(area.width*32,area.height*32,1);
                bool active=view.cover.Protects(game.player.position);
                view.fill.color=active?new Color(.06f,1,.55f,.5f):new Color(.05f,.8f,.6f,.23f);
                view.frame.localPosition=area.center;view.frame.localScale=new Vector3(area.width,area.height,1);
                FitFrame(view.frame);
            }
            movementText.text=game.Restarting?"이동 불가 · 재시작 준비 중":game.Finished?"구간 완료":game.Moving?"이동 중 · 발밑 표시가 도착할 때까지 이동":"";
            if(!game.Moving&&!game.Restarting&&!game.Finished)movementText.text=string.IsNullOrEmpty(game.ActionMessage)?"이동 가능 · 방향키/WASD · 한 번은 한 칸 / 길게 누르면 연속 이동":game.ActionMessage;
            coverText.text=safe?"엄폐 성공 · 초록 영역 안 · 바람/더위/추위 보호":"엄폐 안 됨 · 청록색 바닥 안으로 이동하세요";
            coverText.color=safe?new Color(.35f,1,.68f):new Color(1,.86f,.58f);
            damagePanel.SetActive(game.DamageNoticeRemaining>0);
            damageText.text=game.DamageMessage;
            float ratio=Mathf.Clamp01(game.Exposure/game.ExposureLimit);exposureFill.fillAmount=ratio;
            exposureFill.color=ratio>.75f?new Color(1,.3f,.15f):new Color(.95f,.74f,.25f);
            exposureText.text=safe?"노출 없음 · 엄폐 중":$"{(game.Night?"추위":"더위")} 노출 {game.Exposure:0.0} / {game.ExposureLimit:0.0}초 · 가득 차면 -1칸";
            game.instructionsText.text="흰 발 테두리와 붉은 바닥이 겹치면 피격 · 초록 이동 / 빨강 막힘 / 노랑 대기 · R 재시작";
        }
    }
}
