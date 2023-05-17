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
    public class LimbRegrowth : MonoBehaviour
    {
        public bool Revival = false;
        public bool FinishRevival = false;
        public bool Ready = false;

        public LimbBehaviour limb;

        public List<LimbBehaviour> ConnectLimbs = new List<LimbBehaviour>();
        public List<LimbBehaviour> InitialConnectLimbs = new List<LimbBehaviour>();
        public Vector2 LocalAnchorPosition;
        public Vector2 AnchorPosition;
        public GameObject AnchorTarget;
        public bool ConnectToMiddleBody = true;

        public GameObject IBMParticle;
        public bool Recorded = false;

        public JointAngleLimits2D limit;
        public float ReferAngle;
        public bool useLimits;

        public LimbRegrowthPerson RegrowPerson;
        public float BreakingThreshold;
        public bool IsNewLimb = false;

        public bool OnceNewLimb = false;

        public ParticleSystem.EmissionModule IbmEmission;
        public Vector3 scale = new Vector3(1f, 1f, 1f);

        public float RottenProgress = 0f;
        public float AcidProgress = 0f;
        public float BurnProgress = 0f;
        public Vector2 LimbLimit;

        public bool IsHold = false;
        public PhysicalBehaviour Holding;

        public void Awake()
        {
            limb = GetComponent<LimbBehaviour>();
            this.GetComponent<PhysicalBehaviour>()
                .ContextMenuOptions.Buttons.Add(
                    new ContextMenuButton(
                        "Set current form to revive",
                        "Set current form to revive",
                        "Set current form to revive",
                        () =>
                        {
                            foreach (
                                LimbRegrowth AjinLimb in limb.Person.gameObject.GetComponentsInChildren<LimbRegrowth>()
                            )
                            {
                                RottenProgress = limb.SkinMaterialHandler.RottenProgress;
                                AcidProgress = limb.SkinMaterialHandler.AcidProgress;
                                BurnProgress = limb.PhysicalBehaviour.BurnProgress;
                                AjinLimb.RecordCurrentJointData();
                            }
                            ModAPI.Notify("Set current form to revive!");
                        }
                    )
                );
            this.GetComponent<PhysicalBehaviour>()
            .ContextMenuOptions.Buttons.Add(
                new ContextMenuButton(
                    "Set default form to revive",
                    "Set default form to revive",
                    "Set default form to revive",
                    () =>
                    {
                        foreach (
                            LimbRegrowth AjinLimb in limb.Person.gameObject.GetComponentsInChildren<LimbRegrowth>()
                        )
                        {
                            try{
                                AjinLimb.scale = Vector3.one;
                                AjinLimb.RottenProgress = 0f;
                                AjinLimb.AcidProgress = 0f;
                                AjinLimb.BurnProgress = 0f;
                                if(limb.name != "MiddleBody"){
                                    AjinLimb.GetJointDataFromPrefab();
                                }
                            }
                            catch{}
                        }
                        ModAPI.Notify("Set default form to revive!");
                    }
                )
            );
            
            if(base.gameObject.TryGetComponent<GripBehaviour>(out GripBehaviour grip) && grip.isHolding){
                ChangeHoldAction();
            }

            StartCoroutine("LateAwake");
        }

        public void PrintInformation(){
            string outinfo = "case "+limb.name+":\n"+"BreakingThreshold = "+limb.BreakingThreshold+";\nbreak;";
            Debug.Log(outinfo);
        }

        IEnumerator LateAwake()
        {
            yield return 0;

            StopEmission();
            ConnectLimbs.Clear();

            limb.PhysicalBehaviour.SpawnSpawnParticles = false;
            SetupIbmParticle();

            GetComponent<Rigidbody2D>().collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            if (limb.name != "MiddleBody" && !AnchorTarget)
            {
                if(limb.Joint){
                    RecordCurrentJointData();
                }
                else{
                    GetJointDataFromPrefab();
                }
            }

            if(limb.name == "MiddleBody"){
                RecordCurrentJointData();
            }

            if (IsNewLimb)
            {
                if(limb.name!="MiddleBody"){
                    if(!limb.Joint){
                        AttachLimb();
                    }
                }
            }

            yield return 1;

            RegrowPerson=base.transform.root.gameObject.GetOrAddComponent<LimbRegrowthPerson>();
            if(base.transform.root.gameObject.TryGetComponent<IBMPower>(out IBMPower blackghost)){
                RegrowPerson.IBMUser = blackghost.IBMUser;
            }
            
            Ready = true;
        }

        public void SetupIbmParticle()
        {
            IBMParticle = Instantiate(
                ModAssets.IBMParticlePrefab,
                base.transform.position,
                Quaternion.identity,
                base.transform
            );

            ParticleSystem.ShapeModule shape = IBMParticle.GetComponent<ParticleSystem>().shape;
            shape.spriteRenderer = GetComponent<SpriteRenderer>();
            Bounds bound = GetComponent<Collider2D>().bounds;
            shape.scale = bound.size;
            shape.rotation = new Vector3(90f, 0, 0);
            IbmEmission = IBMParticle.GetComponent<ParticleSystem>().emission;
            IBMParticle.AddComponent<Optout>();
        }

        public GameObject GetReferenceLimb(LimbBehaviour ThisLimb)
        {
            GameObject ReferPerson = ModAPI.FindSpawnable("Human").Prefab;
            LimbBehaviour[] Limbs = ReferPerson.GetComponentsInChildren<LimbBehaviour>();
            foreach (LimbBehaviour EachLimb in Limbs)
            {
                if (EachLimb.name == ThisLimb.name)
                {
                    return EachLimb.gameObject;
                }
            }
            return ThisLimb.gameObject;
        }

        public void GetJointDataFromPrefab(){
            HingeJoint2D referJoint = GetReferenceLimb(limb).GetComponent<HingeJoint2D>();
            LimbBehaviour referLimb = GetReferenceLimb(limb).GetComponent<LimbBehaviour>();
            limit = referJoint.limits;
            LimbLimit = limb.OriginalJointLimits;
            AnchorPosition = referJoint.connectedAnchor;
            LocalAnchorPosition = referJoint.anchor;
            string anchorname = "Null";
            useLimits = referJoint.useLimits;
            switch (limb.name)
            {
                case "LowerBody":
                    anchorname = "MiddleBody";
                    BreakingThreshold = 4;
                    break;
                case "UpperBody":
                    anchorname = "MiddleBody";
                    BreakingThreshold = 4;
                    break;
                case "UpperArm":
                    anchorname = "UpperBody";
                    BreakingThreshold = 2;
                    break;
                case "UpperArmFront":
                    anchorname = "UpperBody";
                    BreakingThreshold = 2;
                    break;
                case "LowerArm":
                    anchorname = "UpperArm";
                    BreakingThreshold = 4;
                    break;
                case "LowerArmFront":
                    anchorname = "UpperArmFront";
                    BreakingThreshold = 4;
                    break;
                case "UpperLeg":
                    anchorname = "LowerBody";
                    BreakingThreshold = 4;
                    break;
                case "UpperLegFront":
                    anchorname = "LowerBody";
                    BreakingThreshold = 4;
                    break;
                case "LowerLeg":
                    anchorname = "UpperLeg";
                    BreakingThreshold = 7;
                    break;
                case "LowerLegFront":
                    anchorname = "UpperLegFront";
                    BreakingThreshold = 7;
                    break;
                case "Foot":
                    anchorname = "LowerLeg";
                    BreakingThreshold = 8;
                    break;
                case "FootFront":
                    anchorname = "LowerLegFront";
                    BreakingThreshold = 8;
                    break;
                case "Head":
                    anchorname = "UpperBody";
                    BreakingThreshold = 5;
                    break;
            }
            AnchorTarget = MatchLimb(anchorname).gameObject;
        }

        public void RecordCurrentJointData()
        {
            scale = this.gameObject.transform.localScale;

            if (limb.Joint){
                BreakingThreshold = limb.BreakingThreshold;
                AnchorPosition = limb.Joint.connectedAnchor;
                LocalAnchorPosition = limb.Joint.anchor;
                limit = limb.Joint.limits;
                LimbLimit=limb.OriginalJointLimits;
                AnchorTarget = limb.Joint.connectedBody.gameObject;
                useLimits = limb.Joint.useLimits;
            }
        }

        public void StopEmission()
        {
            ParticleSystem[] particlesystems = GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem particlesystem in particlesystems)
            {
                ParticleSystem.EmissionModule emissionModule = particlesystem.emission;
                emissionModule.rateOverTimeMultiplier = 0f;
            }
            
            for (int i = 0; i < limb.gameObject.transform.childCount; i++)
            {
                if (limb.gameObject.transform.GetChild(i).name.Contains("Outline"))
                {
                    limb.gameObject.transform.GetChild(i).gameObject.SetActive(false);
                }
            }

            if(base.TryGetComponent<FreezeBehaviour>(out FreezeBehaviour freeze)){
                Destroy(freeze);
            }

            if(base.TryGetComponent<GripBehaviour>(out GripBehaviour grip)){
                IsHold = grip.isHolding;
            }
        }

        public LimbBehaviour MatchLimb(string name)
        {
            foreach (LimbBehaviour EachLimb in limb.Person.Limbs)
            {
                if (EachLimb.name == name)
                {
                    return EachLimb;
                }
            }
            return limb;
        }

        public void Update()
        {
            if(!Ready){
                return;
            }
            try
            {   
                if (Revival)
                {
                    Revive();
                    IbmEmission.rateOverTimeMultiplier = Mathf.MoveTowards(
                        IbmEmission.rateOverTimeMultiplier,
                        50,
                        Time.deltaTime * 100
                    );
                    DisintegrateStabObject();
                }
                else
                {
                    IbmEmission.rateOverTimeMultiplier = Mathf.MoveTowards(
                        IbmEmission.rateOverTimeMultiplier,
                        0,
                        Time.deltaTime * 180
                    );
                }
                if (!limb.Joint && limb.name != "MiddleBody")
                {
                    LimbBehaviour AnchorLimb = AnchorTarget.GetComponent<LimbBehaviour>();
                    LimbRegrowth AnchorAjinLimb = AnchorTarget.GetComponent<LimbRegrowth>();
                    if (
                        RegrowPerson.Revival
                        && RegrowPerson.RegenSection.Contains(limb)
                        && !RegrowPerson.RegenSection.Contains(AnchorLimb)
                        && AnchorAjinLimb.IsNewLimb
                    )
                    {
                        bool AllHide = true;
                        foreach (var seg in RegrowPerson.BodySegment)
                        {
                            List<LimbBehaviour> Segment = (List<LimbBehaviour>)seg;
                            if (Segment.Contains(AnchorLimb))
                            {
                                foreach (LimbBehaviour l in Segment)
                                {
                                    if (!l.GetComponent<LimbRegrowth>().IsNewLimb)
                                    {
                                        AllHide = false;
                                        break;
                                    }
                                }
                            }
                        }
                        if (AllHide)
                        {
                            InverseDirectionAttachLimb(Vector2.zero, false);
                        }
                    }
                    if (
                        RegrowPerson.Revival
                        && (
                            RegrowPerson.RegenSection.Contains(AnchorLimb)
                            || (
                                RegrowPerson.RegenSection.Contains(limb)
                                && AnchorTarget.gameObject.HasComponent<LimbRegrowth>()
                            )
                        )
                    )
                    {
                        if ((!AnchorAjinLimb.IsNewLimb && !IsNewLimb))
                        {
                            Vector2 Displacement =
                                limb.gameObject.transform.TransformPoint(LocalAnchorPosition)
                                - AnchorTarget.transform.TransformPoint(AnchorPosition);
                            LimbAttract(Displacement.normalized);
                            if (Displacement.sqrMagnitude < 0.05f)
                            {
                                AttachLimb();
                            }
                        }
                    }
                }
                CalcLimbConnection();
                CountConnectLimbs();
            }
            catch { }
            if(!limb.Joint){
                limb.HasJoint=false;
            }
            else{
                limb.HasJoint=true;
            }
            if(base.TryGetComponent<GripBehaviour>(out GripBehaviour grip) && grip.isHolding != IsHold){
                IsHold = grip.isHolding;
                ChangeHoldAction(grip);
            }
        }

        public void ChangeHoldAction(bool HoldingState = true){
            GripBehaviour grip = base.GetComponent<GripBehaviour>();
            if(grip.isHolding){
                ((FixedJoint2D)ModUtils.GetPrivate<GripBehaviour>(grip,"joint")).autoConfigureConnectedAnchor = false;
                Holding = grip.CurrentlyHolding;
            }

            if(base.transform.root.gameObject.TryGetComponent<IBMPower>(out IBMPower ibmpower) && ibmpower.IBM){
                foreach(Collider2D coll in ibmpower.IBM.gameObject.GetComponentsInChildren<Collider2D>()){
                    foreach (Collider2D Coll in Holding.gameObject.GetComponentsInChildren<Collider2D>())
                    {
                        IgnoreCollisionStackController.IgnoreCollisionSubstituteMethod(coll, Coll, ignore:HoldingState);
                    }
                }
            }
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            try
            {
                LimbBehaviour AnchorLimb = AnchorTarget.GetComponent<LimbBehaviour>();
                LimbRegrowth AnchorAjinLimb = AnchorTarget.GetComponent<LimbRegrowth>();
                if (
                    RegrowPerson.Revival
                    && (
                        RegrowPerson.RegenSection.Contains(AnchorLimb)
                        && !RegrowPerson.RegenSection.Contains(limb)
                    )
                )
                {
                    if ((!AnchorAjinLimb.IsNewLimb && !IsNewLimb))
                    {
                        if (other.gameObject.TryGetComponent(out LimbBehaviour Limb))
                        {
                            if (!other.gameObject.TryGetComponent(out LimbRegrowth limbregrowth))
                            {
                                Limb.Crush();
                                return;
                            }
                            else{
                                if(!limbregrowth.RegrowPerson.Revival){
                                    Limb.Crush();
                                    return;
                                }
                            }
                        }
                        if (
                            other.gameObject.TryGetComponent(
                                out DestroyableBehaviour destroyBehaviour
                            )
                        )
                        {
                            destroyBehaviour.Break();
                            return;
                        }
                    }
                }
            }
            catch { }
        }

        public void CalcLimbConnection()
        {
            if (limb.name == "MiddleBody"){
                return;
            }
            LimbBehaviour AnchorLimb = AnchorTarget.GetComponent<LimbBehaviour>();
            LimbRegrowth AnchorAjinLimb = AnchorTarget.GetComponent<LimbRegrowth>();
            if (!limb.Joint)
            {
                ConnectToMiddleBody = false;
                if (ConnectLimbs.Contains(AnchorLimb))
                {
                    ConnectLimbs.Remove(AnchorLimb);
                    if(AnchorAjinLimb.ConnectLimbs.Contains(limb)){
                        AnchorAjinLimb.ConnectLimbs.Remove(limb);
                    }
                }
            }
            else
            {
                limb.HasJoint = true;
                if (!ConnectLimbs.Contains(AnchorLimb))
                {
                    ConnectLimbs.Add(AnchorLimb);
                }
                ConnectToMiddleBody = AnchorAjinLimb.ConnectToMiddleBody;
                if (!AnchorAjinLimb.ConnectLimbs.Contains(limb))
                {
                    AnchorAjinLimb.ConnectLimbs.Add(limb);
                }
                if (RegrowPerson.Revival)
                {
                    if (
                        RegrowPerson.RegenSection.Contains(limb)
                        && !RegrowPerson.RegenSection.Contains(AnchorLimb)
                    )
                    {
                        RegrowPerson.RegenSection.Add(AnchorLimb);
                        AnchorLimb.gameObject.GetComponent<LimbRegrowth>().Revival = true;
                    }
                    if (
                        RegrowPerson.RegenSection.Contains(AnchorLimb)
                        && !RegrowPerson.RegenSection.Contains(limb)
                    )
                    {
                        RegrowPerson.RegenSection.Add(limb);
                        limb.gameObject.GetComponent<LimbRegrowth>().Revival = true;
                    }
                }
            }

        }

        public void CountConnectLimbs()
        {
            ConnectLimbs = ConnectLimbs.Distinct().ToList();
            limb.ConnectedLimbs = ConnectLimbs;
            List<ConnectedNodeBehaviour> NodeConnections = new List<ConnectedNodeBehaviour>();
            for (int i = 0; i < ConnectLimbs.Count; i++)
            {
                NodeConnections.Add(ConnectLimbs[i].NodeBehaviour);
            }
            limb.NodeBehaviour.Connections = NodeConnections.ToArray();
        }

        private void OnDisable()
        {
            if(!Ready){
                return;
            }

            if (limb.Joint)
            {
                limb.BreakingThreshold = BreakingThreshold;
            }
            if (!limb.name.Contains("MiddleBody"))
            {
                LimbBehaviour AnchorLimb = AnchorTarget.GetComponent<LimbBehaviour>();
                while (ConnectLimbs.Contains(AnchorLimb))
                {
                    ConnectLimbs.Remove(AnchorLimb);
                }
                while (AnchorTarget.GetComponent<LimbRegrowth>().ConnectLimbs.Contains(limb))
                {
                    AnchorTarget.GetComponent<LimbRegrowth>().ConnectLimbs.Remove(limb);
                }
                ConnectLimbs.Remove(AnchorTarget.GetComponent<LimbBehaviour>());
            }
            foreach (LimbBehaviour EachLimb in ConnectLimbs)
            {
                LimbRegrowth limbregrowth = EachLimb.gameObject.GetComponent<LimbRegrowth>();
                while (limbregrowth.ConnectLimbs.Contains(limb))
                {
                    limbregrowth.ConnectLimbs.Remove(limb);
                }
            }

            ConnectLimbs.Clear();
        }

        private void OnEnable()
        {
            if (IsNewLimb)
            {
                foreach (LimbBehaviour Limb in ConnectLimbs)
                {
                    if (Limb.Joint)
                    {
                        Destroy(Limb.Joint);
                    }
                }
                if (limb.Joint)
                {
                    Destroy(limb.Joint);
                }
                if (RegrowPerson.RegenSection.Contains(limb))
                {
                    RegrowPerson.RegenSection.Remove(limb);
                }
                Hide();
                ConnectLimbs.Clear();
            }
        }

        public void Move(Vector3 Displacement){
            if(ConnectLimbs.Count==0){
                return;
            }
            foreach (LimbBehaviour EachLimb in ConnectLimbs)
            {
                if(EachLimb.gameObject != AnchorTarget){
                    Vector2 differ= (Vector2)EachLimb.gameObject.transform.position - EachLimb.gameObject.GetComponent<Rigidbody2D>().position;
                    EachLimb.gameObject.transform.position -= Displacement + (Vector3)differ;
                    EachLimb.GetComponent<LimbRegrowth>().Move(Displacement);
                }
            }
        }

        public void AttachLimb(bool rotate = true)
        {
            if(limb.name=="MiddleBody"){
                return;
            }

            if(rotate){
                Quaternion rotation = limb.gameObject.transform.rotation;
                limb.gameObject.transform.rotation = rotation;
                
                Vector2 Displacement = ((Vector2)base.transform.TransformPoint(LocalAnchorPosition)) - (Vector2)AnchorTarget.transform.TransformPoint(AnchorPosition);
                base.transform.position -= (Vector3)Displacement;
                try{
                    Move(Displacement);
                }
                catch{}
            }
            Vector2 position = AnchorTarget.GetComponent<Rigidbody2D>().position;

            limb.Joint = limb.gameObject.AddComponent<HingeJoint2D>();
            limb.Joint.anchor = LocalAnchorPosition;

            if(rotate){
                limb.Joint.autoConfigureConnectedAnchor = false;
            }

            limb.Joint.connectedAnchor = AnchorPosition;
            AnchorTarget.GetComponent<Rigidbody2D>().position = position;

            limb.gameObject.transform.rotation = AnchorTarget.gameObject.transform.rotation;
            limb.Joint.enableCollision = false;
            limb.Joint.connectedBody = AnchorTarget.GetComponent<Rigidbody2D>();

            limb.OriginalJointLimits=LimbLimit;
            JointAngleLimits2D newlimit = new JointAngleLimits2D();
            newlimit.min=LimbLimit.x;
            newlimit.max=LimbLimit.y;
            limb.Joint.limits = newlimit;

            if(limb.name=="UpperArm"||limb.name=="UpperArmFront"){
                limb.Joint.useLimits = false;
            }
            else{
                limb.Joint.useLimits = true;
            }

            limb.HasJoint = true;
            limb.Broken = false;
            limb.IsDismembered = false;
            limb.CirculationBehaviour.IsDisconnected = false;

            ConnectLimbs.Add(AnchorTarget.GetComponent<LimbBehaviour>());
            AnchorTarget.GetComponent<LimbRegrowth>().ConnectLimbs.Add(limb);

            if (limb.gameObject.TryGetComponent<GoreStringBehaviour>(out var GoreString))
            {
                GoreString.DestroyJoint();
            }
            if (AnchorTarget.TryGetComponent<GoreStringBehaviour>(out var TargetGoreString))
            {
                GoreString.DestroyJoint();
            }

            limb.BreakingThreshold = Mathf.Infinity;

            limb.Joint.useMotor = true;
            limb.Joint.motor = new JointMotor2D
            {
                maxMotorTorque = limb.MotorStrength,
                motorSpeed = 0f
            };
        }

        private void InverseDirectionAttachLimb(Vector2 Displacement, bool UseDisplacement)
        {
            Vector2 displacement = Vector2.zero;
            if (UseDisplacement)
            {
                displacement = Displacement;
            }
            else
            {
                displacement =
                    AnchorTarget.transform.TransformPoint(AnchorPosition)
                    - this.gameObject.transform.TransformPoint(LocalAnchorPosition);
            }
            AnchorTarget.transform.position =
                (Vector2)AnchorTarget.transform.position - displacement;
            if (!UseDisplacement)
            {
                AttachLimb(false);
            }
            AnchorTarget
                .GetComponent<LimbRegrowth>()
                .InverseDirectionAttachLimb(displacement, true);
        }

        public void LimbAttract(Vector3 Direction)
        {
            Rigidbody2D RigBody = GetComponent<Rigidbody2D>();
            Rigidbody2D OtherRigBody = AnchorTarget.GetComponent<Rigidbody2D>();

            RigBody.angularVelocity=Mathf.MoveTowards(RigBody.angularVelocity,0f,Time.deltaTime*120);
            OtherRigBody.angularVelocity=Mathf.MoveTowards(OtherRigBody.angularVelocity,0f,Time.deltaTime*120);
            if(RigBody.angularVelocity>360){
                RigBody.angularVelocity=180f;
            }
            if(OtherRigBody.angularVelocity>360){
                OtherRigBody.angularVelocity=180f;
            }

            if (RigBody.velocity.magnitude < 20f)
            {
                RigBody.AddForceAtPosition(
                    Direction * Time.deltaTime * -2000f * RigBody.mass,
                    limb.gameObject.transform.TransformPoint(LocalAnchorPosition)
                );
                RigBody.velocity = RigBody.velocity.magnitude * Direction * -1;
            }
            else
            {
                RigBody.velocity = 22f * Direction * -1;
            }
            if (OtherRigBody.velocity.magnitude < 20f)
            {
                OtherRigBody.AddForceAtPosition(
                    Direction * Time.deltaTime * 2000f * OtherRigBody.mass,
                    AnchorTarget.transform.TransformPoint(AnchorPosition)
                );
                OtherRigBody.velocity = RigBody.velocity.magnitude * Direction;
            }
            else
            {
                OtherRigBody.velocity = 22f * Direction;
            }
        }

        public void Revive()
        {
            if(IsNewLimb){
                StartCoroutine("RevealAfterTwoFrame");
                IsNewLimb=false;
            }
            FinishRevival = true;

            limb.CirculationBehaviour.BloodFlow = 1f;
            limb.CirculationBehaviour.IsPump = limb.CirculationBehaviour.WasInitiallyPumping;
            limb.PhysicalBehaviour.BurnIntensity = 0f;
            limb.Wince(1f);

            limb.HealBone();
            limb.IsZombie = false;
            limb.CirculationBehaviour.HealBleeding();
            limb.CirculationBehaviour.IsDisconnected = false;
            limb.PhysicalBehaviour.Charge = 0f;
            limb.PhysicalBehaviour.charge = 0f;

            if(limb.BodyTemperature<35f || limb.BodyTemperature>39f){
                limb.BodyTemperature = Mathf.MoveTowards(limb.BodyTemperature,37f,Time.deltaTime*37f);
                FinishRevival = false;
            }

            if(limb.PhysicalBehaviour.Temperature < 35f || limb.PhysicalBehaviour.Temperature > 39f){
                limb.PhysicalBehaviour.Temperature = Mathf.MoveTowards(limb.PhysicalBehaviour.Temperature,37f,Time.deltaTime*37f);
                FinishRevival = false;
            }

            try
            {
                if (limb.Health < limb.InitialHealth)
                {
                    limb.Health = limb.InitialHealth;
                }
            }
            catch { }

            try
            {
                limb.CirculationBehaviour.ClearLiquid();
                limb.CirculationBehaviour.AddLiquid(limb.GetOriginalBloodType(), 1f);
            }
            catch { }

            if (limb.SkinMaterialHandler.AcidProgress > AcidProgress)
            {
                limb.SkinMaterialHandler.AcidProgress -= 0.5f * Time.deltaTime;
                FinishRevival = false;
            }

            if (limb.PhysicalBehaviour.BurnProgress > BurnProgress)
            {
                limb.PhysicalBehaviour.BurnProgress -= 0.2f * Time.deltaTime;
                FinishRevival = false;
            }

            if (limb.SkinMaterialHandler.RottenProgress > RottenProgress)
            {
                limb.SkinMaterialHandler.RottenProgress -= 0.5f * Time.deltaTime;
                FinishRevival = false;
            }

            for (int j = 0; j < limb.SkinMaterialHandler.damagePoints.Length; j++)
            {
                if (limb.SkinMaterialHandler.damagePoints[j].z > 0)
                {
                    limb.SkinMaterialHandler.damagePoints[j].z -= 2f * Time.deltaTime;
                    FinishRevival = false;
                }
            }

            if (FinishRevival)
            {
                limb.SkinMaterialHandler.currentDamagePointCount = 0;
            }

            limb.SkinMaterialHandler.Sync();
            limb.Vitality = 1f;
            limb.Numbness = 0f;

            limb.CirculationBehaviour.IsPump = limb.CirculationBehaviour.WasInitiallyPumping;

            limb.BruiseCount = 0;
            limb.PhysicalBehaviour.Extinguish();
            limb.CirculationBehaviour.BleedingPointCount = 0;
            limb.CirculationBehaviour.StabWoundCount = 0;
            limb.CirculationBehaviour.GunshotWoundCount = 0;
            limb.LungsPunctured = false;
            if (limb.gameObject.transform.localScale != scale)
            {
                limb.gameObject.transform.localScale = Vector3.MoveTowards(
                    limb.gameObject.transform.localScale,
                    scale,
                    Time.deltaTime
                );
                limb.PhysicalBehaviour.RecalculateMassBasedOnSize();
                FinishRevival = false;
            }
            if(limb.Joint){
                limb.Joint.autoConfigureConnectedAnchor = false;
                limb.Joint.connectedAnchor = AnchorPosition;
            }
        }

        public void InverseDisintegrate()
        {
            foreach (Rigidbody2D RigChild in GetComponentsInChildren<Rigidbody2D>())
            {
                RigChild.simulated = true;
            }
            this.gameObject.SetActive(value: true);
            limb.PhysicalBehaviour.isDisintegrated = false;
            ConnectLimbs.Clear();
            StartCoroutine("LateAwake");
        }

        IEnumerator RevealAfterTwoFrame(){
            yield return 0;
            yield return 1;
            Reveal();
        }

        public void Reveal()
        {
            if (TryGetComponent<BloodTankBehaviour>(out var component))
            {
                component.enabled = true;
            }
            foreach (Renderer RenChild in GetComponentsInChildren<Renderer>())
            {
                RenChild.enabled = true;
            }
            foreach (Collider2D coll in GetComponentsInChildren<Collider2D>())
            {
                coll.enabled = true;
                foreach (GameObject NoCollideObject in RegrowPerson.NoCollideObjects)
                {
                    Physics2D.IgnoreCollision(
                        coll,
                        NoCollideObject.GetComponent<Collider2D>(),
                        ignore: true
                    );
                }
            }
            limb.SkinMaterialHandler.AcidProgress = 1f;
            IsNewLimb=false;
        }

        public void Hide()
        {
            if (TryGetComponent<BloodTankBehaviour>(out var component))
            {
                component.enabled = false;
            }
            foreach (Renderer RenChild in GetComponentsInChildren<Renderer>())
            {
                RenChild.enabled = false;
            }
            foreach (Collider2D coll in GetComponentsInChildren<Collider2D>())
            {
                coll.enabled = false;
            }
        }

        public void DisintegrateStabObject()
        {
            foreach (PhysicalBehaviour StabObject in limb.PhysicalBehaviour.beingStabbedBy)
            {
                AjinDisintegrate AD = StabObject.gameObject.GetOrAddComponent<AjinDisintegrate>();
            }
        }
    }

    public class AjinDisintegrate : MonoBehaviour
    {
        private void Awake()
        {
            StartCoroutine("AjinDisintegraion");
        }

        IEnumerator AjinDisintegraion()
        {
            yield return null;
            yield return new WaitForSeconds(1f);
            Destroy(this.gameObject);
        }
    }

    public class LimbRegrowthPerson : MonoBehaviour
    {
        List<float> SegmentMass = new List<float>();
        public ArrayList BodySegment = new ArrayList();
        public List<LimbBehaviour> RegenSection = new List<LimbBehaviour>();
        public List<GameObject> NoCollideObjects = new List<GameObject>();

        public Dictionary<PoseState, RagdollPose> linkedPoses =
            new Dictionary<PoseState, RagdollPose>();

        public RagdollPose ActivePose;
        public PersonBehaviour Person;
        public bool Revival = false;
        public bool Ready = false;

        public PersonBehaviour DetachedLimbPerson;
        public string IBMUser = "Sato";


        float timer;
        float Timer;
        float tTimer;

        private void Awake()
        {
            Person = this.gameObject.GetComponent<PersonBehaviour>();
            foreach (LimbBehaviour limb in Person.Limbs)
            {
                NoCollideObjects.Add(limb.gameObject);
                limb.gameObject.AddComponent<Optout>();
            }
            Person.gameObject.GetComponent<DisintegrationCounterBehaviour>().DisintegrationCount =
                -100000000;
            StartCoroutine("LateAwake");
        }

        IEnumerator LateAwake()
        {
            yield return 0;

            foreach (LimbBehaviour limb in Person.Limbs)
            {
                if (limb.name == "Head")
                {
                    if (!limb.gameObject.HasComponent<ParalyzingScream>())
                    {
                        limb.gameObject.AddComponent<ParalyzingScream>();
                    }
                }
                if (!limb.gameObject.HasComponent<LimbRegrowth>())
                {
                    LimbRegrowth limbregrowth = limb.gameObject.AddComponent<LimbRegrowth>();
                }
            }

            foreach (LimbRegrowth AjinLimb in GetComponentsInChildren<LimbRegrowth>())
            {
                AjinLimb.RegrowPerson = this;
            }

            linkedPoses = Person.LinkedPoses;
            if (!DetachedLimbPerson)
            {
                DetachedLimbPersonSetup();
            }

            yield return 1;

            Ready = true;
        }

        public void DetachedLimbPersonSetup()
        {
            DetachedLimbPerson = Instantiate(Person);
            Destroy(DetachedLimbPerson.gameObject.GetComponent<LimbRegrowthPerson>());
            foreach (LimbBehaviour EachLimb in DetachedLimbPerson.Limbs)
            {
                Destroy(EachLimb.gameObject.GetComponent<LimbRegrowth>());
                EachLimb.PhysicalBehaviour.Disintegrate();
            }
        }

        public List<LimbBehaviour> ChoseRegenSegment()
        {
            SegmentsCount();
            int Index = 0;
            for (int i = 0; i < SegmentMass.Count; i++)
            {
                if (i == 0)
                {
                    Index = 0;
                }
                else
                {
                    if (SegmentMass[i] > SegmentMass[Index])
                    {
                        Index = i;
                    }
                }
            }

            return (List<LimbBehaviour>)BodySegment[Index];
        }

        private void LateUpdate()
        {
            if(!Ready){
                return;
            }
            if (!Person.IsAlive() && !Revival && Timer <= 1)
            {
                timer += Time.deltaTime;
                if (timer > 2f)
                {
                    Revival = true;
                    tTimer = 0f;
                    SegmentsCount();

                    foreach (LimbBehaviour limb in Person.Limbs)
                    {
                        limb.BreakingThreshold = Mathf.Infinity;
                        if (limb.PhysicalBehaviour.isDisintegrated)
                        {
                            LimbRegrowth limbRegrowth = limb.GetComponent<LimbRegrowth>();
                            limbRegrowth.IsNewLimb = true;
                            limbRegrowth.InverseDisintegrate();
                        }
                        foreach (GameObject NoCollideObject in NoCollideObjects)
                        {
                            Physics2D.IgnoreCollision(
                                limb.GetComponent<Collider2D>(),
                                NoCollideObject.GetComponent<Collider2D>(),
                                ignore: true
                            );
                        }
                    }

                    int Index = 0;
                    for (int i = 0; i < SegmentMass.Count; i++)
                    {
                        if (i == 0)
                        {
                            Index = 0;
                        }
                        else
                        {
                            if (SegmentMass[i] > SegmentMass[Index])
                            {
                                Index = i;
                            }
                        }
                    }

                    RegenSection = (List<LimbBehaviour>)BodySegment[Index];

                    Collider2D[] colls = Physics2D.OverlapCircleAll(
                        RegenSection[0].gameObject.transform.position,
                        8f,
                        LayerMask.GetMask("Objects")
                    );

                    List<GameObject> NewLimbs = new List<GameObject>();
                    List<LimbBehaviour> LimbsWillBeReplace = new List<LimbBehaviour>();
                    foreach (List<LimbBehaviour> limbs in BodySegment)
                    {
                        bool Abandon = true;
                        foreach (LimbBehaviour limb in limbs)
                        {
                            if (colls.Contains(limb.gameObject.GetComponent<Collider2D>()))
                            {
                                Abandon = false;
                                break;
                            }
                        }
                        if (Abandon)
                        {
                            foreach (LimbBehaviour limb in limbs)
                            {
                                if (!limb.GetComponent<LimbRegrowth>().IsNewLimb)
                                {
                                    // Vector3 velocity=limb.gameObject.GetComponent<Rigidbody2D>().velocity;
                                    // limb.gameObject.GetComponent<Rigidbody2D>().velocity=Vector3.zero;
                                    GameObject NewLimb = Instantiate(
                                        limb.gameObject,
                                        limb.gameObject.transform.parent
                                    );
                                    NewLimb.GetComponent<Rigidbody2D>().angularVelocity=0f;
                                    NewLimb.GetComponent<Rigidbody2D>().velocity=Vector3.zero;

                                    Destroy(limb.gameObject.GetComponent<LimbRegrowth>());
                                    Destroy(limb.gameObject.GetComponent<CirculationBehaviour>());
                                    Destroy(NewLimb.gameObject.GetComponent<HingeJoint2D>());

                                    if (limb.TryGetComponent<GoreStringBehaviour>(out GoreStringBehaviour GoreString))
                                    {
                                        GoreString.DestroyJoint();
                                    }

                                    limb.Health = 0f;

                                    limb.HasJoint = false;
                                    // limb.gameObject.transform.parent = Person
                                    //     .gameObject
                                    //     .transform
                                    //     .parent;
                                    // limb.gameObject.GetComponent<Rigidbody2D>().velocity=velocity;

                                    LimbReplace(limb, NewLimb.GetComponent<LimbBehaviour>());
                                    NewLimbs.Add(NewLimb);
                                    NoCollideObjects.Add(NewLimb);
                                    LimbRegrowth LimbRegrow =
                                    NewLimb.gameObject.GetComponent<LimbRegrowth>();
                                    LimbRegrow.Hide();
                                    LimbRegrow.ConnectLimbs.Clear();

                                    NewLimb.GetComponent<LimbBehaviour>().Person = Person;
                                    LimbRegrow.IsNewLimb = true;
                                }
                            }
                        }
                    }
                    foreach (GameObject NewLimb in NewLimbs)
                    {
                        foreach (GameObject NoCollideObject in NoCollideObjects)
                        {
                            if(NoCollideObject!=NewLimb){
                                Physics2D.IgnoreCollision(
                                    NewLimb.GetComponent<Collider2D>(),
                                    NoCollideObject.GetComponent<Collider2D>(),
                                    ignore: true
                                );
                            }
                        }
                    }
                    // Person.Limbs = GetComponentsInChildren<LimbBehaviour>();
                    ModAPI.Notify("Revival begins");
                    Timer = 0f;
                }
            }
            else
            {
                timer = 0f;
                tTimer = 0f;
            }
            try
            {
                if (Revival || Timer >= 1f)
                {
                    tTimer += Time.deltaTime;
                    Person.Braindead = false;
                    Person.BrainDamaged = false;
                    Person.Consciousness += Time.deltaTime * 0.25f;
                    Person.OxygenLevel = 1f;
                    Revival = false;
                    foreach (LimbBehaviour EachLimb in Person.Limbs)
                    {
                        LimbRegrowth limbRegrowth = EachLimb.GetComponent<LimbRegrowth>();
                        if (!limbRegrowth.FinishRevival)
                        {
                            Revival = true;
                            if (RegenSection.Contains(EachLimb))
                            {
                                limbRegrowth.Revival = true;
                            }
                        }
                        if (EachLimb.PhysicalBehaviour.isDisintegrated)
                        {
                            limbRegrowth.IsNewLimb = true;
                            limbRegrowth.InverseDisintegrate();
                            limbRegrowth.Revival = true;
                        }
                    }
                    RegenSection.Distinct();
                    for (int i = 0; i < RegenSection.Count; i++)
                    {
                        if (!Person.Limbs.Contains(RegenSection[i]))
                        {
                            RegenSection.RemoveAt(i);
                            i--;
                        }
                    }
                    if (RegenSection.Count < 14)
                    {
                        Revival = true;
                    }
                    if (!Revival)
                    {
                        if (Timer < 1f)
                        {
                            Timer = 1f;
                            for (int i = 0; i < RegenSection.Count; i++)
                            {
                                LimbBehaviour limb = RegenSection[i];
                                if (limb.gameObject.TryGetComponent(out LimbRegrowth limbRegrowth))
                                {
                                    limbRegrowth.Revive();
                                    limb.GetComponent<Rigidbody2D>().mass = limb.PhysicalBehaviour.InitialMass;
                                }
                                else
                                {
                                    RegenSection.Remove(limb);
                                    i--;
                                    Revival = true;
                                    Timer = 0f;
                                }
                                FixReferenceAngle(limb.Joint);
                            }
                        }
                        if (!Person.IsAlive())
                        {
                            Revival = true;
                            Timer = 0f;
                        }
                        Timer += Time.deltaTime;

                        Person.OxygenLevel = 1f;
                        Person.ShockLevel = 0f;
                        Person.PainLevel = 0f;
                        Person.Braindead = false;
                        Person.BrainDamaged = false;

                        for (int i = 0; i < RegenSection.Count; i++)
                        {
                            LimbBehaviour limb = RegenSection[i];
                            if (limb.gameObject.TryGetComponent(out LimbRegrowth limbRegrowth))
                            {
                                limbRegrowth.Revival = true;
                                limbRegrowth.FinishRevival = false;
                            }
                        }

                        if (Timer >= 1.5f)
                        {
                            FinishRevivalAction();
                        }
                    }
                }
            }
            catch { }
        }

        public void FixReferenceAngle(HingeJoint2D joint){
            if (
                joint
                && (
                    (
                        joint.referenceAngle > 0.5f
                        || joint.referenceAngle < -0.5f
                    )
                    || (
                        joint.jointAngle > 180f
                        || joint.jointAngle < -180f
                    )
                )
            )
            {

                Destroy(joint);
                joint.gameObject.GetComponent<LimbRegrowth>().AttachLimb(false);
            }
        }

        private void FinishRevivalAction(){
            foreach (LimbBehaviour limb in Person.Limbs)
            {
                try{
                    LimbRegrowth limbRegrowth = limb.gameObject.GetComponent<LimbRegrowth>();
                    limbRegrowth.Revival = false;
                    limbRegrowth.FinishRevival = false;
                    limbRegrowth.ConnectLimbs.Clear();
                    limb.BreakingThreshold = limbRegrowth.BreakingThreshold;
                    limb.PhysicalBehaviour.ForceNoCharge = false;
                    limb.gameObject.GetComponent<Rigidbody2D>().mass = limb.PhysicalBehaviour.TrueInitialMass;
                    if (limb.name == "Head")
                    {
                        if (!limb.gameObject.HasComponent<ParalyzingScream>())
                        {
                            limb.gameObject.AddComponent<ParalyzingScream>();
                        }
                    }
                    foreach (LimbBehaviour EachLimb in Person.Limbs)
                    {
                        if(EachLimb != limb){
                            Physics2D.IgnoreCollision(limb.gameObject.GetComponent<Collider2D>(),EachLimb.gameObject.GetComponent<Collider2D>(),ignore: true);
                        }
                    }
                }
                catch{}
            }
            Timer = 0f;
            Revival = false;
            Person.Consciousness = 1f;
            ModAPI.Notify("Finish reviving");
        }

        private void LimbReplace(LimbBehaviour LimbToReplace, LimbBehaviour Limb)
        {
            LimbToReplace.gameObject.transform.SetParent(Person.gameObject.transform.parent);
            Limb.InitialHealth = LimbToReplace.InitialHealth;
            Limb.CirculationBehaviour.WasInitiallyPumping = LimbToReplace
                .CirculationBehaviour
                .WasInitiallyPumping;
            LimbToReplace.PhysicalBehaviour.SimulateTemperature = false;
            Limb.BreakingThreshold = LimbToReplace.BreakingThreshold;
            Limb.name = LimbToReplace.name;
            foreach (LimbBehaviour EachLimb in Person.Limbs)
            {
                if(EachLimb.NearestLimbToBrain == LimbToReplace){
                    EachLimb.NearestLimbToBrain = Limb;
                }
                if (
                    EachLimb.gameObject.GetComponent<LimbRegrowth>().AnchorTarget
                    == LimbToReplace.gameObject
                )
                {
                    EachLimb.gameObject.GetComponent<LimbRegrowth>().AnchorTarget = Limb.gameObject;
                }
                if (
                    EachLimb.CirculationBehaviour.PushesTo.Contains(
                        LimbToReplace.CirculationBehaviour
                    )
                )
                {
                    for (int i = 0; i < EachLimb.CirculationBehaviour.PushesTo.Length; i++)
                    {
                        if (
                            EachLimb.CirculationBehaviour.PushesTo[i]
                            == LimbToReplace.CirculationBehaviour
                        )
                        {
                            EachLimb.CirculationBehaviour.PushesTo[i] = Limb.CirculationBehaviour;
                        }
                    }
                }
                if (EachLimb.CirculationBehaviour.Source == LimbToReplace.CirculationBehaviour)
                {
                    EachLimb.CirculationBehaviour.Source = Limb.CirculationBehaviour;
                }
                if (
                    EachLimb.gameObject.TryGetComponent<GoreStringBehaviour>(
                        out GoreStringBehaviour goreString
                    )
                    && goreString.Other == LimbToReplace.gameObject.GetComponent<Rigidbody2D>()
                )
                {
                    goreString.Other = Limb.gameObject.GetComponent<Rigidbody2D>();
                }
            }

            foreach ((PoseState posestate, RagdollPose ragdollpose) in linkedPoses)
            {
                for (int i = 0; i < ragdollpose.Angles.Count; i++)
                {
                    if (ragdollpose.Angles[i].Limb == LimbToReplace)
                    {
                        RagdollPose.LimbPose NewPose = ragdollpose.Angles[i];
                        NewPose.Limb = Limb;
                        ragdollpose.Angles[i] = NewPose;
                    }
                }
                ragdollpose.ConstructDictionary();
            }

            for (int i = 0; i < Person.ActivePose.Angles.Count; i++)
            {
                if (Person.ActivePose.Angles[i].Limb == LimbToReplace)
                {
                    RagdollPose.LimbPose NewPose = Person.ActivePose.Angles[i];
                    NewPose.Limb = Limb;
                    Person.ActivePose.Angles[i] = NewPose;
                }
            }

            if (LimbToReplace.name.Contains("Head"))
            {
                try
                {
                    typeof(PersonBehaviour)
                        .GetField("Head", BindingFlags.NonPublic | BindingFlags.Instance)
                        .SetValue(LimbToReplace.Person, Limb);
                }
                catch { }
            }

            Limb.Person = Person;
            if (!DetachedLimbPerson)
            {
                DetachedLimbPersonSetup();
            }
            LimbToReplace.Person = DetachedLimbPerson;
            Person.Limbs[Person.Limbs.ToList().IndexOf(LimbToReplace)] = Limb;
        }

        private void SegmentsCount()
        {
            BodySegment.Clear();
            SegmentMass.Clear();
            List<LimbBehaviour> limbs = Person.Limbs.ToList();
            List<LimbBehaviour> Segments = new List<LimbBehaviour>();
            for (int i = 0; i < limbs.Count; i++)
            {
                List<LimbBehaviour> SegmentLimbs = new List<LimbBehaviour>();
                LimbBehaviour limb = limbs[i];
                float sMass = 0f;
                SegmentLimbs.Add(limb);
                if (!limb.PhysicalBehaviour.isDisintegrated)
                {
                    sMass += limb.PhysicalBehaviour.InitialMass;
                    Segments.AddRange(limb.gameObject.GetComponent<LimbRegrowth>().ConnectLimbs);
                    foreach (LimbBehaviour Limb in Segments)
                    {
                        if (!Limb.PhysicalBehaviour.isDisintegrated)
                        {
                            limbs.Remove(Limb);
                            sMass += Limb.PhysicalBehaviour.InitialMass;
                            SegmentLimbs.Add(Limb);
                        }
                    }
                    while (Segments.Count > 0)
                    {
                        List<LimbBehaviour> NewSegment = new List<LimbBehaviour>();
                        foreach (LimbBehaviour Limb in Segments)
                        {
                            if (!Limb.PhysicalBehaviour.isDisintegrated)
                            {
                                foreach (
                                    LimbBehaviour EachLimb in Limb.gameObject
                                        .GetComponent<LimbRegrowth>()
                                        .ConnectLimbs
                                )
                                {
                                    if (
                                        limbs.Contains(EachLimb)
                                        && limbs.IndexOf(EachLimb) > i
                                        && !EachLimb.PhysicalBehaviour.isDisintegrated
                                    )
                                    {
                                        limbs.Remove(EachLimb);
                                        NewSegment.Add(EachLimb);
                                        SegmentLimbs.Add(EachLimb);
                                        sMass += EachLimb.PhysicalBehaviour.InitialMass;
                                    }
                                }
                            }
                        }
                        Segments.Clear();
                        Segments.AddRange(NewSegment);
                        NewSegment.Clear();
                    }
                }
                SegmentMass.Add(sMass);
                BodySegment.Add(SegmentLimbs);
            }
        }
    }
}

// Originally uploaded by 'NeitherFishNorCat'. Do not reupload without their explicit permission.
