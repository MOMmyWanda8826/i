
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mod
{
	public class ParalyzingScream : MonoBehaviour
	{
		float ShakeIntensity=0f;
		private void Awake() {
			this.gameObject.AddComponent<UseEventTrigger>().Action = () =>
			{
				if(this.gameObject.GetComponent<LimbBehaviour>().Person.Consciousness>=0.5f){
					ShakeIntensity=300f;
					Collider2D[] colls=Physics2D.OverlapCircleAll(this.gameObject.transform.position,6f,LayerMask.GetMask("Objects"));
					foreach(Collider2D coll in colls){
						if(coll.gameObject.TryGetComponent<LimbBehaviour>(out LimbBehaviour limb)){
							if(!coll.gameObject.HasComponent<LimbRegrowth>()&&limb.Person.IsAlive()){
								limb.gameObject.AddComponent<Paralyzed>();
								limb.gameObject.GetComponent<Rigidbody2D>().AddForce((this.gameObject.transform.position-limb.gameObject.transform.position).normalized*-60f);
							}
						}
					}
				}
			};
		}
		private void Update() {
			ShakeIntensity=Mathf.MoveTowards(ShakeIntensity,0f,Time.deltaTime*600f);
			CameraShakeBehaviour.main.Shake(ShakeIntensity, base.transform.position, 0.2f);
		}
	}
	public class Paralyzed : MonoBehaviour
	{
		LimbBehaviour limb;
		private void Awake() {
			limb=GetComponent<LimbBehaviour>();
			if(!limb.Frozen){
				StartCoroutine("Recover");
			}
			else{
				Destroy(this);
			}
		}
		private void Update() {
			limb.Frozen=true;
		}
		IEnumerator Recover(){
			yield return new WaitForSeconds(12f);
			limb.Frozen=false;
			Destroy(this);
		}
	}
}
// Originally uploaded by 'NeitherFishNorCat'. Do not reupload without their explicit permission.
