using GeniesIRL;
using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "DrawOnWall", story: "[Genie] draws on wall at [DrawingSpace]", category: "Action/GeniesIRL", id: "078aca63cd58138bd8badece193b4a5a")]
public partial class DrawOnWallAction : Action
{
    [SerializeReference] public BlackboardVariable<Genie> Genie;
    [SerializeReference] public BlackboardVariable<DrawingSpace> DrawingSpace;

    private Genie _genie => Genie.Value;
    private DrawingSpace _drawingSpace => DrawingSpace.Value;
    private bool _isFinished = false;

    protected override Status OnStart()
    {
        _isFinished = false;
        _genie.genieDraw.Draw(_drawingSpace.DrawingPose, () => _isFinished = true);

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (_isFinished)
        {
            return Status.Success;
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (!CurrentStatus.IsCompleted())
        {
            // Action was externally cancelled, so we need to hault the drawing operation.
            _genie.genieDraw.ExternallyCancelDraw();
        }
    }
}

