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
    public class AjinSyringe : SyringeBehaviour
    {
        public override string GetLiquidID() => AjinSerum.ID;
            public class AjinSerum : Liquid
            {
                public const string ID = "AJIN SERUM";

                public AjinSerum()
                {
                    Color = new UnityEngine.Color(0.06f, 0.06f, 0.06f);
                }

                public override void OnEnterLimb(LimbBehaviour limb)
                {
                    if(limb.Person.TryGetComponent<LimbRegrowthPerson>(out LimbRegrowthPerson AjinPerson)){
                        return;
                    }
                    LimbRegrowthPerson Ajin=limb.Person.gameObject.AddComponent<LimbRegrowthPerson>();
                    Ajin.IBMUser="Nagai";
                }

                public override void OnUpdate(BloodContainer container) {}
                public override void OnEnterContainer(BloodContainer container) {}
                public override void OnExitContainer(BloodContainer container) {}
            }
    }
    public class AjinSyringeWithBlackGhost : SyringeBehaviour
    {
        public override string GetLiquidID() => AjinSerum.ID;
            public class AjinSerum : Liquid
            {
                public const string ID = "AJIN SERUM [WITH BLACK GHOST]";

                public AjinSerum()
                {
                    Color = new UnityEngine.Color(0.06f, 0.06f, 0.06f);
                }

                public override void OnEnterLimb(LimbBehaviour limb)
                {
                    string[] IBMUserList=new string[]{"Nagai","Tanaka","Shimomura","Sato","Masumi","Takeshi","Takahashi"};
                    if(limb.Person.gameObject.TryGetComponent<LimbRegrowthPerson>(out LimbRegrowthPerson AjinPerson)){
                        if(!limb.Person.gameObject.HasComponent<IBMPower>()){
                            AjinPerson.IBMUser=IBMUserList[Mathf.FloorToInt(UnityEngine.Random.Range(0f,IBMUserList.Length+1))];
                            limb.Person.gameObject.AddComponent<IBMPower>();
                        }
                        return;
                    }
                    LimbRegrowthPerson Ajin=limb.Person.gameObject.AddComponent<LimbRegrowthPerson>();
                    Ajin.IBMUser=IBMUserList[Mathf.FloorToInt(UnityEngine.Random.Range(0f,IBMUserList.Length+1))];
                    limb.Person.gameObject.AddComponent<IBMPower>();
                }

                public override void OnUpdate(BloodContainer container) {}
                public override void OnEnterContainer(BloodContainer container) {}
                public override void OnExitContainer(BloodContainer container) {}
            }
    }
}
// Originally uploaded by 'NeitherFishNorCat'. Do not reupload without their explicit permission.
