
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mod
{
	public class IBMPower : MonoBehaviour
	{
		public PersonBehaviour IBM;
		public PersonBehaviour Person;
		float Timer;
		Color IBMColor;
		bool SwitchColor=false;
		Texture2D IBMTex;
		Texture2D IBMBoneTex;
		public Sprite IBMHead;
		public bool ReleaseIBM;
		bool Appearing;
		public string IBMUser;
		FieldInfo materialinfo;
		public GameObject IBMPrefab;
		public Sprite IBMWingTex;
		ParticleSystem[] particlesystems;
		UseEventTrigger ActivationComponent;
		
		public float UprightForce;

		float DisappearProgress
		{
			get
			{
				return disappearProgress;
			}
			set
			{
				disappearProgress=value;
				if(value<=0){
					return;
				}
				bool ReadyToDestroy=true;
				foreach (LimbBehaviour EachLimb in IBM.Limbs)
				{
					Material material=(Material)materialinfo.GetValue(EachLimb.SkinMaterialHandler);
					if(value>material.GetFloat(ShaderProperties.Get("_AcidProgress"))){
						material.SetFloat(ShaderProperties.Get("_AcidProgress"), value);
					}
				}
				if(value>=0.7){
					foreach(ParticleSystem particlesystem in particlesystems){
						if(particlesystem.particleCount>0){
							ParticleSystem.EmissionModule emisson=particlesystem.emission;
							emisson.rateOverTimeMultiplier=Mathf.Lerp(emisson.rateOverTimeMultiplier,0,value);
							ReadyToDestroy=false;
							emisson.enabled=false;
						}
					}
				}
				if(value>=0.8f&&ReadyToDestroy){
					Destroy(IBM.gameObject);
				}
			}
		}

		
		Vector3 LookingDirection
		{
			get
			{
				if(Person.gameObject.transform.localScale.x>0)
				{
					return Vector3.right;
				}
				else
				{
					return Vector3.left;
				}
			}
		}

		float disappearProgress;

		private void Awake() {
			Person = this.gameObject.GetComponent<PersonBehaviour>();
			IBMTex = ModAssets.IBMTex;
			IBMBoneTex = ModAssets.IBMBoneTex;
			IBMColor = new Color(1f, 1f, 1f, 1f);
			IBMUser = Person.GetComponent<LimbRegrowthPerson>().IBMUser;
			materialinfo = typeof(SkinMaterialHandler).GetField("material", BindingFlags.NonPublic | BindingFlags.Instance);
			IBMHead = ModAssets.IBMHeadTexture[IBMUser];
			if(IBMUser == "Takeshi"){
				IBMWingTex=ModAPI.LoadSprite("IBM Heads/Takeshi_IBM_Wing.png");
			}
			AddActivationAction();
		}

		public void AddActivationAction(){
			if(ActivationComponent){
				if(ActivationComponent.gameObject.GetComponent<LimbBehaviour>().Person==Person){
					return;
				}
			}
			Destroy(ActivationComponent);
			foreach(LimbBehaviour limb in Person.Limbs){
				if(limb.name=="MiddleBody"){
					if(limb.gameObject.TryGetComponent<UseEventTrigger>(out var Use)){
						Destroy(Use);
					}
					ActivationComponent=limb.gameObject.AddComponent<UseEventTrigger>();
					ActivationComponent.Action = () =>
					{
						if(!IBM){
							InitIBM();
							foreach (LimbBehaviour Limb in IBM.Limbs)
							{
								if(Limb.name != "Head"){
									Limb.PhysicalBehaviour.Selectable=false;
								}
							}
							foreach (LimbRegrowth AjinLimb in base.GetComponentsInChildren<LimbRegrowth>())
							{	try{
									AjinLimb.ChangeHoldAction();
								}
								catch{}
							}
							ReleaseIBM=false;
						}
						else{
							if(!ReleaseIBM){
								ReleaseIBM=true;
								foreach (LimbBehaviour Limb in IBM.Limbs)
								{
									Limb.PhysicalBehaviour.Selectable=true;
								}
								IBM.gameObject.GetComponent<IBMAI>().AttackCD=1f;
								StartCoroutine("Push");
							}
							else if(Appearing){
								Appearing = false;
								DisappearProgress = 0.25f;
							}
						}
						particlesystems=IBM.gameObject.GetComponentsInChildren<ParticleSystem>();
					};
				}
			}
		}

		private void Update(){
			try{
				AddActivationAction();
			}
			catch{}
			UprightForce = Mathf.MoveTowards(UprightForce,0f,Time.deltaTime*600);
			IBMColor = Color.Lerp(new Color(1f,1f,1f,0.3f),new Color(1f,1f,1f,0.9f),Mathf.PingPong(Timer*0.7f, 1));
			IBMColor = Color.Lerp(new Color(1f,1f,1f,0f), IBMColor, Mathf.Clamp01(Timer*0.5f));
			Timer += Time.deltaTime;
			
			if(IBM){
				PersonRevive();
				if(Person.Consciousness<0.2f){
					ReleaseIBM=true;
				}
				float SumInitialHealth=0f;
				float SumHealth=0f;
				foreach(LimbBehaviour limb in IBM.Limbs){
					SumInitialHealth+=limb.InitialHealth;
					SumHealth+=limb.Health;
					LimbBehaviour MatchedLimb=MatchLimb(limb);
					if(!ReleaseIBM){
						if(MatchedLimb.gameObject.GetComponent<LimbRegrowth>().ConnectToMiddleBody){
							Transform rig1 = limb.gameObject.transform;
							Transform rig2 = MatchedLimb.gameObject.transform;
							rig1.rotation = rig2.rotation;
							rig1.position= rig2.position + this.gameObject.transform.rotation*(LookingDirection*0.1f*Mathf.Clamp(Timer,0f,1f));
						}
					}
					Material material=(Material)materialinfo.GetValue(limb.SkinMaterialHandler);
					material.SetFloat(ShaderProperties.Get("_AcidProgress"), 1-limb.Health/limb.InitialHealth);
					if(limb.name=="Head"){
						Rigidbody2D rigidBody=limb.gameObject.GetComponent<Rigidbody2D>();
						rigidBody.AddForce(rigidBody.mass*(Vector3.up)*UprightForce*Time.deltaTime);
					}
					limb.Color=IBMColor;
				}
				if(!Appearing){
					DisappearProgress=Mathf.MoveTowards(DisappearProgress,1f,Time.deltaTime*0.2f);
				}
				if(Timer>360f || SumHealth/SumInitialHealth<0.85f){
					Appearing = false;
				}
			}
		}

		public void PersonRevive(){
			IBM.OxygenLevel=1f;
			IBM.ShockLevel=0f;
			IBM.PainLevel=0f;
			IBM.Consciousness=1f;
			IBM.Braindead=false;
			IBM.BrainDamaged=false;
		}

		public void EnableCollisionWithPerson(PersonBehaviour person, bool able){
			foreach(LimbBehaviour Limb in IBM.Limbs){
				foreach(LimbBehaviour limb in person.Limbs){
					Physics2D.IgnoreCollision(Limb.gameObject.GetComponent<Collider2D>(), limb.gameObject.GetComponent<Collider2D>(), ignore: !able);
				}
			}
		}

		public void InitIBM(){
			Timer=0f;
			GameObject human=ModAPI.FindSpawnable("Human").Prefab;
			foreach (PhysicalBehaviour phys in human.GetComponentsInChildren<PhysicalBehaviour>())
			{
				phys.SpawnSpawnParticles=false;
			}
			GameObject IBMInstance=Instantiate(human);
			foreach (PhysicalBehaviour phys in human.GetComponentsInChildren<PhysicalBehaviour>())
			{
				phys.SpawnSpawnParticles=true;
			}

			IBMInstance.name="Black Ghost";
			IBMInstance.transform.localScale=Person.gameObject.transform.localScale;
			IBM=IBMInstance.GetComponent<PersonBehaviour>();
			IBM.SetBodyTextures(IBMTex, IBMBoneTex, IBMBoneTex, 1);

			EnableCollisionWithPerson(Person, false);

			foreach(LimbBehaviour Limb in IBM.Limbs){
				Limb.gameObject.GetComponent<SpriteRenderer>().sortingOrder+=20;
				IBMLimb IbmLimb=Limb.gameObject.AddComponent<IBMLimb>();
				Limb.PhysicalBehaviour.BulletPenetration=false;

				if(Limb.name.Contains("LowerArmFront")||Limb.name.Contains("LowerArm")){
					Limb.gameObject.AddComponent<IBMClaw>();
				}
				if(Limb.gameObject.TryGetComponent<GoreStringBehaviour>(out GoreStringBehaviour gorestring)){
					Destroy(gorestring);
				}

				if(Limb.name=="Head"){
					for(int i=0;i<Limb.gameObject.transform.childCount;i++){
						Transform child = Limb.gameObject.transform.GetChild(i);
						if(child.name=="Hair"||child.name=="Hat"){
							Destroy(child.gameObject);
						}
					}
					try{
						if(IBMUser!="Nagai"){
							Limb.gameObject.GetComponent<SpriteRenderer>().sprite=IBMHead;
						}
					}
					catch{}
				}

				if(Limb.name=="Foot"||Limb.name=="FootFront"){
					Limb.gameObject.AddComponent<IBMClaw>();
				}

				if(IBMUser=="Takeshi"){
					if(Limb.name=="Foot"||Limb.name=="FootFront"){
						Limb.gameObject.AddComponent<GripBehaviour>();
					}
					if(Limb.name.Contains("LowerArmFront")||Limb.name.Contains("LowerArm")){
						Destroy(Limb.gameObject.GetComponent<IBMClaw>());
						IBMWing IbmWing=Limb.gameObject.AddComponent<IBMWing>();
						IbmWing.IBMWingTex=IBMWingTex;
						IbmWing.IbmPower=this;
					}
					Limb.gameObject.GetComponent<Rigidbody2D>().mass*=100;
				}
				Limb.InitialHealth *= 8f;
				Limb.Health=Limb.InitialHealth;
			}

			IBMAI AI=IBM.gameObject.AddComponent<IBMAI>();
			AI.IbmPower=this;
			IBM.SetBloodColour(5,5,5);
			IBM.SetBruiseColor(5,5,5);
			IBM.SetSecondBruiseColor(5,5,5);
			IBM.SetThirdBruiseColor(5,5,5);
			Appearing=true;
			disappearProgress=0f;
		}

		IEnumerator Push(){
			yield return null;
			foreach(LimbBehaviour Limb in IBM.Limbs){
				Rigidbody2D rigidBody=Limb.gameObject.GetComponent<Rigidbody2D>();
				rigidBody.AddForce(this.gameObject.transform.rotation*(rigidBody.mass*LookingDirection*100f));
				if(Limb.name=="Head"){
					rigidBody.AddForce(rigidBody.mass*(Vector3.up)*2500f);
				}
				if(Limb.name.Contains("LowerArmFront")||Limb.name.Contains("LowerArm")){
					rigidBody.AddForce(this.gameObject.transform.rotation*(rigidBody.mass*LookingDirection*700f));
				}
			}
			UprightForce=6000f;
		}

		public LimbBehaviour MatchLimb(LimbBehaviour limb){
			foreach(LimbBehaviour Limb in Person.Limbs){
				if(Limb.name==limb.name){
					return Limb;
				}
			}
			return limb;
		}
	}

	public class IBMAI : MonoBehaviour{
		PersonBehaviour IBM;
		public IBMPower IbmPower;
		GameObject MiddleBody;
		public Dictionary<PersonBehaviour,Transform> TargetPosDictionary=new Dictionary<PersonBehaviour,Transform>();
		Global global;
		public float AttackCD;
		IBMWing[] Wing;
		TargetSelection targetselect;
		public Vector3 Destination;

		private void Awake() {
			global=FindObjectOfType<Global>();
			IBM=this.gameObject.GetComponent<PersonBehaviour>();
			foreach(LimbBehaviour limb in IBM.Limbs){
				if(limb.name=="MiddleBody"){
					MiddleBody=limb.gameObject;
				}
				if(limb.name=="Head"){
					targetselect=limb.gameObject.AddComponent<TargetSelection>();
				}
			}
			StartCoroutine("LateAwake");
		}

		IEnumerator LateAwake(){
			yield return null;
			if(IbmPower.IBMUser=="Takeshi"){
				Wing=GetComponentsInChildren<IBMWing>();
				foreach (IBMWing wing in Wing)
				{
					wing.Ai=this;
				}
			}

			targetselect.IbmAi=this;
			targetselect.ibmPower=IbmPower;
		}

		private void Update() {
			TargetPosDictionary=targetselect.TargetPosDictionary;
			AttackCD=Mathf.MoveTowards(AttackCD,0,Time.deltaTime);

			if(IbmPower.ReleaseIBM&&IBM.Consciousness>=0.8f&&TargetPosDictionary.Count!=0){
				Vector3 EnemyPosition=GetNearestEnemy();

				try{
					if(IbmPower.IBMUser=="Takeshi"){
						FlyTo(EnemyPosition);
					}
					else{
						WalkingTo(EnemyPosition);
					}
				}
				catch{}

				Attack(EnemyPosition);
			}
		}

		public Vector3 GetNearestEnemy(){
			Dictionary<float,Vector3> PositionDictionary=new Dictionary<float,Vector3>();
			if(TargetPosDictionary.Count==0){
				return MiddleBody.transform.position;
			}
			float MinDistance=Mathf.Infinity;
			foreach(PersonBehaviour key in TargetPosDictionary.Keys){
				Transform transform=TargetPosDictionary[key];
				Vector3 Position=transform.position;
				float SqrDistance=0;
				if(key.IsAlive()){
					SqrDistance=(Position-MiddleBody.transform.position).sqrMagnitude;
					if(SqrDistance<MinDistance){
						MinDistance=SqrDistance;
					}
				}
				PositionDictionary.Add(SqrDistance,Position);
			}
			return PositionDictionary[MinDistance];
		}

		public void WalkingTo(Vector3 TargetPosition){
			if(TargetPosition.x>MiddleBody.transform.position.x){
				IBM.DesiredWalkingDirection=1;
				AddForceToPerson(300f,Vector3.right);
			}
			else
			{
				IBM.DesiredWalkingDirection=-1;
				AddForceToPerson(300f,Vector3.left);
			}
			if(TargetPosition.y<MiddleBody.transform.position.y){
				IbmPower.UprightForce=Mathf.MoveTowards(IbmPower.UprightForce,0f,1500*Time.deltaTime);
			}
		}

		public void FlyTo(Vector3 TargetPosition){
			Vector3 TargetDirection=(TargetPosition-MiddleBody.transform.position).normalized;
			foreach(IBMWing ibmwing in Wing){
				ibmwing.FlyDirection=TargetDirection;
			}
			if(IbmPower.UprightForce<3000f){
				IbmPower.UprightForce=3000f;
			}
			if(IBM.IsTouchingFloor){
				IbmPower.UprightForce=24000f;
			}
			if(TargetPosition.y<MiddleBody.transform.position.y){
				IbmPower.UprightForce=Mathf.MoveTowards(IbmPower.UprightForce,0f,1500*Time.deltaTime);
			}
		}

		public void Attack(Vector3 TargetPosition){
			if(AttackCD!=0f){
				return;
			}
			Vector3 TargetDirection=(TargetPosition-MiddleBody.transform.position);
			if(TargetDirection.sqrMagnitude<15f){
				TargetDirection=TargetDirection.normalized;
				foreach(LimbBehaviour Limb in IBM.Limbs){
					Rigidbody2D rigidBody=Limb.gameObject.GetComponent<Rigidbody2D>();
					rigidBody.velocity=Vector3.zero;
					rigidBody.AddForce(this.gameObject.transform.rotation*(rigidBody.mass*TargetDirection*300f));
					if(Limb.name=="Head"){
						rigidBody.AddForce(rigidBody.mass*(Vector3.up)*2500f);
					}
					if(Limb.name.Contains("LowerArmFront")||Limb.name.Contains("LowerArm")){
						rigidBody.AddForce(this.gameObject.transform.rotation*(rigidBody.mass*TargetDirection*1000f));
					}
				}
				IbmPower.UprightForce=300f;
				AttackCD=0.5f;
			}
		}

		public void AddForceToPerson(float Magnitude,Vector3 Direction){
			foreach(LimbBehaviour Limb in IBM.Limbs){
				Rigidbody2D rigidBody=Limb.gameObject.GetComponent<Rigidbody2D>();
				if(Limb.name=="Head"){
					rigidBody.AddForce(rigidBody.mass*(Vector3.up)*2500f*Time.deltaTime);
				}
				rigidBody.AddForce(this.gameObject.transform.rotation*(rigidBody.mass*Direction*Magnitude*Time.deltaTime));
			}
		}
	}

	public class TargetSelection : MonoBehaviour{

		Global global;
		public Dictionary<PersonBehaviour,Transform> TargetPosDictionary=new Dictionary<PersonBehaviour,Transform>();
		public Dictionary<PersonBehaviour,LineRenderer> TargetLineDictionary=new Dictionary<PersonBehaviour,LineRenderer>();
		ToolControllerBehaviour toolcontrollerbehaviour;
		public IBMAI IbmAi;
		public IBMPower ibmPower;
		LineRenderer[] linerenderers;
		LineRenderer ChoosingLine;
		bool Choosing=false;


		private void Awake() {
			global=FindObjectOfType<Global>();
			toolcontrollerbehaviour=FindObjectOfType<ToolControllerBehaviour>();
			Global.main.LimbStatusToggled += Main_LimbStatusToggled;

			GameObject line=Instantiate(ModAssets.DashLinePrefab);
			ChoosingLine=line.GetComponent<LineRenderer>();
			ChoosingLine.enabled=false;

			this.GetComponent<PhysicalBehaviour>().ContextMenuOptions.Buttons.Add(new ContextMenuButton("ClearSelection", "Clear all enemy selection", "ClearSelection", () =>
            {
                foreach(LineRenderer lineRenderer in TargetLineDictionary.Values){
					Destroy(lineRenderer);
				}
				TargetLineDictionary.Clear();
				TargetPosDictionary.Clear();
            }));

			this.gameObject.AddComponent<UseEventTrigger>().Action = () => {
				if(Choosing){
					Choosing=false;
					ChoosingLine.enabled=false;
				}
				else{
					Choosing=true;
					ChoosingLine.enabled=true;
				}
			};

			ModAPI.OnDeath += (sender, being) => {
				try{
					if(being == ibmPower.Person){
						ibmPower.EnableCollisionWithPerson(ibmPower.Person, false);
					}
					DestroyLine(being);
				}
				catch{}
            };

			ModAPI.OnItemRemoved += (sender, args) => {
				try{
					if(args.Instance.TryGetComponent<PersonBehaviour>(out PersonBehaviour person)){
						DestroyLine(person);
					}
				}
				catch{}
            };
		}

		private void Main_LimbStatusToggled(object sender, bool e)
		{
			try{
				foreach(LineRenderer line in TargetLineDictionary.Values)
				{
					line.enabled=e;
				}
			}
			catch{}
		}

		private void Update() {
			try{
				if(Choosing){
					ChoosingLine.SetPositions(new Vector3[]{global.MousePosition,this.gameObject.transform.position});
					PhysicalBehaviour chosen=toolcontrollerbehaviour.CurrentTool.ActiveSingleSelected;
					if(chosen.gameObject.TryGetComponent<LimbBehaviour>(out LimbBehaviour limb)){
						if(limb.Person==ibmPower.IBM){
							return;
						}
						if(ibmPower.ReleaseIBM){
							ibmPower.EnableCollisionWithPerson(limb.Person, true);
						}
						TargetPosDictionary.Add(limb.Person,limb.gameObject.transform);
						AddLine(limb.Person);
					}
				}
			}
			catch{}
		}

		private void LateUpdate() {
			if(global.ShowLimbStatus){
				foreach(PersonBehaviour person in TargetPosDictionary.Keys){
					try{
						TargetLineDictionary[person].SetPositions(new Vector3[]{TargetPosDictionary[person].position,this.gameObject.transform.position});
					}
					catch{
						DestroyLine(person);
					}
				}
			}
		}
		private void AddLine(PersonBehaviour person){
			GameObject line=Instantiate(ModAssets.DashLinePrefab);
			LineRenderer linerenderer=line.GetComponent<LineRenderer>();
			TargetLineDictionary.Add(person,linerenderer);
			linerenderer.enabled=global.ShowLimbStatus;
		}

		private void DestroyLine(PersonBehaviour person){
			Destroy(TargetLineDictionary[person].gameObject);
			TargetLineDictionary.Remove(person);
			TargetPosDictionary.Remove(person);
		}

		private void OnDestroy() {
			foreach(LineRenderer line in TargetLineDictionary.Values){
				Destroy(line);
			}
			Destroy(ChoosingLine);
		}
	}

	public class IBMWing : MonoBehaviour{
		public IBMAI Ai;
		public Sprite IBMWingTex;
		public IBMPower IbmPower;
		public Vector3 FlyDirection;
		Rigidbody2D rigidbody;
		private void Awake() {
			rigidbody=this.gameObject.GetComponent<Rigidbody2D>();
			StartCoroutine("Wing");
		}

		IEnumerator Wing(){
			yield return null;
			this.gameObject.GetComponent<SpriteRenderer>().sprite=IBMWingTex;
		}

		private void Update(){
			IbmPower.UprightForce=Mathf.MoveTowards(IbmPower.UprightForce,7000f,Time.deltaTime*4000f);
			if(Ai.TargetPosDictionary.Count!=0){
				if(FlyDirection.sqrMagnitude>1f){
					if(rigidbody.velocity.sqrMagnitude<50f){
						rigidbody.AddForce(FlyDirection*rigidbody.mass*5000*Time.deltaTime);
					}
				}
				rigidbody.velocity=Vector3.RotateTowards(rigidbody.velocity,FlyDirection*rigidbody.velocity.magnitude,Time.deltaTime*60,Time.deltaTime*5f);
			}
			else
			{
				IbmPower.UprightForce=Mathf.MoveTowards(IbmPower.UprightForce,7000f,Time.deltaTime*5500f);
			}
		}
	}

	public class IBMClaw : MonoBehaviour{
		Rigidbody2D rigbody;
		private void Awake() {
			rigbody=GetComponent<Rigidbody2D>();
		}
		private void OnCollisionEnter2D(Collision2D other) {
			if(other.gameObject.TryGetComponent<LimbBehaviour>(out LimbBehaviour limb)){
				float RelativeSpeed=(other.gameObject.GetComponent<Rigidbody2D>().velocity-rigbody.velocity).sqrMagnitude;
				if(other.gameObject.TryGetComponent<IBMLimb>(out IBMLimb Ibmlimb)){
					this.GetComponent<LimbBehaviour>().Health-=this.GetComponent<LimbBehaviour>().InitialHealth*0.01f;
					other.gameObject.GetComponent<LimbBehaviour>().Health-=this.GetComponent<LimbBehaviour>().InitialHealth*0.1f;
					return;
				}
				if(RelativeSpeed>2.5){
					limb.Slice();
				}
			}
		}
	}

	public class IBMLimb : LimbRegrowth, Messages.IShot
	{
		public IBMPower BlackGhost;

		private void Awake() {
			limb = GetComponent<LimbBehaviour>();
			if(limb.Joint){
				limb.Joint.autoConfigureConnectedAnchor = false;
			}

			StartCoroutine("LateAwake");
		}

		IEnumerator LateAwake()
        {
            yield return 0;

            StopEmission();
            ConnectLimbs.Clear();

            limb.PhysicalBehaviour.SpawnSpawnParticles = false;
            SetupIbmParticle();

            GetComponent<Rigidbody2D>().collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            if(limb.name == "MiddleBody"){
                RecordCurrentJointData();
            }
        }

		private void Update() {
			Revive();
			try{
				IbmEmission.rateOverTimeMultiplier = Mathf.MoveTowards(IbmEmission.rateOverTimeMultiplier, 15, Time.deltaTime*100);
			}
			catch{}
		}

		public void Shot(Shot shot)
        {
            IbmEmission.rateOverTimeMultiplier = 300f;
        }

		public void SetupIbmParticle()
        {
            IBMParticle = Instantiate(
                ModAssets.IBMParticlePrefab,
                base.transform.position,
                Quaternion.identity,
                base.transform
            );

			ParticleSystem.MainModule main = IBMParticle.GetComponent<ParticleSystem>().main;
			main.simulationSpeed = 0.5f;
			main.startLifetimeMultiplier *= 0.5f;

            ParticleSystem.ShapeModule shape = IBMParticle.GetComponent<ParticleSystem>().shape;
            shape.spriteRenderer = GetComponent<SpriteRenderer>();
            Bounds bound = GetComponent<Collider2D>().bounds;
            shape.scale = bound.size;
            shape.rotation = new Vector3(90f, 0, 0);
            IbmEmission = IBMParticle.GetComponent<ParticleSystem>().emission;
            IBMParticle.AddComponent<Optout>();
        }


		private void OnCollisionEnter2D(Collision2D other) {
			if(other.gameObject.TryGetComponent<LimbBehaviour>(out LimbBehaviour OtherLimb)&&!OtherLimb.Person.IsAlive()){
				foreach (LimbBehaviour EachLimb in OtherLimb.Person.Limbs)
				{
					Physics2D.IgnoreCollision(limb.gameObject.GetComponent<Collider2D>(), EachLimb.gameObject.GetComponent<Collider2D>(), ignore: true);
				}
			}
		}

		public void Revive(){
			limb.IsAndroid = true;
			limb.CirculationBehaviour.ImmuneToDamage = true;
			limb.CirculationBehaviour.HealBleeding();
			limb.CirculationBehaviour.BloodFlow=1f;
			limb.CirculationBehaviour.WasInitiallyPumping=false;
			limb.PhysicalBehaviour.BurnIntensity=0f;
			limb.Wince(1f);

			limb.HealBone();
			limb.IsZombie=false;
			limb.CirculationBehaviour.HealBleeding();
			limb.CirculationBehaviour.IsDisconnected=false;
			limb.PhysicalBehaviour.charge=0f;

			try{
				limb.CirculationBehaviour.ClearLiquid();
				limb.CirculationBehaviour.AddLiquid(limb.GetOriginalBloodType(), 1f);
			}
			catch{}
			
			if (limb.SkinMaterialHandler.RottenProgress>0f){
				limb.SkinMaterialHandler.RottenProgress-=0.5f*Time.deltaTime;
			}
			limb.Vitality=1f;
			limb.Numbness=0f;

			limb.CirculationBehaviour.IsPump=limb.CirculationBehaviour.WasInitiallyPumping;

			limb.BruiseCount = 0;
			limb.PhysicalBehaviour.Extinguish();
			limb.CirculationBehaviour.BleedingPointCount = 0;
			limb.CirculationBehaviour.StabWoundCount = 0;
			limb.CirculationBehaviour.GunshotWoundCount = 0;
			limb.HasLungs = false;
			limb.VitalParts = new Bounds[]{new Bounds(new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f))};
			limb.BreakingThreshold = Mathf.Infinity;
			limb.RoughClassification = LimbBehaviour.BodyPart.Torso;
		}
	}
}
// Originally uploaded by 'NeitherFishNorCat'. Do not reupload without their explicit permission.
