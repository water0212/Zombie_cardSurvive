#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ZombieCardSurvive.Buildings.Data;
using ZombieCardSurvive.Buildings.UI;

namespace ZombieCardSurvive.Buildings.Editor
{
    /// <summary>Scoped, repeatable presentation migration. Never saves a scene or changes economy data.</summary>
    public static class BuildingBookmarkPanelSetupUtility
    {
        private const string Folder = "Assets/Prefab/Buildings/";
        private static TMP_FontAsset font;
        private static TMP_SpriteAsset icons;
        private static Sprite paper;
        private static Sprite dark;
        private static readonly Color Ink = new Color32(49, 48, 44, 255);
        private static readonly Color Chalk = new Color32(232, 229, 218, 255);

        [MenuItem("Tools/Zombie Card Survive/Gameplay HUD/Apply Building Bookmark Panel")]
        public static void ApplyLoaded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before applying building UI.");
            LoadAssets();
            EditPrefab("CraftRecipeRow.prefab", go => Recipe(go));
            EditPrefab("CraftJobRow.prefab", go => Job(go));
            EditPrefab("CardCraftView.prefab", go => Craft(go));
            EditPrefab("BuildingBookmarkButton.prefab", go => { Button b = Get<Button>(go); Surface(go, false); TMP_Text label=Label(go.transform, "Label", "功能", 26);label.alignment=TextAlignmentOptions.Midline;label.margin=new Vector4(12,6,12,6); Height(go, 80); });
            EditPrefab("BaseDetailsPage.prefab", go => {
                Surface(go, false); DisableLayout(go);
                BaseDetailsPageView view = Get<BaseDetailsPageView>(go);
                ScrollRect scroll = Scroll(go.transform, "DetailsScroll", Vector2.zero, Vector2.one);
                TMP_Text text = Label(scroll.content, "Details", "基地詳情（目前）", 24);
                AutoText(text); Ref(view, "detailText", text);
            });
            EditPrefab("FoodPlanInteractionView.prefab", go => SimplePage(go, true));
            EditPrefab("CardUpgradeView.prefab", go => SimplePage(go, false));
            EditPrefab("BuildingDetailView.prefab", go => Apply(Get<BuildingDetailView>(go)));
            ConfigureIntel();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded || scene.path != "Assets/Scenes/SampleScene.unity") continue;
                foreach (GameObject root in scene.GetRootGameObjects())
                    foreach (BuildingDetailView view in root.GetComponentsInChildren<BuildingDetailView>(true))
                    {
                        Undo.RegisterFullObjectHierarchyUndo(view.gameObject, "Apply Building Bookmark Panel");
                        Apply(view);
                    }
                EditorSceneManager.MarkSceneDirty(scene);
            }
            Debug.Log("Building bookmark prefabs updated. SampleScene changes are unsaved; review and save explicitly. Economy and queues were not changed.");
        }

        public static void Apply(BuildingDetailView view)
        {
            LoadAssets();
            // Prefab hierarchy is authored above. Repair references and the approved
            // panel layout without reparenting inherited prefab children.
            if (view.gameObject.scene.path == "Assets/Scenes/SampleScene.unity" && PrefabUtility.IsPartOfPrefabInstance(view))
            {
                RebindSceneInstance(view);
                return;
            }
            GameObject root = Read<GameObject>(view, "root") ?? view.gameObject;
            Ref(view, "root", root);
            DisableLayout(root);
            Rect(root.transform, new Vector2(.025f, .035f), new Vector2(.975f, .965f));
            Surface(root, true);
            RectTransform left = Child(root.transform, "BookmarkBuildingInfo");
            Rect(left, new Vector2(.015f, .025f), new Vector2(.26f, .96f));
            ScrollRect info = Scroll(left, "InfoScroll", new Vector2(0, .19f), Vector2.one);
            foreach (string field in new[] { "titleText", "statusText", "rewardText", "requirementText" })
            {
                TMP_Text text = Read<TMP_Text>(view, field);
                if (text == null)
                {
                    string legacy = field == "titleText" ? "Title" : field == "statusText" ? "Status" : field == "rewardText" ? "Reward" : "Requirement";
                    Transform existing = FindUniqueOwned(root.transform, legacy);
                    if (existing != null) text = existing.GetComponent<TMP_Text>();
                    if (text != null) Ref(view, field, text);
                }
                if (text == null) { text = Label(info.content, field, "", field == "titleText" ? 30 : 23); Ref(view, field, text); }
                if (text.transform.parent != info.content) text.transform.SetParent(info.content, false); Style(text, field == "titleText" ? 30 : 23); text.color = Chalk; AutoText(text);
            }
            PositionButton(view, "constructButton", left, new Vector2(0, .095f), new Vector2(1, .18f));
            PositionButton(view, "cancelConstructionButton", left, Vector2.zero, new Vector2(1, .085f));
            PositionButton(view, "closeButton", root.transform, new Vector2(.88f, .925f), new Vector2(.98f, .985f));
            ScrollRect bookmarks = Scroll(root.transform, "BookmarkNavigation", new Vector2(.27f, .03f), new Vector2(.40f, .91f));
            Ref(view, "bookmarkRoot", bookmarks.content);
            Ref(view, "bookmarkButtonPrefab", AssetDatabase.LoadAssetAtPath<Button>(Folder + "BuildingBookmarkButton.prefab"));
            Transform host = Read<Transform>(view, "interactionRoot") ?? Child(root.transform, "Interactions");
            DisableLayout(host.gameObject); Rect(host, new Vector2(.415f, .035f), new Vector2(.98f, .865f));
            Ref(view, "interactionRoot", host);
            TMP_Text status = Label(root.transform, "BookmarkPageStatus", "", 22);
            status.color = Chalk; Rect(status.transform, new Vector2(.415f, .87f), new Vector2(.87f, .975f));
            Ref(view, "pageStatusText", status);
            Ref(view, "cardCraftPrefab", AssetDatabase.LoadAssetAtPath<CardCraftView>(Folder + "CardCraftView.prefab"));
            Ref(view, "foodPlanPrefab", AssetDatabase.LoadAssetAtPath<FoodPlanInteractionView>(Folder + "FoodPlanInteractionView.prefab"));
            Ref(view, "cardUpgradePrefab", AssetDatabase.LoadAssetAtPath<CardUpgradeView>(Folder + "CardUpgradeView.prefab"));
            EditorUtility.SetDirty(view);
            PrefabUtility.RecordPrefabInstancePropertyModifications(view);
        }

        private static void Craft(GameObject go)
        {
            CardCraftView view = Get<CardCraftView>(go); Surface(go, false); DisableLayout(go);
            TMP_Text title = Label(go.transform, "Title", "製作列", 28); Rect(title.transform, new Vector2(.025f,.91f), new Vector2(.69f,.975f));
            TMP_Text production = Label(go.transform, "Production", "產能", 26); Rect(production.transform, new Vector2(.025f,.83f), new Vector2(.69f,.91f));
            TMP_Text availability = Label(go.transform, "Availability", "", 24); Rect(availability.transform,new Vector2(.025f,.73f),new Vector2(.69f,.83f));
            ScrollRect recipes = Scroll(go.transform, "RecipeScroll", new Vector2(.02f,.04f),new Vector2(.68f,.72f));
            ScrollRect queue = Scroll(go.transform, "QueueScroll", new Vector2(.71f,.18f),new Vector2(.98f,.85f));
            TMP_Text queueTitle = Label(go.transform,"QueueTitle","製作序列",26); Rect(queueTitle.transform,new Vector2(.71f,.86f),new Vector2(.97f,.975f));
            ScrollRect selectedScroll = Scroll(go.transform,"SelectedJobScroll",new Vector2(.71f,.01f),new Vector2(.98f,.17f));
            Transform legacySelected=go.transform.Find("SelectedJob"); if(legacySelected!=null)legacySelected.SetParent(selectedScroll.content,false);
            TMP_Text selected = Label(selectedScroll.content,"SelectedJob","",24); AutoText(selected);
            TMP_Text emptyRecipes = Label(recipes.content,"EmptyRecipes","沒有食譜",22); AutoText(emptyRecipes);
            TMP_Text emptyQueue = Label(queue.content,"EmptyQueue","目前沒有工作",20); AutoText(emptyQueue);
            // Retire only legacy roots owned by this craft prefab, not any scene hierarchy.
            foreach (string name in new[] { "Recipes", "Queue" }) { Transform old=go.transform.Find(name); if(old!=null) old.gameObject.SetActive(false); }
            Ref(view,"titleText",title); Ref(view,"productionText",production); Ref(view,"availabilityText",availability);
            Ref(view,"recipeRoot",recipes.content); Ref(view,"queueRoot",queue.content);
            Ref(view,"recipeScrollRect",recipes); Ref(view,"queueScrollRect",queue);
            Ref(view,"recipeRowPrefab",AssetDatabase.LoadAssetAtPath<CraftRecipeRowView>(Folder+"CraftRecipeRow.prefab"));
            Ref(view,"jobRowPrefab",AssetDatabase.LoadAssetAtPath<CraftJobRowView>(Folder+"CraftJobRow.prefab"));
            Ref(view,"emptyRecipesText",emptyRecipes); Ref(view,"emptyQueueText",emptyQueue); Ref(view,"selectedJobText",selected);
        }

        private static void Recipe(GameObject go)
        {
            CraftRecipeRowView view=Get<CraftRecipeRowView>(go); Surface(go,false); Vertical(go,8);
            Button header=Button(go.transform,"Expand","食譜",27); Height(header.gameObject,72);
            RectTransform detail=Child(go.transform,"Detail"); Vertical(detail.gameObject,10);
            Image art=Get<Image>(Child(detail,"Artwork").gameObject); art.preserveAspect=true; art.raycastTarget=false; Height(art.gameObject,100);
            TMP_Text text=Label(detail,"Description","產出與成本",26); AutoText(text);
            TMP_Text reason=Label(detail,"Reason","",25); AutoText(reason);
            Button add=Button(detail,"Add","加入製作",26); Height(add.gameObject,66);
            Ref(view,"expandButton",header); Ref(view,"addButton",add); Ref(view,"titleText",header.GetComponentInChildren<TMP_Text>(true));
            Ref(view,"detailText",text); Ref(view,"reasonText",reason); Ref(view,"detailRoot",detail.gameObject); Ref(view,"artwork",art);
        }

        private static void Job(GameObject go)
        {
            CraftJobRowView view=Get<CraftJobRowView>(go); Surface(go,false); Height(go,280);
            TMP_Text description=Read<TMP_Text>(view,"descriptionText") ?? Label(go.transform,"Description","製作工作",20);
            Style(description,24); Rect(description.transform,new Vector2(.07f,.19f),new Vector2(.93f,.67f));
            Image art=Get<Image>(Child(go.transform,"Artwork").gameObject); art.preserveAspect=true; art.raycastTarget=false;
            Rect(art.transform,new Vector2(.3f,.71f),new Vector2(.7f,.99f));
            TMP_Text hint=Label(go.transform,"Hint","左鍵查看／右鍵取消",20); Rect(hint.transform,new Vector2(.06f,.03f),new Vector2(.94f,.19f));
            Button old=Read<Button>(view,"cancelButton"); if(old!=null)old.gameObject.SetActive(false);
            Ref(view,"descriptionText",description); Ref(view,"artwork",art); Ref(view,"hintText",hint);
        }

        private static void SimplePage(GameObject go,bool food)
        {
            Surface(go,false); DisableLayout(go); Component view=food?(Component)Get<FoodPlanInteractionView>(go):Get<CardUpgradeView>(go);
            ScrollRect scroll=Scroll(go.transform,"BookmarkPageScroll",new Vector2(.02f,.02f),new Vector2(.98f,.98f));
            foreach(string field in food?new[]{"titleText","descriptionText","stateText"}:new[]{"titleText","descriptionText","costText"})
            { TMP_Text text=Read<TMP_Text>(view,field); if(text==null)continue; text.transform.SetParent(scroll.content,false); Style(text,24);AutoText(text); }
            foreach(string field in food?new[]{"normalPlanButton","generousPlanButton"}:new[]{"upgradeButton"})
            {Button b=Read<Button>(view,field);if(b==null)continue;b.transform.SetParent(scroll.content,false);Surface(b.gameObject,false);Height(b.gameObject,68);foreach(TMP_Text t in b.GetComponentsInChildren<TMP_Text>(true))Style(t,24);}
        }

        private static void ConfigureIntel()
        {
            BuildBaseData intel=null;
            foreach(string guid in AssetDatabase.FindAssets("t:BuildBaseData"))
            {BuildBaseData data=AssetDatabase.LoadAssetAtPath<BuildBaseData>(AssetDatabase.GUIDToAssetPath(guid));if(data!=null&&data.BuildingId=="building_intel_center") { if(intel!=null)throw new InvalidOperationException("Duplicate building_intel_center IDs; configuration not changed.");intel=data;}}
            if(intel==null) { Debug.LogWarning("No building_intel_center asset; base details registration skipped.");return; }
            if(intel.Presentation!=null) {Validate(intel.Presentation); return;}
            string path="Assets/ScriptObj/BuildingScriptObj/IntelBookmarkPresentation.asset";
            BuildingPresentationData config=AssetDatabase.LoadAssetAtPath<BuildingPresentationData>(path);
            if(config==null)
            {
                config=ScriptableObject.CreateInstance<BuildingPresentationData>();
                SerializedObject so=new SerializedObject(config); SerializedProperty pages=so.FindProperty("pages"); pages.arraySize=intel.Interactions.Count+1;
                for(int i=0;i<pages.arraySize;i++)
                {
                    SerializedProperty page=pages.GetArrayElementAtIndex(i);bool info=i==intel.Interactions.Count;
                    page.FindPropertyRelative("pageId").stringValue=info?"base-details":"interaction-"+intel.Interactions[i].InteractionId;
                    page.FindPropertyRelative("title").stringValue=info?"基地詳情":intel.Interactions[i].DisplayName;
                    page.FindPropertyRelative("order").intValue=i;page.FindPropertyRelative("isPublic").boolValue=true;
                    page.FindPropertyRelative("interaction").objectReferenceValue=info?null:intel.Interactions[i];
                    page.FindPropertyRelative("informationPagePrefab").objectReferenceValue=info?AssetDatabase.LoadAssetAtPath<BaseDetailsPageView>(Folder+"BaseDetailsPage.prefab"):null;
                }
                so.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.CreateAsset(config,path);
            }
            Validate(config);Ref(intel,"presentation",config);AssetDatabase.SaveAssetIfDirty(intel);AssetDatabase.SaveAssetIfDirty(config);
        }

        private static void Validate(BuildingPresentationData config)
        {var ids=new HashSet<string>();foreach(BuildingPageDefinition page in config.Pages)if(page==null||string.IsNullOrWhiteSpace(page.PageId)||!ids.Add(page.PageId)||!page.HasSingleSource)throw new InvalidOperationException("Invalid building presentation page ID/source: "+AssetDatabase.GetAssetPath(config));}
        private static void LoadAssets()
        {font=Load<TMP_FontAsset>("Assets/Font/TW-Sung-98_1 SDF.asset");icons=Load<TMP_SpriteAsset>("Assets/Art/GameplayHUD/Icons/Review01/EventInline_food.asset");paper=Load<Sprite>("Assets/Art/GameplayHUD/PaperUI/ui_panel_paper_light_9slice_01.png");dark=Load<Sprite>("Assets/Art/GameplayHUD/PaperUI/ui_panel_paper_dark_9slice_01.png");}
        private static T Load<T>(string path) where T:UnityEngine.Object {T a=AssetDatabase.LoadAssetAtPath<T>(path);if(a==null)throw new InvalidOperationException("Missing approved UI asset: "+path);return a;}
        private static void EditPrefab(string name,Action<GameObject> apply)
        {string path=Folder+name;bool exists=AssetDatabase.LoadAssetAtPath<GameObject>(path)!=null;GameObject go=exists?PrefabUtility.LoadPrefabContents(path):new GameObject(name.Replace(".prefab",""),typeof(RectTransform));try{apply(go);PrefabUtility.SaveAsPrefabAsset(go,path);}finally{if(exists)PrefabUtility.UnloadPrefabContents(go);else UnityEngine.Object.DestroyImmediate(go);}}
        private static T Get<T>(GameObject go) where T:Component {return go.GetComponent<T>()??go.AddComponent<T>();}
        private static RectTransform Child(Transform parent,string name){Transform found=parent.Find(name);if(found!=null)return (RectTransform)found;var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);return (RectTransform)go.transform;}
        private static void Rect(Transform transform,Vector2 min,Vector2 max){RectTransform r=(RectTransform)transform;r.anchorMin=min;r.anchorMax=max;r.offsetMin=Vector2.zero;r.offsetMax=Vector2.zero;r.localScale=Vector3.one;}
        private static void Surface(GameObject go,bool useDark){Image image=Get<Image>(go);image.sprite=useDark?dark:paper;image.type=Image.Type.Sliced;image.preserveAspect=false;image.pixelsPerUnitMultiplier=4;image.color=Color.white;}
        private static TMP_Text Label(Transform parent,string name,string value,float size){TMP_Text t=Get<TextMeshProUGUI>(Child(parent,name).gameObject);t.text=value;Style(t,size);Rect(t.transform,Vector2.zero,Vector2.one);return t;}
        private static void Style(TMP_Text t,float size){t.font=font;t.spriteAsset=icons;t.enableAutoSizing=false;t.fontSize=size;t.color=Ink;t.enableWordWrapping=true;t.overflowMode=TextOverflowModes.Overflow;t.raycastTarget=false;t.alignment=TextAlignmentOptions.TopLeft;}
        private static void AutoText(TMP_Text text){LayoutElement le=Get<LayoutElement>(text.gameObject);le.minHeight=35;le.preferredHeight=-1;le.flexibleWidth=1;}
        private static void Height(GameObject go,float height){LayoutElement le=Get<LayoutElement>(go);le.minHeight=height;le.preferredHeight=height;le.flexibleHeight=0;}
        private static Button Button(Transform parent,string name,string label,float size){GameObject go=Child(parent,name).gameObject;Surface(go,false);Button b=Get<Button>(go);b.targetGraphic=go.GetComponent<Image>();TMP_Text t=Label(go.transform,"Label",label,size);t.alignment=TextAlignmentOptions.Midline;t.margin=new Vector4(10,5,10,5);return b;}
        private static void Vertical(GameObject go,int space){VerticalLayoutGroup l=Get<VerticalLayoutGroup>(go);l.enabled=true;l.spacing=space;l.padding=new RectOffset(8,8,8,8);l.childControlWidth=true;l.childControlHeight=true;l.childForceExpandWidth=true;l.childForceExpandHeight=false;ContentSizeFitter f=Get<ContentSizeFitter>(go);f.verticalFit=ContentSizeFitter.FitMode.PreferredSize;}
        private static void DisableLayout(GameObject go){LayoutGroup l=go.GetComponent<LayoutGroup>();if(l!=null)l.enabled=false;ContentSizeFitter f=go.GetComponent<ContentSizeFitter>();if(f!=null)f.enabled=false;}
        private static ScrollRect Scroll(Transform parent,string name,Vector2 min,Vector2 max){RectTransform root=Child(parent,name);Rect(root,min,max);ScrollRect s=Get<ScrollRect>(root.gameObject);RectTransform viewport=Child(root,"Viewport");Rect(viewport,Vector2.zero,Vector2.one);viewport.offsetMax=new Vector2(-14,0);Get<RectMask2D>(viewport.gameObject);Image hit=Get<Image>(viewport.gameObject);hit.color=new Color(0,0,0,.001f);RectTransform content=Child(viewport,"Content");content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);content.anchoredPosition=Vector2.zero;content.sizeDelta=Vector2.zero;Vertical(content.gameObject,12);s.viewport=viewport;s.content=content;s.horizontal=false;s.vertical=true;s.movementType=ScrollRect.MovementType.Clamped;s.scrollSensitivity=35;
            RectTransform barRoot=Child(root,"Scrollbar");Rect(barRoot,new Vector2(1,0),Vector2.one);barRoot.offsetMin=new Vector2(-11,0);Image track=Get<Image>(barRoot.gameObject);track.color=new Color(.2f,.2f,.18f,.2f);Scrollbar bar=Get<Scrollbar>(barRoot.gameObject);RectTransform handle=Child(barRoot,"Handle");Rect(handle,Vector2.zero,Vector2.one);Image handleImage=Get<Image>(handle.gameObject);handleImage.color=new Color(.45f,.42f,.35f,.85f);bar.handleRect=handle;bar.targetGraphic=handleImage;bar.direction=Scrollbar.Direction.BottomToTop;s.verticalScrollbar=bar;s.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;return s;}
        private static T Read<T>(UnityEngine.Object owner,string field)where T:UnityEngine.Object {SerializedProperty p=new SerializedObject(owner).FindProperty(field);return p==null?null:p.objectReferenceValue as T;}
        private static void Ref(UnityEngine.Object owner,string field,UnityEngine.Object value){SerializedObject so=new SerializedObject(owner);SerializedProperty p=so.FindProperty(field);if(p==null)throw new InvalidOperationException(owner.GetType().Name+" missing field "+field);p.objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(owner);}
        private static void PositionButton(Component view,string field,Transform parent,Vector2 min,Vector2 max){Button b=Read<Button>(view,field);if(b==null){Transform legacy=FindUniqueOwned(view.transform,field=="closeButton"?"Close":field=="constructButton"?"Construct":"CancelConstruction");if(legacy!=null)b=legacy.GetComponent<Button>();if(b==null)b=Button(parent,field,field=="closeButton"?"關閉":field=="constructButton"?"施工／升級":"取消施工",26);Ref(view,field,b);}if(b.transform.parent!=parent)b.transform.SetParent(parent,false);Rect(b.transform,min,max);Surface(b.gameObject,false);foreach(TMP_Text t in b.GetComponentsInChildren<TMP_Text>(true)){Style(t,26);Rect(t.transform,Vector2.zero,Vector2.one);t.alignment=TextAlignmentOptions.Midline;t.margin=new Vector4(12,6,12,6);}}
        private static Transform FindUniqueOwned(Transform root,string name)
        {
            Transform result=null;
            foreach(Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
                if(candidate.name!=name)continue;
                if(result!=null)throw new InvalidOperationException("Ambiguous owned building UI name: "+name);
                result=candidate;
            }
            return result;
        }
        private static void RebindSceneInstance(BuildingDetailView view)
        {
            BuildingDetailView source=PrefabUtility.GetCorrespondingObjectFromSource(view);
            if(source==null)throw new InvalidOperationException("Building detail prefab source is missing.");
            foreach(string field in new[]{"root","titleText","statusText","requirementText","rewardText","constructButton","cancelConstructionButton","closeButton","interactionRoot","cardCraftPrefab","foodPlanPrefab","cardUpgradePrefab","bookmarkRoot","bookmarkButtonPrefab","pageStatusText"})
            {
                UnityEngine.Object value=Read<UnityEngine.Object>(source,field);
                Component component=value as Component;
                GameObject gameObject=value as GameObject;
                Transform transform=component!=null?component.transform:gameObject!=null?gameObject.transform:null;
                if(transform!=null&&(transform==source.transform||transform.IsChildOf(source.transform)))
                {
                    string path=AnimationUtility.CalculateTransformPath(transform,source.transform);
                    Transform mapped=string.IsNullOrEmpty(path)?view.transform:view.transform.Find(path);
                    if(mapped==null)throw new InvalidOperationException("Updated building prefab child not found in scene: "+path);
                    value=component!=null?(UnityEngine.Object)mapped.GetComponent(component.GetType()):mapped.gameObject;
                }
                Ref(view,field,value);
            }
            // This migration explicitly replaces building panel geometry, including
            // old half-screen instance overrides. Other scene panels are untouched.
            foreach(RectTransform original in source.GetComponentsInChildren<RectTransform>(true))
            {
                string path=AnimationUtility.CalculateTransformPath(original,source.transform);
                RectTransform target=(string.IsNullOrEmpty(path)?view.transform:view.transform.Find(path)) as RectTransform;
                if(target==null)continue;
                target.anchorMin=original.anchorMin;target.anchorMax=original.anchorMax;
                target.pivot=original.pivot;target.sizeDelta=original.sizeDelta;
                target.anchoredPosition3D=original.anchoredPosition3D;
                target.localScale=original.localScale;target.localRotation=original.localRotation;
                EditorUtility.SetDirty(target);PrefabUtility.RecordPrefabInstancePropertyModifications(target);
                TMP_Text originalText=original.GetComponent<TMP_Text>();
                TMP_Text targetText=target.GetComponent<TMP_Text>();
                if(originalText!=null&&targetText!=null)
                {
                    Style(targetText,originalText.fontSize);targetText.color=originalText.color;
                    targetText.alignment=originalText.alignment;
                    EditorUtility.SetDirty(targetText);PrefabUtility.RecordPrefabInstancePropertyModifications(targetText);
                }
            }
            PrefabUtility.RecordPrefabInstancePropertyModifications(view);
        }
    }
}
#endif
