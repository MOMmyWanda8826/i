
using System;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;


using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Mod
{
    public struct ModAssets
    {
        public static GameObject IBMParticlePrefab;
        public static GameObject DashLinePrefab;
        public static Dictionary<string,Sprite> IBMHeadTexture=new Dictionary<string,Sprite>();
        public static Sprite IBMWingTex;
        public static Texture2D IBMTex;
        public static Texture2D IBMBoneTex;
    }

    public class Mod : MonoBehaviour
    {
        public static string ModTag = "AJIN-";

        public static void Main()
        {
            ModAPI.Metadata.Name="AJIN:Demi-Human";

            if(!ModAssets.IBMParticlePrefab){
                LoadAssets();
            }

            CategoryBuilder.Create("AJIN","It's a creature that does not die", ModAPI.LoadSprite("Thumbnail/Category.png"));

            ModAPI.RegisterLiquid(AjinSyringe.AjinSerum.ID, new AjinSyringe.AjinSerum());
            ModAPI.RegisterLiquid(AjinSyringeWithBlackGhost.AjinSerum.ID, new AjinSyringeWithBlackGhost.AjinSerum());

            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Acid Syringe"), 
                    NameOverride = ModTag+"AJIN Syringe",
                    DescriptionOverride = "A Syringe turns regular human to Ajin!",
                    CategoryOverride = ModAPI.FindCategory("AJIN"),
                    ThumbnailOverride = ModAPI.LoadSprite("AJIN syringe view.png"),
                    AfterSpawn = (Instance) =>
                    {
                        UnityEngine.Object.Destroy(Instance.GetComponent<SyringeBehaviour>());
                        Instance.GetOrAddComponent<AjinSyringe>();
                    }
                }
            );

            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Acid Syringe"), 
                    NameOverride = ModTag+"AJIN Syringe[With black ghost]",
                    DescriptionOverride = "A Syringe turns regular human to Ajin with a random black ghost!",
                    CategoryOverride = ModAPI.FindCategory("AJIN"),
                    ThumbnailOverride = ModAPI.LoadSprite("AJIN syringe blackghost view.png"),
                    AfterSpawn = (Instance) =>
                    {
                        UnityEngine.Object.Destroy(Instance.GetComponent<SyringeBehaviour>());
                        Instance.GetOrAddComponent<AjinSyringeWithBlackGhost>();
                    }
                }
            );

            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Human"), 
                    NameOverride = ModTag+"Nagai", 
                    DescriptionOverride = "“This is a carefully considered way of maximizing what is our current 0% chance of winning! It is a perfectly logical thing to do!!”",
                    CategoryOverride = ModAPI.FindCategory("AJIN"), 
                    ThumbnailOverride = ModAPI.LoadSprite("Thumbnail/Nagai.png"), 
                    AfterSpawn = (Instance) => 
                    {
                        PersonBehaviour Person=Instance.GetComponent<PersonBehaviour>();
                        if(!Instance.gameObject.HasComponent<LimbRegrowthPerson>()){
                            LimbRegrowthPerson Ajin=Instance.gameObject.AddComponent<LimbRegrowthPerson>();
                            Ajin.IBMUser="Nagai";
                        }
                        if(!Instance.HasComponent<IBMPower>()){
                            Instance.AddComponent<IBMPower>();
                        }
                        foreach(LimbBehaviour limb in Person.Limbs){
                            if(limb.name=="Head"){
                                GameObject Hair=new GameObject("Hair");
                                Hair.transform.SetParent(limb.gameObject.transform);
                                Hair.transform.localPosition=new Vector3(0f, 0f);
                                Hair.transform.rotation=limb.gameObject.transform.rotation;
                                Hair.transform.localScale=new Vector3(1.05f, 1.05f);
                                SpriteRenderer spriteRenderer = Hair.AddComponent<SpriteRenderer>();
                                spriteRenderer.sprite = ModAPI.LoadSprite("Characters/Nagai_Hair.png");
                                spriteRenderer.sortingOrder+=5;
                                spriteRenderer.sortingLayerName = "Foreground";
                            }
                            if(limb.name=="LowerArmFront"||limb.name=="UpperArmFront"){
                                SpriteRenderer spriterenderer = limb.gameObject.GetComponent<SpriteRenderer>();
                                spriterenderer.sortingOrder+=15;
                            }
                        }

                        Texture2D BodyTex=ModAPI.LoadTexture("Characters/Nagai.png");
                        Person.SetBodyTextures(BodyTex,null,null,1f);
                    }
                }
            );
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Human"), 
                    NameOverride = ModTag+"Ko", 
                    DescriptionOverride = "“Doesn't that make you angry?”",
                    CategoryOverride = ModAPI.FindCategory("AJIN"), 
                    ThumbnailOverride = ModAPI.LoadSprite("Thumbnail/Nakano.png"), 
                    AfterSpawn = (Instance) => 
                    {
                        PersonBehaviour Person=Instance.GetComponent<PersonBehaviour>();
                        if(!Instance.gameObject.HasComponent<LimbRegrowthPerson>()){
                            LimbRegrowthPerson Ajin=Instance.gameObject.AddComponent<LimbRegrowthPerson>();
                        }
                        foreach(LimbBehaviour limb in Person.Limbs){
                            if(limb.name=="Head"){
                                GameObject Hair=new GameObject("Hair");
                                Hair.transform.SetParent(limb.gameObject.transform);
                                Hair.transform.localPosition=new Vector3(0f, 0f);
                                Hair.transform.rotation=limb.gameObject.transform.rotation;
                                Hair.transform.localScale=new Vector3(1.05f, 1.05f);
                                SpriteRenderer spriteRenderer = Hair.AddComponent<SpriteRenderer>();
                                spriteRenderer.sprite = ModAPI.LoadSprite("Characters/Nakano_Hair.png");
                                spriteRenderer.sortingOrder+=5;
                                spriteRenderer.sortingLayerName = "Foreground";
                            }
                            if(limb.name=="LowerArmFront"||limb.name=="UpperArmFront"){
                                SpriteRenderer spriterenderer = limb.gameObject.GetComponent<SpriteRenderer>();
                                spriterenderer.sortingOrder+=15;
                            }
                        }

                        Texture2D BodyTex=ModAPI.LoadTexture("Characters/Nakano.png");
                        Person.SetBodyTextures(BodyTex,null,null,1f);
                    }
                }
            );
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Human"), 
                    NameOverride = ModTag+"Izumi", 
                    DescriptionOverride = "“Thank you.”",
                    CategoryOverride = ModAPI.FindCategory("AJIN"), 
                    ThumbnailOverride = ModAPI.LoadSprite("Thumbnail/Shimomura.png"), 
                    AfterSpawn = (Instance) => 
                    {
                        PersonBehaviour Person=Instance.GetComponent<PersonBehaviour>();
                        if(!Instance.gameObject.HasComponent<LimbRegrowthPerson>()){
                            LimbRegrowthPerson Ajin=Instance.gameObject.AddComponent<LimbRegrowthPerson>();
                            Ajin.IBMUser="Shimomura";
                        }
                        if(!Instance.HasComponent<IBMPower>()){
                            Instance.AddComponent<IBMPower>();
                        }
                        foreach(LimbBehaviour limb in Person.Limbs){
                            if(limb.name=="Head"){
                                GameObject Hair=new GameObject("Hair");
                                Hair.transform.SetParent(limb.gameObject.transform);
                                Hair.transform.localPosition=new Vector3(0f, 0f);
                                Hair.transform.rotation=limb.gameObject.transform.rotation;
                                Hair.transform.localScale=new Vector3(1.05f, 1.05f);
                                SpriteRenderer spriteRenderer = Hair.AddComponent<SpriteRenderer>();
                                spriteRenderer.sprite = ModAPI.LoadSprite("Characters/Shimomura_Hair.png");
                                spriteRenderer.sortingOrder+=5;
                                spriteRenderer.sortingLayerName = "Foreground";
                            }
                            if(limb.name=="LowerArmFront"||limb.name=="UpperArmFront"){
                                SpriteRenderer spriterenderer = limb.gameObject.GetComponent<SpriteRenderer>();
                                spriterenderer.sortingOrder+=15;
                            }
                        }

                        Texture2D BodyTex=ModAPI.LoadTexture("Characters/Shimomura.png");
                        Person.SetBodyTextures(BodyTex,null,null,1f);
                    }
                }
            );
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Human"), 
                    NameOverride = ModTag+"Takeshi", 
                    DescriptionOverride = "“I'll let you get past these walls, only once”",
                    CategoryOverride = ModAPI.FindCategory("AJIN"), 
                    ThumbnailOverride = ModAPI.LoadSprite("Thumbnail/Takeshi.png"), 
                    AfterSpawn = (Instance) => 
                    {
                        PersonBehaviour Person=Instance.GetComponent<PersonBehaviour>();
                        if(!Instance.gameObject.HasComponent<LimbRegrowthPerson>()){
                            LimbRegrowthPerson Ajin=Instance.gameObject.AddComponent<LimbRegrowthPerson>();
                            Ajin.IBMUser="Takeshi";
                        }
                        if(!Instance.HasComponent<IBMPower>()){
                            Instance.AddComponent<IBMPower>();
                        }
                        foreach(LimbBehaviour limb in Person.Limbs){
                            if(limb.name=="Head"){
                                GameObject Hair=new GameObject("Hair");
                                Hair.transform.SetParent(limb.gameObject.transform);
                                Hair.transform.localPosition=new Vector3(0f, 0f);
                                Hair.transform.rotation=limb.gameObject.transform.rotation;
                                Hair.transform.localScale=new Vector3(1.05f, 1.05f);
                                SpriteRenderer spriteRenderer = Hair.AddComponent<SpriteRenderer>();
                                spriteRenderer.sprite = ModAPI.LoadSprite("Characters/Takeshi_Hair.png");
                                spriteRenderer.sortingOrder+=5;
                                spriteRenderer.sortingLayerName = "Foreground";
                            }
                            if(limb.name=="LowerArmFront"||limb.name=="UpperArmFront"){
                                SpriteRenderer spriterenderer = limb.gameObject.GetComponent<SpriteRenderer>();
                                spriterenderer.sortingOrder+=15;
                            }
                        }

                        Texture2D BodyTex=ModAPI.LoadTexture("Characters/Takeshi.png");
                        Person.SetBodyTextures(BodyTex,null,null,1f);
                    }
                }
            );
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Human"), 
                    NameOverride = ModTag+"Sato", 
                    DescriptionOverride = "“Looks like someone puts in another coin!”",
                    CategoryOverride = ModAPI.FindCategory("AJIN"), 
                    ThumbnailOverride = ModAPI.LoadSprite("Thumbnail/Sato.png"), 
                    AfterSpawn = (Instance) => 
                    {
                        PersonBehaviour Person=Instance.GetComponent<PersonBehaviour>();
                        if(!Instance.gameObject.HasComponent<LimbRegrowthPerson>()){
                            LimbRegrowthPerson Ajin=Instance.gameObject.AddComponent<LimbRegrowthPerson>();
                            Ajin.IBMUser="Sato";
                        }
                        if(!Instance.HasComponent<IBMPower>()){
                            Instance.AddComponent<IBMPower>();
                        }
                        foreach(LimbBehaviour limb in Person.Limbs){
                            if(limb.name=="Head"){
                                GameObject Hair=new GameObject("Hair");
                                Hair.transform.SetParent(limb.gameObject.transform);
                                Hair.transform.localPosition=new Vector3(0f, 0f);
                                Hair.transform.rotation=limb.gameObject.transform.rotation;
                                Hair.transform.localScale=new Vector3(1.05f, 1.05f);
                                SpriteRenderer spriteRenderer = Hair.AddComponent<SpriteRenderer>();
                                spriteRenderer.sprite = ModAPI.LoadSprite("Characters/Sato_Hair.png");
                                spriteRenderer.sortingOrder+=5;
                                spriteRenderer.sortingLayerName = "Foreground";

                                GameObject Hat=new GameObject("Hat");
                                Hat.transform.SetParent(limb.gameObject.transform);
                                Hat.transform.localPosition=new Vector3(0f, 0f);
                                Hat.transform.rotation=limb.gameObject.transform.rotation;
                                Hat.transform.localScale=new Vector3(1.1f, 1.1f);
                                SpriteRenderer HatspriteRenderer = Hat.AddComponent<SpriteRenderer>();
                                HatspriteRenderer.sprite = ModAPI.LoadSprite("Characters/Sato_Hat.png");
                                HatspriteRenderer.sortingOrder+=10;
                                HatspriteRenderer.sortingLayerName = "Foreground";
                            }
                            if(limb.name=="LowerArmFront"||limb.name=="UpperArmFront"){
                                SpriteRenderer spriterenderer = limb.gameObject.GetComponent<SpriteRenderer>();
                                spriterenderer.sortingOrder+=15;
                            }
                        }

                        Texture2D BodyTex=ModAPI.LoadTexture("Characters/Sato.png");
                        Person.SetBodyTextures(BodyTex,null,null,1f);
                    }
                }
            );
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Human"), 
                    NameOverride = ModTag+"Takahashi", 
                    DescriptionOverride = "“I don't care!”",
                    CategoryOverride = ModAPI.FindCategory("AJIN"), 
                    ThumbnailOverride = ModAPI.LoadSprite("Thumbnail/Takahashi.png"), 
                    AfterSpawn = (Instance) => 
                    {
                        PersonBehaviour Person=Instance.GetComponent<PersonBehaviour>();
                        if(!Instance.gameObject.HasComponent<LimbRegrowthPerson>()){
                            LimbRegrowthPerson Ajin=Instance.gameObject.AddComponent<LimbRegrowthPerson>();
                            Ajin.IBMUser="Takahashi";
                        }
                        if(!Instance.HasComponent<IBMPower>()){
                            Instance.AddComponent<IBMPower>();
                        }
                        foreach(LimbBehaviour limb in Person.Limbs){
                            if(limb.name=="Head"){
                                GameObject Hair=new GameObject("Hair");
                                Hair.transform.SetParent(limb.gameObject.transform);
                                Hair.transform.localPosition=new Vector3(0f, 0f);
                                Hair.transform.rotation=limb.gameObject.transform.rotation;
                                Hair.transform.localScale=new Vector3(1.05f, 1.05f);
                                SpriteRenderer spriteRenderer = Hair.AddComponent<SpriteRenderer>();
                                spriteRenderer.sprite = ModAPI.LoadSprite("Characters/Takahashi_Hair.png");
                                spriteRenderer.sortingOrder+=5;
                                spriteRenderer.sortingLayerName = "Foreground";
                            }
                            if(limb.name=="LowerArmFront"||limb.name=="UpperArmFront"){
                                SpriteRenderer spriterenderer = limb.gameObject.GetComponent<SpriteRenderer>();
                                spriterenderer.sortingOrder+=15;
                            }
                        }

                        Texture2D BodyTex=ModAPI.LoadTexture("Characters/Takahashi.png");
                        Person.SetBodyTextures(BodyTex,null,null,1f);
                    }
                }
            );
             ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Human"), 
                    NameOverride = ModTag+"Tanaka", 
                    DescriptionOverride = "“I don't think it's right...to kill her!”",
                    CategoryOverride = ModAPI.FindCategory("AJIN"), 
                    ThumbnailOverride = ModAPI.LoadSprite("Thumbnail/Tanaka.png"), 
                    AfterSpawn = (Instance) => 
                    {
                        PersonBehaviour Person=Instance.GetComponent<PersonBehaviour>();
                        if(!Instance.gameObject.HasComponent<LimbRegrowthPerson>()){
                            LimbRegrowthPerson Ajin=Instance.gameObject.AddComponent<LimbRegrowthPerson>();
                            Ajin.IBMUser="Tanaka";
                        }
                        if(!Instance.HasComponent<IBMPower>()){
                            Instance.AddComponent<IBMPower>();
                        }
                        foreach(LimbBehaviour limb in Person.Limbs){
                            if(limb.name=="Head"){
                                GameObject Hair=new GameObject("Hair");
                                Hair.transform.SetParent(limb.gameObject.transform);
                                Hair.transform.localPosition=new Vector3(0f, 0f);
                                Hair.transform.rotation=limb.gameObject.transform.rotation;
                                Hair.transform.localScale=new Vector3(1.05f, 1.05f);
                                SpriteRenderer spriteRenderer = Hair.AddComponent<SpriteRenderer>();
                                spriteRenderer.sprite = ModAPI.LoadSprite("Characters/Tanaka_Hair.png");
                                spriteRenderer.sortingOrder+=5;
                                spriteRenderer.sortingLayerName = "Foreground";
                            }
                            if(limb.name=="LowerArmFront"||limb.name=="UpperArmFront"){
                                SpriteRenderer spriterenderer = limb.gameObject.GetComponent<SpriteRenderer>();
                                spriterenderer.sortingOrder+=15;
                            }
                        }

                        Texture2D BodyTex=ModAPI.LoadTexture("Characters/Tanaka.png");
                        Person.SetBodyTextures(BodyTex,null,null,1f);
                    }
                }
            );
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Human"), 
                    NameOverride = ModTag+"Gen", 
                    DescriptionOverride = "A human",
                    CategoryOverride = ModAPI.FindCategory("AJIN"), 
                    ThumbnailOverride = ModAPI.LoadSprite("Thumbnail/Gen.png"), 
                    AfterSpawn = (Instance) => 
                    {
                        PersonBehaviour Person=Instance.GetComponent<PersonBehaviour>();
                        foreach(LimbBehaviour limb in Person.Limbs){
                            if(limb.name=="Head"){
                                GameObject Hair=new GameObject("Hair");
                                Hair.transform.SetParent(limb.gameObject.transform);
                                Hair.transform.localPosition=new Vector3(0f, 0f);
                                Hair.transform.rotation=limb.gameObject.transform.rotation;
                                Hair.transform.localScale=new Vector3(1.05f, 1.05f);
                                SpriteRenderer spriteRenderer = Hair.AddComponent<SpriteRenderer>();
                                spriteRenderer.sprite = ModAPI.LoadSprite("Characters/Gen_Hair.png");
                                spriteRenderer.sortingOrder+=5;
                                spriteRenderer.sortingLayerName = "Foreground";
                            }
                            if(limb.name=="LowerArmFront"||limb.name=="UpperArmFront"){
                                SpriteRenderer spriterenderer = limb.gameObject.GetComponent<SpriteRenderer>();
                                spriterenderer.sortingOrder+=15;
                            }
                        }

                        Texture2D BodyTex=ModAPI.LoadTexture("Characters/Gen.png");
                        Person.SetBodyTextures(BodyTex,null,null,1f);
                    }
                }
            );
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Human"), 
                    NameOverride = ModTag+"Masumi", 
                    DescriptionOverride = "“I'm alone again.”",
                    CategoryOverride = ModAPI.FindCategory("AJIN"), 
                    ThumbnailOverride = ModAPI.LoadSprite("Thumbnail/Masumi.png"), 
                    AfterSpawn = (Instance) => 
                    {
                        PersonBehaviour Person=Instance.GetComponent<PersonBehaviour>();
                        if(!Instance.gameObject.HasComponent<LimbRegrowthPerson>()){
                            LimbRegrowthPerson Ajin=Instance.gameObject.AddComponent<LimbRegrowthPerson>();
                            Ajin.IBMUser="Masumi";
                        }
                        if(!Instance.HasComponent<IBMPower>()){
                            Instance.AddComponent<IBMPower>();
                        }
                        foreach(LimbBehaviour limb in Person.Limbs){
                            if(limb.name=="Head"){
                                GameObject Hair=new GameObject("Hair");
                                Hair.transform.SetParent(limb.gameObject.transform);
                                Hair.transform.localPosition=new Vector3(0f, 0f);
                                Hair.transform.rotation=limb.gameObject.transform.rotation;
                                Hair.transform.localScale=new Vector3(1.05f, 1.05f);
                                SpriteRenderer spriteRenderer = Hair.AddComponent<SpriteRenderer>();
                                spriteRenderer.sprite = ModAPI.LoadSprite("Characters/Masumi_Hair.png");
                                spriteRenderer.sortingOrder+=5;
                                spriteRenderer.sortingLayerName = "Foreground";
                            }
                            if(limb.name=="LowerArmFront"||limb.name=="UpperArmFront"){
                                SpriteRenderer spriterenderer = limb.gameObject.GetComponent<SpriteRenderer>();
                                spriterenderer.sortingOrder+=15;
                            }
                        }

                        Texture2D BodyTex=ModAPI.LoadTexture("Characters/Masumi.png");
                        Person.SetBodyTextures(BodyTex,null,null,1f);
                    }
                }
            );
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Human"), 
                    NameOverride = ModTag+"SAT", 
                    DescriptionOverride = "Human",
                    CategoryOverride = ModAPI.FindCategory("AJIN"), 
                    ThumbnailOverride = ModAPI.LoadSprite("Thumbnail/SAT.png"), 
                    AfterSpawn = (Instance) => 
                    {
                        PersonBehaviour Person=Instance.GetComponent<PersonBehaviour>();
                        Texture2D BodyTex=ModAPI.LoadTexture("Characters/SAT.png");
                        Person.SetBodyTextures(BodyTex,null,null,1f);
                    }
                }
            );
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Human"), 
                    NameOverride = ModTag+"Tosaki", 
                    DescriptionOverride = "“Fight!...for your own sake!”",
                    CategoryOverride = ModAPI.FindCategory("AJIN"), 
                    ThumbnailOverride = ModAPI.LoadSprite("Thumbnail/Tosaki.png"), 
                    AfterSpawn = (Instance) => 
                    {
                        PersonBehaviour Person=Instance.GetComponent<PersonBehaviour>();
                        foreach(LimbBehaviour limb in Person.Limbs){
                            if(limb.name=="Head"){
                                GameObject Hair=new GameObject("Hair");
                                Hair.transform.SetParent(limb.gameObject.transform);
                                Hair.transform.localPosition=new Vector3(0f, 0f);
                                Hair.transform.rotation=limb.gameObject.transform.rotation;
                                Hair.transform.localScale=new Vector3(1.05f, 1.05f);
                                SpriteRenderer spriteRenderer = Hair.AddComponent<SpriteRenderer>();
                                spriteRenderer.sprite = ModAPI.LoadSprite("Characters/Tosaki_Hair.png");
                                spriteRenderer.sortingOrder+=5;
                                spriteRenderer.sortingLayerName = "Foreground";
                            }
                            if(limb.name=="LowerArmFront"||limb.name=="UpperArmFront"){
                                SpriteRenderer spriterenderer = limb.gameObject.GetComponent<SpriteRenderer>();
                                spriterenderer.sortingOrder+=15;
                            }
                        }
                        Texture2D BodyTex=ModAPI.LoadTexture("Characters/Tosaki.png");
                        Person.SetBodyTextures(BodyTex,null,null,1f);
                    }
                }
            );
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem = ModAPI.FindSpawnable("Human"), 
                    NameOverride = ModTag+"Ikuya Ogura", 
                    DescriptionOverride = "“Are you an idot?”",
                    CategoryOverride = ModAPI.FindCategory("AJIN"), 
                    ThumbnailOverride = ModAPI.LoadSprite("Thumbnail/Ikuya Ogura.png"), 
                    AfterSpawn = (Instance) => 
                    {
                        PersonBehaviour Person=Instance.GetComponent<PersonBehaviour>();
                        Texture2D BodyTex=ModAPI.LoadTexture("Characters/Ikuya Ogura.png");
                        Person.SetBodyTextures(BodyTex,null,null,1f);
                    }
                }
            );
        }

        public static void LoadAssets(){
            Assembly assembly=Assembly.LoadFrom(Path.Combine(ModAPI.Metadata.MetaLocation, "UnityEngine.AssetBundleModule.dll"));
			Type assetbundle=assembly.GetType("UnityEngine.AssetBundle");
			MethodInfo load=assetbundle.GetMethod("LoadFromFile",new [] {typeof(string)});
			MethodInfo loadAsset=assetbundle.GetMethod("LoadAsset",new [] {typeof(string), typeof(Type)});
			MethodInfo unload=assetbundle.GetMethod("Unload");

			object[] objectArray  = new object [] {false};

			object ab = load.Invoke(null,new[] {Path.Combine(ModAPI.Metadata.MetaLocation, "ibm.ab")});
			GameObject IBMParticlePrefab = (GameObject)loadAsset.Invoke(ab,new object[] {"IBM.ab",typeof(GameObject)});
			unload.Invoke(ab,objectArray);

            object lineab = load.Invoke(null,new[] {Path.Combine(ModAPI.Metadata.MetaLocation, "line.ab")});
			GameObject DashLine = (GameObject)loadAsset.Invoke(lineab,new object[] {"Line.ab",typeof(GameObject)});
			unload.Invoke(lineab,objectArray);

            ModAssets.IBMParticlePrefab=IBMParticlePrefab;
            ModAssets.DashLinePrefab=DashLine;

            ModAssets.IBMHeadTexture.Add("Nagai",ModAPI.LoadSprite("IBM Heads/Nagai_IBM.png"));
            ModAssets.IBMHeadTexture.Add("Tanaka",ModAPI.LoadSprite("IBM Heads/Tanaka_IBM.png"));
            ModAssets.IBMHeadTexture.Add("Shimomura",ModAPI.LoadSprite("IBM Heads/Shimomura_IBM.png"));
            ModAssets.IBMHeadTexture.Add("Sato",ModAPI.LoadSprite("IBM Heads/Sato_IBM.png"));
            ModAssets.IBMHeadTexture.Add("Masumi",ModAPI.LoadSprite("IBM Heads/Masumi_IBM.png"));
            ModAssets.IBMHeadTexture.Add("Takeshi",ModAPI.LoadSprite("IBM Heads/Takeshi_IBM.png"));
            ModAssets.IBMHeadTexture.Add("Takahashi",ModAPI.LoadSprite("IBM Heads/Takahashi_IBM.png"));
            ModAssets.IBMWingTex=ModAPI.LoadSprite("IBM Heads/Takeshi_IBM_Wing.png");

            ModAssets.IBMTex=ModAPI.LoadTexture("IBM.png");
			ModAssets.IBMBoneTex=ModAPI.LoadTexture("IBM_Bone.png");
        }
    }
}

// Originally uploaded by 'NeitherFishNorCat'. Do not reupload without their explicit permission.
