using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace CrossTheLine.Cana
{
 [System.Serializable] public sealed class CanaSeat
 {
  public Transform point;
  public bool facesLeft;
  public int table;
  public CanaDirtyMarker dirtyMarker;
  [HideInInspector] public bool dirty;
 }
 public sealed class CanaCustomerSpawner : MonoBehaviour
 {
  public CanaCustomer customerPrefab;
  public CanaCustomer[] normalPrefabs;
  public CanaCustomer gluttonPrefab, impatientPrefab, troublemakerPrefab, groupPrefab;
  public CanaCustomer[] troublemakerPrefabs;
  [Header("NPC animation speeds")]
  [Min(0)] public float moveAnimationSpeed=1, idleAnimationSpeed=1;
  [Min(0.01f)] public float drinkAnimationSpeed=1;
  public Transform spawnPoint, moneyPoint;
  public CanaNavigation navigation;
  public CanaDissatisfaction dissatisfaction;
  public CanaWallet wallet;
  public CanaRound round;
  public CanaInventory inventory;
  public CanaStaffManager staff;
  public CanaSeat[] seats;
  public bool spawnOnStart=true;
  public bool simulateEverySeat;
  [Min(0.1f)] public float spawnInterval=5;
  [Range(0,0.25f)] public float specialChance=0.15f;
  [Min(0.1f)] public float groupSpawnGap=0.4f, troubleTrashInterval=3;
  public SpriteRenderer[] placementPreviews;
  public CanaCustomer ActiveCustomer { get; private set; }
  public float DirtySeatRatio => seats.Length == 0 ? 0 : (float)seats.Count(seat=>seat.dirty)/seats.Length;
  private CanaCustomer[] occupants;
  private bool[] reserved;
  private CanaPlayer[] cleaners;
  private Component[] staffCleaners;
  private float nextSpawnTime, nextGroupMember;
  private int introduced, reservedTable=-1;
  private readonly Queue<CanaCustomerKind> guaranteed=new Queue<CanaCustomerKind>();
  private bool waitingForGroup;
  private CanaParty spawningParty;
  private int[] groupSeats;
  private int groupIndex;
  private void Start()
  {
   foreach(var preview in placementPreviews) if(preview) preview.enabled=false;
   navigation.Rebuild();
   occupants=new CanaCustomer[seats.Length];reserved=new bool[seats.Length];cleaners=new CanaPlayer[seats.Length];staffCleaners=new Component[seats.Length];
   foreach(var seat in seats){seat.dirty=false;seat.dirtyMarker.gameObject.SetActive(false);}
   if(spawnOnStart) SpawnRandom(CanaCustomerKind.Normal);
   nextSpawnTime=Time.time+spawnInterval;
  }
  private void Update()
  {
   if(round.Finished || (inventory && inventory.ArrivalsPaused))return;
   if(round.Elapsed>=160 && introduced<3 && reservedTable<0)
    reservedTable=Enumerable.Range(0,6).OrderBy(t=>Enumerable.Range(0,seats.Length).Count(i=>seats[i].table==t && (occupants[i] || seats[i].dirty))).First();
   while(introduced<4 && round.Elapsed>=(introduced+1)*60)
   {introduced++;guaranteed.Enqueue((CanaCustomerKind)introduced);nextSpawnTime=Time.time;}
   if(spawningParty)
   {
    if(Time.time>=nextGroupMember)
    {
     var npc=SpawnAt(groupSeats[groupIndex],CanaCustomerKind.Group,spawningParty,groupIndex==0);
     spawningParty.members[groupIndex++]=npc;
     nextGroupMember=Time.time+groupSpawnGap;
     if(groupIndex==4)spawningParty=null;
    }
    return;
   }
   if(waitingForGroup)
   {
    if(TryStartGroup())
    {
     waitingForGroup=false;
     if(guaranteed.Count>0 && guaranteed.Peek()==CanaCustomerKind.Group)guaranteed.Dequeue();
     nextSpawnTime=Time.time+spawnInterval;
    }
    return;
   }
   if(Time.time<nextSpawnTime)return;
   var kind=guaranteed.Count>0?guaranteed.Peek():PickKind();
   if(kind==CanaCustomerKind.Group)
   {
    if(TryRejectAtDoor(kind,out _))
    {
     reservedTable=-1;
     if(guaranteed.Count>0)guaranteed.Dequeue();
     nextSpawnTime=Time.time+spawnInterval;
    }
    else waitingForGroup=true;
    return;
   }
   if(SpawnRandom(kind) && guaranteed.Count>0)guaranteed.Dequeue();
   nextSpawnTime=Time.time+spawnInterval;
  }
  private CanaCustomerKind PickKind()
  {
   float roll=Random.value;
   for(int i=1;i<=introduced;i++)if(roll<i*specialChance)return (CanaCustomerKind)i;
   return CanaCustomerKind.Normal;
  }
  private bool Available(int i)=>!occupants[i] && !reserved[i] && !seats[i].dirty;
  public CanaCustomer SpawnCustomer()=>SpawnRandom(CanaCustomerKind.Normal);
  public CanaCustomer SpawnRandom(CanaCustomerKind kind)
  {
   if(round.Finished || (inventory && inventory.ArrivalsPaused))return null;
   if(TryRejectAtDoor(kind,out var rejected))return rejected;
   var choices=Enumerable.Range(0,seats.Length).Where(i=>Available(i) && seats[i].table!=reservedTable).ToArray();
   return choices.Length==0?null:SpawnAt(choices[Random.Range(0,choices.Length)],kind,null,false);
  }
  private bool TryRejectAtDoor(CanaCustomerKind kind,out CanaCustomer rejected)
  {
   rejected=null;
   if(Random.value>=DirtySeatRatio)return false;
   var prefab=kind==CanaCustomerKind.Glutton?gluttonPrefab:kind==CanaCustomerKind.Impatient?impatientPrefab:
    kind==CanaCustomerKind.Troublemaker?ChooseTroublemaker():normalPrefabs[Random.Range(0,normalPrefabs.Length)];
   rejected=Instantiate(prefab,spawnPoint.position,Quaternion.identity);
   rejected.name=kind+" - Dirty Restaurant Rejection";
   rejected.RejectAtDoor(this,kind);
   ActiveCustomer=rejected;
   return true;
  }
  public CanaCustomer SpawnCustomerAtSeat(int index)=>SpawnAt(index,CanaCustomerKind.Normal,null,false);
  public CanaCustomer SpawnAt(int index,CanaCustomerKind kind,CanaParty party,bool leader)
  {
   if(round.Finished || (inventory && inventory.ArrivalsPaused) || index<0 || index>=seats.Length || occupants[index] || seats[index].dirty)return null;
   if(reserved[index] && !party)return null;
   if(!navigation.TryRoute(spawnPoint.position,seats[index].point.position,out var path))return null;
   var prefab=kind==CanaCustomerKind.Glutton?gluttonPrefab:kind==CanaCustomerKind.Impatient?impatientPrefab:
    kind==CanaCustomerKind.Troublemaker?ChooseTroublemaker():kind==CanaCustomerKind.Group?groupPrefab:normalPrefabs[Random.Range(0,normalPrefabs.Length)];
   var npc=Instantiate(prefab,path[0],Quaternion.identity);
   if(kind==CanaCustomerKind.Group)
   {
    var appearance=normalPrefabs[Random.Range(0,normalPrefabs.Length)];
    npc.moveAnimation=appearance.moveAnimation;
    npc.idleAnimation=appearance.idleAnimation;
    npc.drinkAnimation=appearance.drinkAnimation;
    npc.characterSprite.sprite=appearance.characterSprite.sprite;
   }
   npc.name=kind+" - "+seats[index].point.name;occupants[index]=npc;reserved[index]=false;ActiveCustomer=npc;
   npc.Initialize(this,index,path,seats[index].facesLeft);npc.Configure(kind,party,leader);
   return npc;
  }
  private CanaCustomer ChooseTroublemaker() => troublemakerPrefabs != null && troublemakerPrefabs.Length > 0
   ? troublemakerPrefabs[Random.Range(0,troublemakerPrefabs.Length)] : troublemakerPrefab;

  public bool TryStartGroup()
  {
   if(round.Finished || (inventory && inventory.ArrivalsPaused))return false;
   int table=reservedTable;
   if(table<0)table=Enumerable.Range(0,6).Where(TableFree).DefaultIfEmpty(-1).First();
   if(table<0 || !TableFree(table))return false;
   groupSeats=Enumerable.Range(0,seats.Length).Where(i=>seats[i].table==table).ToArray();
   if(groupSeats.Length!=4)return false;
   foreach(int i in groupSeats)if(!navigation.TryRoute(spawnPoint.position,seats[i].point.position,out _))return false;
   foreach(int i in groupSeats)reserved[i]=true;
   spawningParty=new GameObject("Four-person Party").AddComponent<CanaParty>();
   groupIndex=0;nextGroupMember=Time.time;reservedTable=-1;return true;
  }
  private bool TableFree(int table)=>Enumerable.Range(0,seats.Length).Where(i=>seats[i].table==table).All(Available);
  public void VacateSeat(int index,CanaCustomer npc,bool eaten)
  {
   if(occupants[index]!=npc)return;
   occupants[index]=null;if(eaten)MarkDirty(index);
  }
  public void ReleaseSeat(int index,CanaCustomer npc)
  {if(occupants!=null && occupants[index]==npc)occupants[index]=null;}
  public void MarkDirty(int index){seats[index].dirty=true;seats[index].dirtyMarker.gameObject.SetActive(true);}
  public bool TryClean(CanaPlayer player)
  {
   if(round.Finished || player.IsCleaning)return false;
   Vector3 position=player.transform.position;
   int index=Enumerable.Range(0,seats.Length).Where(i=>seats[i].dirty && !occupants[i] && !cleaners[i] && !staffCleaners[i] && Vector2.Distance(position,seats[i].point.position)<=1.25f)
    .OrderBy(i=>Vector2.Distance(position,seats[i].point.position)).DefaultIfEmpty(-1).First();
   if(index<0)return false;
   if(!player.BeginCleaning(this,index,seats[index].facesLeft))return false;
   cleaners[index]=player;
   return true;
  }
  public void CompleteClean(int index,CanaPlayer player)
  {
   if(cleaners[index]!=player)return;
   cleaners[index]=null;
   seats[index].dirty=false;
   seats[index].dirtyMarker.gameObject.SetActive(false);
  }
  public void AbandonCleaning(int index,CanaPlayer player)
  {
   if(cleaners!=null && cleaners[index]==player)cleaners[index]=null;
  }
  public void ClearAllDirt()
  {
   foreach(var seat in seats){seat.dirty=false;seat.dirtyMarker.gameObject.SetActive(false);}
  }
  public bool CanStaffClean(int index,Component worker)=>index>=0 && index<seats.Length && seats[index].dirty && !occupants[index] && !cleaners[index] && (!staffCleaners[index] || staffCleaners[index]==worker);
  public bool TryBeginStaffClean(int index,Component worker)
  {
   if(!CanStaffClean(index,worker))return false;
   staffCleaners[index]=worker;return true;
  }
  public void CompleteStaffClean(int index,Component worker)
  {
   if(staffCleaners[index]!=worker)return;
   staffCleaners[index]=null;seats[index].dirty=false;seats[index].dirtyMarker.gameObject.SetActive(false);
  }
  public void CancelStaffClean(int index,Component worker)
  {if(staffCleaners!=null && staffCleaners[index]==worker)staffCleaners[index]=null;}
  public void ScatterTrash(int source)
  {
   if(staff && staff.CleanerHired && Random.value>=.5f)return;
   var choices=Enumerable.Range(0,seats.Length).Where(i=>Available(i) && Vector2.Distance(seats[i].point.position,seats[source].point.position)<4).ToArray();
   if(choices.Length>0)MarkDirty(choices[Random.Range(0,choices.Length)]);
  }
 }
}
