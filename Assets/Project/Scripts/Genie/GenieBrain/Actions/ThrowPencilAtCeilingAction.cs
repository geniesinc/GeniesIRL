using GeniesIRL;
using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "ThrowPencilAtCeiling", story: "[Genie] throws pencil at ceiling", category: "Action/GeniesIRL", id: "4e31c3e97d3fb5f57bbe972313f47a54")]
public partial class ThrowPencilAtCeilingAction : Action
{
    [SerializeReference] public BlackboardVariable<Genie> Genie;
    [SerializeReference] public BlackboardVariable<GameObject> PencilPrefab;
    [SerializeReference] public BlackboardVariable<AudioClip> PencilThrowWoosh;
    [SerializeReference] public BlackboardVariable<float> PencilThrowWooshVolume = new BlackboardVariable<float>(1f);

    private Pencil _pencil;
    private Genie _genie => Genie.Value;

    private float _animEndTime;

    protected override Status OnStart()
    {
        _pencil = GameObject.Instantiate(PencilPrefab.Value).GetComponent<Pencil>();
        _genie.genieGrabber.InstantGrabAndTeleportToHand(GenieHand.Right, _pencil.Item);
        _genie.genieAudio.animEventDispatcher.OnPencilThrownAnimEvent += OnPencilThrownAnimEvent;
        AnimationClip clip = _genie.genieAnimation.Animator.SetTriggerAndReturnClip("ThrowPencilAtCeiling");
        _animEndTime = Time.time + clip.length;

        return Status.Running;
    }

    private void OnPencilThrownAnimEvent()
    {
        _genie.genieAudio.PlayGeneralSingleShotSound(PencilThrowWoosh.Value, PencilThrowWooshVolume.Value);
        _genie.genieGrabber.InstantReleaseHeldItem();
        _pencil.Launch();
    }

    protected override Status OnUpdate()
    {
        if (Time.time >= _animEndTime)
        {
            return Status.Success;
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        _genie.genieAudio.animEventDispatcher.OnPencilThrownAnimEvent -= OnPencilThrownAnimEvent;
        _genie.genieAudio.singleShotGeneral.Stop();

        if (!CurrentStatus.IsCompleted()) // If the action was ended prematurely and the pencil hasn't been thrown yet, destroy it.
        {
            if (!_pencil.IsLaunched)
            {
                GameObject.Destroy(_pencil.gameObject);
            }
        }
    }
}

