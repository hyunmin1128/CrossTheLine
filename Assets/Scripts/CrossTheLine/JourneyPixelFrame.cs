using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace CrossTheLine
{
    // Apply framing only after story simulation, and undo it before any controller updates.
    // Scaling the complete stage preserves hand contacts, paths and prop placement.
    [DefaultExecutionOrder(-31000), DisallowMultipleComponent]
    public sealed class JourneyPixelFrame : MonoBehaviour
    {
        public const float CameraSize=8.4375f;
        public float authoredScale=1.5f;
        Vector3 position,scale,cameraPosition;float cameraSize;bool presented;
        JourneyGame game;JourneyPixelFrameLate late;
        void Awake(){game=GetComponent<JourneyGame>();game.journeyCamera.orthographicSize=CameraSize/authoredScale;late=gameObject.AddComponent<JourneyPixelFrameLate>();late.frame=this;}
        void Update(){Restore();}
        public void Restore()
        {
            if(!presented)return;
            presented=false;
            transform.position=position;transform.localScale=scale;
            // Scene teardown can destroy the camera before disabling this component.
            if(game&&game.journeyCamera){game.journeyCamera.transform.position=cameraPosition;game.journeyCamera.orthographicSize=cameraSize;}
        }
        public void Present()
        {
            if(!game||!game.journeyCamera||presented||game.Mode=="menu")return;
            var world=GetComponent<JourneyContinuousWorld>();
            float factor=world&&world.ActiveSection?world.ActiveSection.pixelFrameScale:authoredScale;
            Vector3 origin=world&&world.ActiveSection?world.ActiveSection.transform.position:Vector3.zero;
            var camera=game.journeyCamera;
            position=transform.position;scale=transform.localScale;cameraPosition=camera.transform.position;cameraSize=camera.orthographicSize;
            transform.position=origin+(position-origin)*factor;transform.localScale=scale*factor;
            Vector3 p=origin+(cameraPosition-origin)*factor;p.z=cameraPosition.z;
            p.x=Mathf.Round(p.x*32)/32;p.y=Mathf.Round(p.y*32)/32;
            camera.transform.position=p;camera.orthographicSize=CameraSize;
            camera.allowHDR=false;camera.allowMSAA=false;camera.allowDynamicResolution=false;
            var pixel=camera.GetComponent<PixelPerfectCamera>();
            if(pixel){pixel.assetsPPU=32;pixel.refResolutionX=960;pixel.refResolutionY=540;pixel.gridSnapping=PixelPerfectCamera.GridSnapping.PixelSnapping;pixel.cropFrame=PixelPerfectCamera.CropFrame.Windowbox;pixel.enabled=true;}
            presented=true;
        }
        void OnDisable(){Restore();}
    }
}
