using System;
using System.IO;
using System.Diagnostics;
using UnityEditor;

namespace CrossTheLine.EditorTools
{
    public static class Chapter08FishingArtExporter
    {
        [MenuItem("CrossTheLine/Chapter 08/Export authored Aseprite layers")]
        public static void Export()
        {
            string executable=EditorPrefs.GetString("CrossTheLine.AsepriteExecutable","C:/Program Files (x86)/Steam/steamapps/common/Aseprite/Aseprite.exe");
            if(!File.Exists(executable)){executable=EditorUtility.OpenFilePanel("Aseprite 실행 파일 선택","","exe");if(string.IsNullOrEmpty(executable))return;EditorPrefs.SetString("CrossTheLine.AsepriteExecutable",executable);}
            string project=Directory.GetParent(UnityEngine.Application.dataPath).FullName;
            string output=Path.Combine(project,"Assets/Sprite/Minigames/Chapter08/ExportedLayers");Directory.CreateDirectory(output);
            AssetDatabase.StartAssetEditing();
            try
            {
                string source=Path.Combine(project,"Assets/Sprite/Background/Chapter8-Minigame-Sea.aseprite");
                string[] layers={"Sea","Throw Net","Peter&Andrew","boat"},names={"Sea","ThrowNet","PeterAndrew","Boat"};
                for(int i=0;i<layers.Length;i++)Run(executable,"--batch --all-layers --layer "+Quote(layers[i])+" "+Quote(source)+" --save-as "+Quote(Path.Combine(output,names[i]+".png")),project);
                Run(executable,"--batch --all-layers "+Quote(Path.Combine(project,"Assets/Sprite/Objects/Chapter8/Net.aseprite"))+" --save-as "+Quote(Path.Combine(output,"Net.png")),project);
            }
            finally{AssetDatabase.StopAssetEditing();AssetDatabase.Refresh();}
            UnityEngine.Debug.Log("Chapter08: 원본 레이어 PNG를 갱신했습니다. 기존 .meta와 스프라이트 참조는 유지됩니다.");
        }
        static string Quote(string value)=>"\""+value.Replace("\"","\\\"")+"\"";
        static void Run(string executable,string arguments,string directory)
        {
            var info=new ProcessStartInfo(executable,arguments){WorkingDirectory=directory,UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden};
            using(var process=Process.Start(info))
            {
                if(process==null)throw new InvalidOperationException("Aseprite 배치 실행 실패");
                if(!process.WaitForExit(10000))throw new TimeoutException("Aseprite 내보내기가 끝나지 않았습니다. 원본은 변경하지 않았습니다.");
                if(process.ExitCode!=0)throw new InvalidOperationException("Aseprite 내보내기 종료 코드: "+process.ExitCode);
            }
        }
    }
}
