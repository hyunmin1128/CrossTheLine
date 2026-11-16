using TMPro;
using UnityEngine;

namespace CrossTheLine.Minigames
{
    /// <summary>Sizes the existing sibling background and label without rebuilding the HUD.</summary>
    [ExecuteAlways,DisallowMultipleComponent]
    public sealed class FastingHeaderFit : MonoBehaviour
    {
        public TMP_Text heading;
        public Vector2 padding=new Vector2(16,10);
        public float minimumWidth=120,minimumHeight=40,rightMargin=12;
        RectTransform panel;
        string previousText;
        TMP_FontAsset previousFont;
        float previousSize=-1,previousCanvasWidth=-1;
        Vector2 previousPadding;
        void OnEnable(){Refresh(true);}
        void LateUpdate(){Refresh();}
        void OnValidate(){previousSize=-1;}
        public void Refresh(bool force=false)
        {
            if(!heading)return;if(!panel)panel=GetComponent<RectTransform>();if(!panel)return;
            var parent=panel.parent as RectTransform;if(!parent)return;float canvasWidth=parent.rect.width;
            if(!force&&heading.text==previousText&&heading.font==previousFont&&Mathf.Approximately(heading.fontSize,previousSize)&&Mathf.Approximately(canvasWidth,previousCanvasWidth)&&padding==previousPadding)return;
            previousText=heading.text;previousFont=heading.font;previousSize=heading.fontSize;previousCanvasWidth=canvasWidth;previousPadding=padding;
            Vector2 natural=heading.GetPreferredValues(heading.text,float.PositiveInfinity,float.PositiveInfinity);
            float available=Mathf.Max(1,canvasWidth-panel.anchoredPosition.x-rightMargin);
            float width=Mathf.Clamp(Mathf.Ceil(natural.x+padding.x*2),Mathf.Min(minimumWidth,available),available);
            float textWidth=Mathf.Max(1,width-padding.x*2);Vector2 wrapped=heading.GetPreferredValues(heading.text,textWidth,float.PositiveInfinity);
            float height=Mathf.Max(minimumHeight,Mathf.Ceil(wrapped.y+padding.y*2));
            panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,width);panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,height);
            var label=heading.rectTransform;label.anchorMin=label.anchorMax=panel.anchorMin;label.pivot=panel.pivot;
            label.anchoredPosition=panel.anchoredPosition+new Vector2(padding.x,-padding.y);label.sizeDelta=new Vector2(textWidth,Mathf.Max(1,height-padding.y*2));
        }
    }
}
