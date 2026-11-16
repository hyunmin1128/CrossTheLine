using System;
using System.IO;
using System.Text;
using UnityEngine;
using CrossTheLine.Minigames;
public static class ExportActualVignetteImages
{
    static void Export(FastingHitVignette fx,string path)
    {
        Canvas.ForceUpdateCanvases();var mesh=fx.canvasRenderer.GetMesh();var colors=mesh.colors;
        const int width=960,height=540,columns=48,rows=28;
        if(mesh.vertexCount!=columns*rows*4)throw new Exception("Vignette mesh layout changed");
        var pixels=new Color32[width*height];
        for(int y=0;y<height;y++)for(int x=0;x<width;x++)
        {
            float gx=(x+.5f)/width*columns,gy=(y+.5f)/height*rows;int cx=Mathf.FloorToInt(gx),cy=Mathf.FloorToInt(gy),i=(cy*columns+cx)*4;float u=gx-cx,v=gy-cy;
            // Reproduce the actual UI triangles' vertex-color interpolation, with straight alpha for PNG.
            Color c=v>=u?colors[i]*(1-v)+colors[i+1]*(v-u)+colors[i+2]*u:colors[i]*(1-u)+colors[i+2]*v+colors[i+3]*(u-v);
            pixels[y*width+x]=c;
        }
        var texture=new Texture2D(width,height,TextureFormat.RGBA32,false);texture.SetPixels32(pixels);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.Destroy(texture);
    }
    public static string Run()
    {
        var g=UnityEngine.Object.FindAnyObjectByType<FastingTopDownGame>();if(!Application.isPlaying||!g)throw new Exception("Play scene");bool enabled=g.enabled;g.enabled=false;
        string dir="Docs/Exports/Vignette-Actual-20261116";Directory.CreateDirectory(dir);var info=new StringBuilder("Actual code-generated vignette meshes, peak snapshot at 0.20 seconds. Transparent straight-alpha PNG, 960x540. No generated artwork and no artificial opacity increase.\n");
        try
        {
            foreach(FastingTopDownHazard.Kind kind in Enum.GetValues(typeof(FastingTopDownHazard.Kind)))
            {
                g.hitVignette.PlayDamage(kind);g.hitVignette.Advance(.2f);Export(g.hitVignette,dir+"/"+kind+".png");info.AppendLine(kind+": "+g.hitVignette.CurrentDuration.ToString("0.000")+" sec");
            }
            g.hitVignette.Play(false);g.hitVignette.Advance(.2f);Export(g.hitVignette,dir+"/CommonDamage.png");g.hitVignette.Play(true);g.hitVignette.Advance(.2f);Export(g.hitVignette,dir+"/Shield.png");
            File.WriteAllText(dir+"/Readme.txt",info.ToString());return "Exported 16 actual transparent vignette PNGs to "+dir;
        }
        finally{g.hitVignette.Clear();g.enabled=enabled;}
    }
}
