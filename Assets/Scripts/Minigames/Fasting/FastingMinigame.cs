using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace CrossTheLine.Minigames
{
    public sealed class FastingMinigame : MonoBehaviour
    {
        public FastingSettings settings;
        public Transform player,artwork,hazardRoot;
        public SpriteRenderer body;
        public Sprite idle;public Sprite[] walk;
        public Camera gameCamera;
        public SpriteRenderer[] backgrounds,warnings;
        public FastingCover[] covers;
        public FastingSandstorm stormPrefab;
        public TMP_Text dayText,statusText,instructionsText,resultText;
        public Image[] staminaSlots;
        public Image nightOverlay;
        public GameObject resultPanel;
        public Button replayButton;
        public int Stamina{get;private set;}
        public int StageIndex{get;private set;}
        public int Day=>Mathf.Clamp(StageIndex*5+1+Mathf.Clamp(Mathf.FloorToInt(stageTime/Stage.seconds*5),0,4),1,40);
        public bool Night=>stageTime>=Stage.seconds*.5f;
        public bool Sheltered=>Array.Exists(covers,c=>c&&c.Protects(player.position));
        public bool Finished{get;private set;}
        public bool Restarting{get;private set;}
        public float StageTime=>stageTime;
        public IReadOnlyList<FastingSandstorm> Storms=>storms;
        readonly List<FastingSandstorm> storms=new List<FastingSandstorm>();
        readonly List<int> waveLanes=new List<int>();
        FastingSettings.Difficulty Stage=>settings.stages[Mathf.Clamp(StageIndex,0,settings.stages.Length-1)];
        float stageTime,waveTime,warningTime,invulnerable,exposure,restartTime,walkClock;
        float moveTime;Vector3 moveFrom,moveTo;Vector2Int pending;int lane=1,waveNumber;
        bool moving,warningActive;
        public float Gate=>Mathf.Min(settings.ground.xMax,settings.ground.xMin+(StageIndex+1)*settings.stageLength);
        void Start()
        {
            Application.runInBackground=true;
            if(!settings||settings.stages.Length!=8||!player||!body||!gameCamera||!stormPrefab)throw new InvalidOperationException("Fasting scene bindings incomplete.");
            JourneyRuntimeFonts.Apply(gameObject.scene);
            foreach(var t in GetComponentsInChildren<TMP_Text>(true))if(t) t.font=JourneyRuntimeFonts.Resolve(t.font);
            replayButton.onClick.AddListener(Restart);
            Restart();
        }
        void OnDestroy(){if(replayButton)replayButton.onClick.RemoveListener(Restart);}
        public void Restart()
        {
            foreach(var s in storms)if(s){s.gameObject.SetActive(false);Destroy(s.gameObject);}storms.Clear();
            Stamina=settings.maximumStamina;StageIndex=0;stageTime=0;waveTime=2.5f;warningTime=0;waveNumber=0;
            exposure=0;invulnerable=0;warningActive=false;Finished=false;Restarting=false;moving=false;pending=Vector2Int.zero;lane=1;
            player.position=new Vector3(settings.ground.xMin,settings.lanes[lane],0);body.sprite=idle;body.color=Color.white;body.flipX=false;
            resultPanel.SetActive(false);foreach(var w in warnings)if(w)w.enabled=false;
            RefreshUI();FollowCamera();
        }
        void Update()
        {
            var k=Keyboard.current;
            if(k!=null&&k.rKey.wasPressedThisFrame){Restart();return;}
            if(Finished)return;
            if(Restarting){restartTime-=Time.deltaTime;if(restartTime<=0)Restart();return;}
            if(!Application.isFocused&& !Application.runInBackground)return;
            Vector2Int input=Vector2Int.zero;
            if(k!=null){if(k.dKey.wasPressedThisFrame||k.rightArrowKey.wasPressedThisFrame)input=Vector2Int.right;
            else if(k.aKey.wasPressedThisFrame||k.leftArrowKey.wasPressedThisFrame)input=Vector2Int.left;
            else if(k.wKey.wasPressedThisFrame||k.upArrowKey.wasPressedThisFrame)input=Vector2Int.up;
            else if(k.sKey.wasPressedThisFrame||k.downArrowKey.wasPressedThisFrame)input=Vector2Int.down;}
            if(input!=Vector2Int.zero)RequestStep(input);
            Advance(Time.deltaTime);
        }
        public void RequestStep(Vector2Int direction)
        {
            if(Finished||Restarting)return;
            if(moving){pending=direction;return;}
            int nextLane=Mathf.Clamp(lane-direction.y,0,settings.lanes.Length-1);
            Vector3 target=player.position;
            target.x=Mathf.Clamp(target.x+direction.x*settings.stepWidth,settings.ground.xMin,Gate);
            target.y=settings.lanes[nextLane];
            if((target-player.position).sqrMagnitude<.001f)return;
            // Feet follow the ground continuously. Swept solid check prevents walking through a rock.
            for(int i=1;i<=8;i++)if(Array.Exists(covers,c=>c&&c.Blocks(Vector3.Lerp(player.position,target,i/8f))))return;
            lane=nextLane;moveFrom=player.position;moveTo=target;moveTime=0;moving=true;
            if(direction.x!=0)body.flipX=direction.x<0;
        }
        public void Advance(float dt)
        {
            if(Finished||Restarting||dt<=0)return;
            invulnerable=Mathf.Max(0,invulnerable-dt);stageTime+=dt;
            if(moving){moveTime+=dt;float t=Mathf.Clamp01(moveTime/settings.stepSeconds);player.position=Vector3.Lerp(moveFrom,moveTo,t);walkClock+=dt;body.sprite=walk!=null&&walk.Length>0?walk[(int)(walkClock*10)%walk.Length]:idle;
                if(t>=1){moving=false;var queued=pending;pending=Vector2Int.zero;if(queued!=Vector2Int.zero)RequestStep(queued);}}
            else body.sprite=idle;
            AlignArtwork();
            bool safe=Sheltered;
            exposure=safe?0:exposure+dt;
            float limit=Stage.exposureSeconds*(Night?1:1.6f);
            if(exposure>=limit){exposure=0;TakeHit();}
            if(Restarting)return;
            if(warningActive){warningTime-=dt;if(warningTime<=0)SpawnWave();}
            else{waveTime-=dt;if(waveTime<=0)WarnWave();}
            for(int i=storms.Count-1;i>=0;i--){var s=storms[i];if(!s){storms.RemoveAt(i);continue;}s.Advance(dt);
                if(!safe&&s.Hits(player.position)){s.spent=true;TakeHit();}
                if(s.transform.position.x<settings.ground.xMin-4){Destroy(s.gameObject);storms.RemoveAt(i);}}
            if(!Restarting&&stageTime>=Stage.seconds&&player.position.x>=Gate-.05f){
                if(StageIndex==settings.stages.Length-1){Finished=true;resultPanel.SetActive(true);resultText.text="40일의 금식 기도 구간 완료\n다음 연결 예정: 지치심 → 사탄의 첫 시험";}
                else{StageIndex++;stageTime=0;exposure=0;waveTime=Mathf.Max(waveTime,1.5f);}}
            body.color=invulnerable>0?new Color(1,.65f,.45f,.65f):Color.white;
            RefreshUI();FollowCamera();
        }
        public void TakeHit()
        {
            if(Finished||Restarting||invulnerable>0||Sheltered)return;
            Stamina=Mathf.Max(0,Stamina-1);invulnerable=settings.hitGraceSeconds;
            if(Stamina==0){Restarting=true;restartTime=settings.restartDelay;moving=false;pending=Vector2Int.zero;
                resultPanel.SetActive(true);resultText.text="지침을 모두 소모했습니다\n잠시 후 1일째부터 다시 시작합니다";}
            RefreshUI();
        }
        void WarnWave()
        {
            waveLanes.Clear();int first=waveNumber++%3;waveLanes.Add(first);
            if(Stage.dangerousLanes==2)waveLanes.Add((first+1)%3);
            foreach(var w in warnings)if(w)w.enabled=false;
            foreach(int l in waveLanes){warnings[l].enabled=true;warnings[l].transform.position=new Vector3(gameCamera.transform.position.x+13,settings.lanes[l]+.35f,0);}
            warningActive=true;warningTime=Stage.warningSeconds;
        }
        void SpawnWave()
        {
            foreach(var w in warnings)if(w)w.enabled=false;
            foreach(int l in waveLanes){var s=Instantiate(stormPrefab,hazardRoot);s.transform.position=new Vector3(Mathf.Min(settings.ground.xMax+3,gameCamera.transform.position.x+17),settings.lanes[l],0);s.speed=Stage.windSpeed;storms.Add(s);}
            warningActive=false;waveTime=Stage.waveInterval;
        }
        void AlignArtwork()
        {
            if(!body.sprite)return;
            // Imported 16 PPU originals are scaled to 32 PPU without changing the source import.
            float scale=body.sprite.pixelsPerUnit/32f;body.transform.localScale=Vector3.one*scale;
            var b=body.sprite.bounds;body.transform.localPosition=new Vector3((body.flipX?b.center.x:-b.center.x)*scale,-b.min.y*scale,0);
            body.sortingOrder=100-Mathf.RoundToInt(player.position.y*10);
        }
        void FollowCamera()
        {
            float half=15;float x=Mathf.Clamp(player.position.x+7,settings.ground.xMin-1.5f+half,settings.ground.xMax+3.5f-half);
            gameCamera.transform.position=new Vector3(Mathf.Round(x*32)/32f,0,-10);
        }
        void RefreshUI()
        {
            if(!dayText)return;
            dayText.text=$"광야 · {Day} / 40일   {StageIndex+1}/8단계 · {(Night?"밤":"낮")}";
            statusText.text=Restarting?"1일째부터 다시 시작":Finished?"40일 완료":Sheltered?"엄폐 중 · 바람이 지나가면 다시 전진":warningActive?"모래바람 예고 · 오른쪽에서 다가옵니다":$"{(Night?"추위":"더위")} 노출 {exposure:0.0}초 · 바위 왼쪽 / 나무 뒤로 피하세요";
            instructionsText.text=stageTime>=Stage.seconds?"오른쪽 구간 끝까지 이동하세요 · R 처음부터":player.position.x>=Gate-.05f?$"이 구간의 바람을 피하며 버티세요 · 다음 길까지 {Mathf.CeilToInt(Stage.seconds-stageTime)}초 · R 처음부터":"방향키 / WASD: 한 칸 이동 · R 처음부터 · 지침 0칸이면 재시작";
            for(int i=0;i<staminaSlots.Length;i++)staminaSlots[i].color=i<Stamina?new Color(.91f,.69f,.31f):new Color(.22f,.25f,.28f);
            if(nightOverlay)nightOverlay.color=new Color(.04f,.09f,.25f,Night?.37f:0);
        }
    }
}
