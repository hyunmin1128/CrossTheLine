using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CrossTheLine
{
    public sealed class JourneySceneBindings : MonoBehaviour
    {
        public Canvas canvas;
        public GameObject titlePanel, header, objective, journalButton, menuButton;
        public TMP_Text chapterTitle, location, objectiveText, actorText, controls, toast;
        public Button newJourney, continueJourney, settings;
        public GameObject panelTemplate, textTemplate, buttonTemplate;
        JourneyGame owner;
        public void Bind(JourneyGame game) { owner=game;HideTitle(); }
        public void HideTitle() { if(titlePanel)titlePanel.SetActive(false); }
        public void OpenTitle(bool canContinue) { if(titlePanel)titlePanel.SetActive(true);if(continueJourney)continueJourney.interactable=canContinue; }
        public void NewJourney() { owner?.BeginNewJourneyFromTitle(); }
        public void ContinueJourney() { owner?.ContinueGame(); }
        public void Settings() { owner?.ShowSettings(); }
        public GameObject MakeUI(string kind,string name,Transform parent)
        {
            var template=kind=="Panel"?panelTemplate:kind=="Text"?textTemplate:buttonTemplate;
            if(!template)throw new System.InvalidOperationException("Scene UI template missing: "+kind);
            var go=Instantiate(template,parent,false);go.name=name;foreach(var target in go.GetComponentsInChildren<JourneyEditableVisual>(true)){string path=UnityRelativePath(target.transform,go.transform);var source=path.Length==0?template.transform:template.transform.Find(path);if(source)target.BindTemplate(source.gameObject);}go.SetActive(true);return go;
        }
        static string UnityRelativePath(Transform node,Transform root){string path="";while(node!=root&&node){path=node.name+(path.Length>0?"/"+path:"");node=node.parent;}return path;}
    }
}
