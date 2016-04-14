using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;

namespace RogoDigital.Lipsync{
	[ExecuteInEditMode]
	public class BlendSystem : MonoBehaviour{

		// BlendSystem GUI information
		public string blendableDisplayName = "Blendable";
		public string blendableDisplayNamePlural = "Blendables";
		public string noBlendablesMessage = "No Blendables found.";
		public string notReadyMessage = "Setup incomplete.";
		public float blendRangeLow = 0;
		public float blendRangeHigh = 100;

		[Obsolete("Reference BlendSystems are no longer supported as of 0.6. They should be replaced by standard blend systems that work with a custom component to report blendables.")]
		public bool useReferences = false;

		/// <summary>
		/// Is the Blend System ready to use?
		/// </summary>
		public bool isReady = false;

		/// <summary>
		/// The LipSync component using this BlendSystem.
		/// </summary>
		private BlendSystemUser[] users;

		/// <summary>
		/// Gets the number of blendables associated with this Blend System.
		/// </summary>
		/// <value>The blendable count.</value>
		public int blendableCount{
			get {
				if(_blendables == null) _blendables = new List<Blendable>();
				return _blendables.Count;
			}
		}
			
		private List<Blendable> _blendables;

		public virtual void OnEnable () {
			this.hideFlags = HideFlags.HideInInspector;
			users = GetComponents<BlendSystemUser>();
			if(users == null){
				if(Application.isPlaying){
					Destroy(this);
				}else{
					DestroyImmediate(this);
				}
			}
			OnVariableChanged();
			GetBlendables();
		}

		/// <summary>
		/// Sets the value of a blendable.
		/// </summary>
		/// <param name="blendable">Blendable.</param>
		/// <param name="value">Value.</param>
		public virtual void SetBlendableValue (int blendable , float value) {
		}

		/// <summary>
		/// Gets the value of a blendable.
		/// </summary>
		/// <returns>The blendable value.</returns>
		/// <param name="blendable">Blendable.</param>
		public float GetBlendableValue (int blendable) {
			if(_blendables == null) _blendables = new List<Blendable>();
			return _blendables[blendable].currentWeight;
		}

		/// <summary>
		/// Called when a BlendSystem variable is changed in the LipSync editor.
		/// </summary>
		public virtual void OnVariableChanged () {
		}

		/// <summary>
		/// Gets the blendables associated with this Blend System.
		/// </summary>
		/// <returns>The blendables.</returns>
		public virtual string[] GetBlendables () {
			return null;
		}

		// Internal blendable list methods
		public void AddBlendable (int blendable , float currentValue) {
			if(_blendables == null) _blendables = new List<Blendable>();
			_blendables.Insert(blendable , new Blendable(blendable , currentValue));
		}

		public void ClearBlendables () {
			_blendables = new List<Blendable>();
		}

		public void SetInternalValue (int blendable , float value) {
			if(_blendables == null){
				_blendables = new List<Blendable>();
				GetBlendables();
			}
			_blendables[blendable].currentWeight = value;
		}
	}
}
