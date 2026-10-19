#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using ZombieCardSurvive.Exploration.UI;

namespace ZombieCardSurvive.UI.World.Editor
{
    public static class ExplorationPresentationSetupUtility
    {
        private const string Paper = "Assets/Art/GameplayHUD/PaperUI/";
        private const string Icons = "Assets/Art/GameplayHUD/Icons/Review01/EventInline_food.asset";

        [MenuItem("Tools/Zombie Card Survive/Gameplay HUD/Apply Exploration Presentation")]
        public static void ApplyLoaded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new System.InvalidOperationException("Stop Play before applying exploration presentation.");
            foreach (var view in Object.FindObjectsOfType<ExplorationFlowView>(true))
            {
                if (!view.gameObject.scene.IsValid() || !view.gameObject.scene.isLoaded) continue;
                Apply(view);
                EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
            }
        }

        public static void Apply(ExplorationFlowView view)
        {
            var so = new SerializedObject(view);
            var root = (GameObject)so.FindProperty("root").objectReferenceValue;
            if (root == null) return;
            var spriteAsset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(Icons);
            Surface(root, "ui_background_archival_paper_01.png", false);
            TMP_Text title = Ref<TMP_Text>(so, "titleText");
            TMP_Text description = Ref<TMP_Text>(so, "descriptionText");
            Text(title, spriteAsset, 32, 42, .05f, .84f, .77f, .95f);
            Text(description, spriteAsset, 22, 28, .05f, .75f, .94f, .84f);
            var cards = so.FindProperty("regionCards");
            int count = cards.arraySize;
            for (int i = 0; i < count; i++)
            {
                var card = cards.GetArrayElementAtIndex(i).objectReferenceValue as ExplorationRegionCardView;
                if (card == null) continue;
                float width = (.9f - .02f * (count - 1)) / Mathf.Max(1, count);
                float left = .05f + i * (width + .02f);
                var cardRect = card.GetComponent<RectTransform>();
                if (cardRect == null) cardRect = Undo.AddComponent<RectTransform>(card.gameObject);
                Place(cardRect, left, .14f, left + width, .72f);
                var panel = card.transform.Find("Region1");
                if (panel == null) panel = card.transform;
                if (panel != card.transform) Place((RectTransform)panel, 0, 0, 1, 1);
                Surface(panel.gameObject, "ui_panel_paper_light_9slice_01.png", true);
                var cs = new SerializedObject(card);
                Text(Ref<TMP_Text>(cs,"nameText"), spriteAsset, 26, 34, .06f,.85f,.94f,.97f);
                Text(Ref<TMP_Text>(cs,"descriptionText"), spriteAsset, 22, 26, .06f,.30f,.94f,.49f);
                Text(Ref<TMP_Text>(cs,"dangerText"), spriteAsset, 22, 26, .06f,.17f,.94f,.29f);
                Text(Ref<TMP_Text>(cs,"rewardsText"), spriteAsset, 22, 26, .06f,.03f,.94f,.16f);
                TMP_Text danger = Ref<TMP_Text>(cs,"dangerText");
                if (danger != null) danger.color = new Color32(137,42,35,255);
                Image art = Ref<Image>(cs,"artworkImage");
                if (art == null)
                {
                    var artRect = Child(panel, "RegionArtwork", typeof(Image));
                    art = artRect.GetComponent<Image>();
                    cs.FindProperty("artworkImage").objectReferenceValue = art;
                }
                Place(art.rectTransform,.07f,.51f,.93f,.83f);
                Undo.RecordObject(art,"Configure exploration artwork");
                art.preserveAspect = true;
                art.raycastTarget = false;
                art.enabled = art.sprite != null;
                // Legacy scene had a separate white placeholder, never bound to the view.
                var legacy = panel.Find("Image");
                var placeholder = legacy != null ? legacy.GetComponent<Image>() : null;
                if (placeholder != null && placeholder != art && placeholder.sprite == null)
                {
                    Undo.RecordObject(placeholder, "Hide unused exploration placeholder");
                    placeholder.enabled = false;
                }
                cs.ApplyModifiedProperties();
            }
            Button close = Ref<Button>(so,"closeButton");
            if (close != null)
            {
                Place((RectTransform)close.transform,.76f,.04f,.94f,.12f);
                Surface(close.gameObject,"ui_panel_paper_dark_9slice_01.png",true);
                foreach(var t in close.GetComponentsInChildren<TMP_Text>(true))
                { Undo.RecordObject(t,"Style exploration continue"); t.text="繼續"; t.color=new Color32(232,229,218,255); }
            }
            if (description == null) return;
            RectTransform result = Child(root.transform,"ExplorationResultScroll",typeof(Image),typeof(ScrollRect));
            Place(result,.06f,.17f,.94f,.73f);
            Surface(result.gameObject,"ui_panel_paper_light_9slice_01.png",true);
            RectTransform viewport = Child(result,"Viewport",typeof(RectMask2D));
            Place(viewport,.055f,.14f,.945f,.86f);
            RectTransform content = Child(viewport,"Content",typeof(TextMeshProUGUI),typeof(ContentSizeFitter));
            var text=content.GetComponent<TextMeshProUGUI>();
            Undo.RecordObject(text,"Configure exploration result text");
            text.font=description.font;
            text.spriteAsset=spriteAsset;
            text.richText=true;
            text.fontSize=28;
            text.enableAutoSizing=false;
            text.enableWordWrapping=true;
            text.alignment=TextAlignmentOptions.TopLeft;
            text.color=new Color32(49,48,44,255);
            text.raycastTarget=false;
            text.lineSpacing=6;
            content.anchorMin=new Vector2(0,1); content.anchorMax=Vector2.one;
            content.pivot=new Vector2(.5f,1); content.anchoredPosition=Vector2.zero; content.sizeDelta=Vector2.zero;
            var fitter=content.GetComponent<ContentSizeFitter>();
            Undo.RecordObject(fitter,"Size result content");
            fitter.horizontalFit=ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            var scroll=result.GetComponent<ScrollRect>();
            Undo.RecordObject(scroll,"Configure result scroll");
            scroll.viewport=viewport; scroll.content=content; scroll.horizontal=false; scroll.vertical=true;
            scroll.movementType=ScrollRect.MovementType.Clamped; scroll.scrollSensitivity=28;
            so.FindProperty("resultSummaryText").objectReferenceValue=text;
            so.FindProperty("resultPanel").objectReferenceValue=result.gameObject;
            so.ApplyModifiedProperties();
            result.gameObject.SetActive(false);
        }

        private static T Ref<T>(SerializedObject so,string name) where T:Object
            => so.FindProperty(name).objectReferenceValue as T;

        private static RectTransform Child(Transform parent,string name,params System.Type[] types)
        {
            Transform found=parent.Find(name);
            if(found!=null) return (RectTransform)found;
            var go=new GameObject(name,typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go,"Create exploration presentation");
            go.transform.SetParent(parent,false); go.layer=parent.gameObject.layer;
            foreach(var type in types) Undo.AddComponent(go,type);
            return (RectTransform)go.transform;
        }

        private static void Place(RectTransform rect,float x0,float y0,float x1,float y1)
        {
            Undo.RecordObject(rect,"Layout exploration presentation");
            rect.anchorMin=new Vector2(x0,y0); rect.anchorMax=new Vector2(x1,y1);
            rect.offsetMin=rect.offsetMax=Vector2.zero;
            rect.localScale=Vector3.one;
        }

        private static void Text(TMP_Text text,TMP_SpriteAsset asset,float min,float max,float x0,float y0,float x1,float y1)
        {
            if(text==null)return;
            Place(text.rectTransform,x0,y0,x1,y1);
            Undo.RecordObject(text,"Style exploration text");
            text.spriteAsset=asset; text.richText=true; text.enableAutoSizing=true;
            text.fontSizeMin=min; text.fontSizeMax=max; text.enableWordWrapping=true;
            text.color=new Color32(49,48,44,255); text.alignment=TextAlignmentOptions.MidlineLeft;
            text.raycastTarget=false;
        }

        private static void Surface(GameObject go,string filename,bool sliced)
        {
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Paper+filename);
            if(sprite==null)throw new System.InvalidOperationException("Missing paper: "+filename);
            var image=go.GetComponent<Image>();
            if(image==null)image=Undo.AddComponent<Image>(go);
            Undo.RecordObject(image,"Style exploration surface");
            image.sprite=sprite; image.color=Color.white;
            image.type=sliced?Image.Type.Sliced:Image.Type.Simple;
        }
    }
}
#endif
