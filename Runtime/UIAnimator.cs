using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace CryII.I.UIFramework
{
    [RequireComponent(typeof(PlayableDirector))]
    public class UIAnimator : MonoBehaviour
    {
        public List<PlayableAsset> timelines = new();

        private readonly Dictionary<string, int> _dctName2Anim = new();
        private PlayableDirector _playableDirector;
        private Action _playStopCallback;

        private void Awake()
        {
            _playableDirector = GetComponent<PlayableDirector>();
            _playableDirector.stopped += OnPlayableStop;
            
            for (int i = 0, timelineCnt = timelines.Count; i < timelineCnt; ++i)
            {
                var timeline = timelines[i];
                _dctName2Anim.Add(timeline.name, i);
            }
        }

        public void PlayAnim(string animName)
        {
            if (!_dctName2Anim.TryGetValue(animName, out var index))
            {
                return;
            }

            if (index >= timelines.Count)
            {
                return;
            }
            
            var timeline = timelines[index];
            _playableDirector.playableAsset = timeline;
            _playableDirector.Play();
        }

        public void PlayAnim(string animName, Action callback)
        {
            if (!_dctName2Anim.TryGetValue(animName, out var index))
            {
                return;
            }

            if (index >= timelines.Count)
            {
                return;
            }
            
            var timeline = timelines[index];
            _playableDirector.playableAsset = timeline;
            _playableDirector.Play();

            _playStopCallback = callback;
        }

        private void OnPlayableStop(PlayableDirector director)
        {
            if (_playStopCallback != null)
            {
                _playStopCallback();
            }
        }

        private void OnDestroy()
        {
            _playStopCallback = null;
            _playableDirector.stopped -= OnPlayableStop;
        }
    }
}