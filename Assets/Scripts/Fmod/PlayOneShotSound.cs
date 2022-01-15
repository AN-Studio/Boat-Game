using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

public class PlayOneShotSound : MonoBehaviour 
{
    #region Settings
        [Header("Settings")]
        [SerializeField] EmitterGameEvent playEventOn;
        [FMODUnity.EventRef] [SerializeField] string soundEvent;
        [SerializeField] float delayDuration = 0;
        [SerializeField] bool isGlobalSFX = false;
        public StudioParameterTrigger trigger;
    #endregion

    #region Variables
        Transform cam;
        FMOD.Studio.EventInstance instance;
    #endregion

    #region MonoBehaviour Functions

        #region Collisions & Triggers
            private void OnCollisionEnter(Collision other) {
                if (playEventOn == EmitterGameEvent.CollisionEnter) 
                {    
                    StartCoroutine(PlayOneShot());
                }
            }
            private void OnCollisionEnter2D(Collision2D other) {
                if (playEventOn == EmitterGameEvent.CollisionEnter2D) 
                {    
                    StartCoroutine(PlayOneShot());
                }
            }
            private void OnCollisionExit(Collision other) {
                if (playEventOn == EmitterGameEvent.CollisionExit) 
                {    
                    StartCoroutine(PlayOneShot());
                }
            }
            private void OnCollisionExit2D(Collision2D other) {
                if (playEventOn == EmitterGameEvent.CollisionExit2D) 
                {    
                    StartCoroutine(PlayOneShot());
                }
            }
            private void OnTriggerEnter(Collider other) {
                if (playEventOn == EmitterGameEvent.TriggerEnter) 
                {    
                    StartCoroutine(PlayOneShot());
                }
            }
            private void OnTriggerEnter2D(Collider2D other) {
                if (playEventOn == EmitterGameEvent.TriggerEnter2D) 
                {    
                    StartCoroutine(PlayOneShot());
                }
            }
            private void OnTriggerExit(Collider other) {
                if (playEventOn == EmitterGameEvent.TriggerExit) 
                {    
                    StartCoroutine(PlayOneShot());
                }
            }
            private void OnTriggerExit2D(Collider2D other) {
                if (playEventOn == EmitterGameEvent.TriggerExit2D) 
                {    
                    StartCoroutine(PlayOneShot());
                }
            }
            private void OnMouseEnter() {
                if (playEventOn == EmitterGameEvent.MouseEnter) 
                {    
                    StartCoroutine(PlayOneShot());
                }
            }
            private void OnMouseExit() {
                if (playEventOn == EmitterGameEvent.MouseExit) 
                {    
                    StartCoroutine(PlayOneShot());
                }
            }
            private void OnMouseDown() {
                if (playEventOn == EmitterGameEvent.MouseDown) 
                {    
                    StartCoroutine(PlayOneShot());
                }
            }
            private void OnMouseUp() {
                if (playEventOn == EmitterGameEvent.MouseUp) 
                {    
                    StartCoroutine(PlayOneShot());
                }
            }
        #endregion

        private void Awake() {
            cam = Camera.main.transform;
        }

        private void Start() {
            if (playEventOn == EmitterGameEvent.ObjectStart) 
            {    
                StartCoroutine(PlayOneShot());
            }
        }

        private void OnDestroy() {
            if (playEventOn == EmitterGameEvent.ObjectDestroy) 
            {    
                StartCoroutine(PlayOneShot());
            }
        }

        private void OnEnable() {
            if (playEventOn == EmitterGameEvent.ObjectEnable) 
            {    
                StartCoroutine(PlayOneShot());
            }
        }

        private void OnDisable() {
            if (playEventOn == EmitterGameEvent.ObjectDisable) 
            {    
                StartCoroutine(PlayOneShot());
            }
        }

    #endregion

    IEnumerator PlayOneShot()
    {
        yield return new WaitForSeconds(delayDuration);

        instance = RuntimeManager.CreateInstance(soundEvent);
        instance.start();

        FMOD.ATTRIBUTES_3D attributes3D = new FMOD.ATTRIBUTES_3D();
        
        FMOD.VECTOR position = new FMOD.VECTOR();
        position.x = transform.position.x;
        position.y = transform.position.y;
        position.z = transform.position.z;

        attributes3D.position = position;

        instance.set3DAttributes(attributes3D);

        instance.release();
    }
}