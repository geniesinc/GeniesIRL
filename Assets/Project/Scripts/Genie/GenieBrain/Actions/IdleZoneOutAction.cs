using GeniesIRL;
using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using System.Collections.Generic;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "IdleZoneOut", story: "[Genie] zones out", category: "Action/GeniesIRL", id: "f2be89acae51bce546996f4af16406f4")]
public partial class IdleZoneOutAction : Action
{
    [SerializeReference] public BlackboardVariable<Genie> Genie;

    // We unfortunately can't serialize lists, nor can we serialize custom classes, so we have to explicitly declare each animKey and clip.
    [Tooltip("Each randomly-selected key corresponds to an animation trigger in the Genie's Animator. The Animation Clip MUST have the same name as the trigger!")]
    [SerializeReference] public BlackboardVariable<string> zoneOut1Key = new BlackboardVariable<string>("ZoneOut-LookAtNails");
    [SerializeReference] public BlackboardVariable<float> zoneOut1Duration = new BlackboardVariable<float>(7.967f);
    [SerializeReference] public BlackboardVariable<AudioClip> zoneOut1AudioClip = new BlackboardVariable<AudioClip>();
    [SerializeReference] public BlackboardVariable<float> zoneOut1AudioVolume = new BlackboardVariable<float>(1f);
    [Tooltip("If true, the zoneOut will be ignored for debugging purposes. This allows you to isolate the specific ZoneOuts you are testing.")]
    [SerializeReference] public BlackboardVariable<bool> zoneOut1DebugIgnore = new BlackboardVariable<bool>(false);

    [Tooltip("Each randomly-selected key corresponds to an animation trigger in the Genie's Animator. The Animation Clip MUST have the same name as the trigger!")]
    [SerializeReference] public BlackboardVariable<string> zoneOut2Key = new BlackboardVariable<string>("ZoneOut-UseSmartphone");
    [SerializeReference] public BlackboardVariable<float> zoneOut2Duration = new BlackboardVariable<float>(11.763f);
    [SerializeReference] public BlackboardVariable<AudioClip> zoneOut2AudioClip = new BlackboardVariable<AudioClip>();
    [SerializeReference] public BlackboardVariable<float> zoneOut2AudioVolume = new BlackboardVariable<float>(1f);
    [Tooltip("If true, the zoneOut will be ignored for debugging purposes. This allows you to isolate the specific ZoneOuts you are testing.")]
    [SerializeReference] public BlackboardVariable<bool> zoneOut2DebugIgnore = new BlackboardVariable<bool>(false);

    [Tooltip("Each randomly-selected key corresponds to an animation trigger in the Genie's Animator. The Animation Clip MUST have the same name as the trigger!")]
    [SerializeReference] public BlackboardVariable<string> zoneOut3Key = new BlackboardVariable<string>("IdleZoneOut3");
    [SerializeReference] public BlackboardVariable<float> zoneOut3Duration = new BlackboardVariable<float>(5f);
    [SerializeReference] public BlackboardVariable<AudioClip> zoneOut3AudioClip = new BlackboardVariable<AudioClip>();
    [SerializeReference] public BlackboardVariable<float> zoneOut3AudioVolume = new BlackboardVariable<float>(1f);
    [Tooltip("If true, the zoneOut will be ignored for debugging purposes. This allows you to isolate the specific ZoneOuts you are testing.")]
    [SerializeReference] public BlackboardVariable<bool> zoneOut3DebugIgnore = new BlackboardVariable<bool>(false);

    private Animator _animator => Genie.Value.genieAnimation.Animator;
    private float _endTime;
    private int _lastUsedIndex = -1;

    private List<ZoneOut> _zoneOuts = null;

    private struct ZoneOut
    {
        public string key;
        public float duration;
        public AudioClip audioClip;
        public float audioVolume;
    }

    protected override Status OnStart()
    {
        // Future Note: Instead of getting animation clip, you might instead want to check to see whether the animation state is playing.
        // (You'll have to know the animation state name.)
        Debug.Log("IdleZoneOutAction.OnStart");

        // Add all the keys to a list so we can randomly select one.
        if (_zoneOuts == null) 
        {
            _zoneOuts = InitializeZoneOuts();
        }

        // Select a key to play.
        int _newIndex = SelectNextIndex();

        ZoneOut selectedZoneOut = _zoneOuts[_newIndex];

        _animator.SetTrigger(selectedZoneOut.key);

        PlayAudio(selectedZoneOut);

        _endTime = Time.time + selectedZoneOut.duration;

        _lastUsedIndex = _newIndex; // Update the last used index so we don't use it next time.

        Genie.Value.genieLookAndYaw.eyeballAimer.StopTrackingTarget();
        
        return Status.Running;
    }

    private List<ZoneOut> InitializeZoneOuts()
    {
        List<ZoneOut> zoneOuts = new List<ZoneOut>();
        
        if (!String.IsNullOrEmpty(zoneOut1Key.Value) && !zoneOut1DebugIgnore.Value)
        {
            zoneOuts.Add(
                new ZoneOut() 
                {
                    key = zoneOut1Key.Value,
                    duration = zoneOut1Duration.Value,
                    audioClip = zoneOut1AudioClip.Value,
                    audioVolume = zoneOut1AudioVolume.Value,
                }
            );
        }
        
        if (!String.IsNullOrEmpty(zoneOut2Key.Value) && !zoneOut2DebugIgnore.Value)
        {
            zoneOuts.Add(
                new ZoneOut() 
                {
                    key = zoneOut2Key.Value,
                    duration = zoneOut2Duration.Value,
                    audioClip = zoneOut2AudioClip.Value,
                    audioVolume = zoneOut2AudioVolume.Value,
                }
            );
        }

        if (!String.IsNullOrEmpty(zoneOut3Key.Value) && !zoneOut3DebugIgnore.Value)
        {
            zoneOuts.Add(
                new ZoneOut() 
                {
                    key = zoneOut3Key.Value,
                    duration = zoneOut3Duration.Value,
                    audioClip = zoneOut3AudioClip.Value,
                    audioVolume = zoneOut3AudioVolume.Value,
                }
            );
        }

        return zoneOuts;
    }

    private void PlayAudio(ZoneOut zoneOut)
    {
        AudioClip clip = zoneOut.audioClip;
        float volume = zoneOut.audioVolume;

        if (clip != null)
        {
            Genie.Value.genieAudio.PlayGeneralSingleShotSound(clip, volume);
        }
    }

    private int SelectNextIndex()
    {
        if (_lastUsedIndex == -1 || _zoneOuts.Count == 1)
        {
            _lastUsedIndex = 0;
            return UnityEngine.Random.Range(0, _zoneOuts.Count);
        }

        // Select a new key that is different from the last one used.
        List<int> availableIndices = new List<int>();
        for (int i = 0; i < _zoneOuts.Count; i++)
        {
            if (i != _lastUsedIndex)
            {
                availableIndices.Add(i);
            }
        }

        int randomIndex = UnityEngine.Random.Range(0, availableIndices.Count);

        return availableIndices[randomIndex];
    }

    protected override Status OnUpdate()
    {
        if (Time.time >= _endTime)
        {
            return Status.Success;
        }

        return Status.Running;
    }

    // This is important because it allows us to tie up any loose ends if the Action is externally interrupted.
    protected override void OnEnd()
    {
         Debug.Log("IdleZoneOutAction.OnEnd");

         // Stop any audio playing
        Genie.Value.genieAudio.singleShotGeneral.Stop();
        
        // Disappear any props that might've gotten spawned.
        Genie.Value.genieEphemeralProps.DisableAllProps();
    }
}

