using UnityEngine;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.UI;
using System;
using System.Collections.Generic;
using System.Collections;

namespace GeniesIRL
{
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(LazyFollow))]
    public class GeniesPrespawnUI : MonoBehaviour
    {
        [SerializeField] private float deathDuration = 0.5f;
        [SerializeField] private TextMeshProUGUI text;
        private Animator _animator;
        private LazyFollow _lazyFollow;
        private CanvasGroup _canvasGroup;

        /// <summary>
        /// Called by the UIManager who spawned this object. 
        /// </summary>
        /// <param name="genieManagerState"></param>
        public void OnSpawned(GenieManager.GenieManagerState genieManagerState)
        {
            // Here you can set up the UI to reflect whether the Genie Manager is trying to Spawn
            // or Teleport a Genie.
        }

        /// <summary>
        /// 'Kills' the UI by making it fade down and delete itself.
        /// </summary>
        public void Die()
        {
            StartCoroutine(Die_C());
        }

        private void Awake()
        {
            _animator = GetComponent<Animator>();

            _lazyFollow = GetComponent<LazyFollow>();
            _lazyFollow.target = Camera.main.transform;
            _lazyFollow.enabled = true;

            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.alpha = 0;

            StartCoroutine(FadeTo_C(1f));
        }

        private IEnumerator Die_C()
        {
            yield return StartCoroutine(FadeTo_C(0));

            Destroy(this.gameObject);
            
        }

        private IEnumerator FadeTo_C(float destAlpha)
        {
            float startTime = Time.time;
            float endTime = startTime + deathDuration;

            float startAlpha = _canvasGroup.alpha;

            while (Time.time < endTime)
            {
                float t = Mathf.InverseLerp(startTime, endTime, Time.time);

                _canvasGroup.alpha = Mathf.Lerp(startAlpha, destAlpha, t);
                yield return null;
            }
        }
    }
}
