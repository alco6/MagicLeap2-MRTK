using System;
using System.Collections;
using System.Collections.Generic;
using MagicLeap.MRTK.Utilities;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

namespace MagicLeap.MRTK.DeviceManagement.Input
{
    public class MagicLeapJointSmoother
    {
        private Dictionary<TrackedHandJoint, PoseFilter> _progressByHandJoint =
            new Dictionary<TrackedHandJoint, PoseFilter>();

        //An array of bones that are supported by the Magic Leap. 4 fingers each
        private readonly TrackedHandJoint[] _handJoints = new TrackedHandJoint[]
        {
            TrackedHandJoint.ThumbTip, TrackedHandJoint.ThumbDistalJoint, TrackedHandJoint.ThumbProximalJoint,TrackedHandJoint.ThumbMetacarpalJoint,
            TrackedHandJoint.IndexTip, TrackedHandJoint.IndexDistalJoint, TrackedHandJoint.IndexMiddleJoint,TrackedHandJoint.IndexKnuckle,
            TrackedHandJoint.MiddleTip, TrackedHandJoint.MiddleDistalJoint, TrackedHandJoint.MiddleMiddleJoint,TrackedHandJoint.MiddleKnuckle,
            TrackedHandJoint.RingTip, TrackedHandJoint.RingDistalJoint, TrackedHandJoint.RingMiddleJoint,TrackedHandJoint.RingKnuckle,
            TrackedHandJoint.PinkyTip, TrackedHandJoint.PinkyDistalJoint, TrackedHandJoint.PinkyMiddleJoint,TrackedHandJoint.PinkyKnuckle,
            TrackedHandJoint.Palm, TrackedHandJoint.Wrist
        };

        private class PoseFilter
        {
            //Super smooth filter
            //private EuroFilter _positionDataFilter = new EuroFilter(3, .5f, 0.6f, 0.5f, false);

            //Kinda smooth filter
            private EuroFilter _positionDataFilter = new EuroFilter(3, .9f, 0.8f, 0.2f, false);

            private EuroFilter _rotationDataFilter  = new EuroFilter(4, 1, 0.3f, 0.6f, false);

            private KeyPointMotionFilter _keyPointMotionFilter = new KeyPointMotionFilter(0.5f, 0.005f, 0.005f, 8, 10);

            private SimpleSmoother _simpleSmoother = new SimpleSmoother();

            public MixedRealityPose FilterPose(MixedRealityPose pose, double time, MagicLeapHandTrackingInputProfile.SmoothingType type, bool updateRotation = false)
            {
                var jointPose = pose;
                switch (type)
                {
                    case MagicLeapHandTrackingInputProfile.SmoothingType.None:
                        break;
                    case MagicLeapHandTrackingInputProfile.SmoothingType.Simple:

                        jointPose.Position = _simpleSmoother.UpdatePosition(jointPose.Position);
                        if (updateRotation)
                            jointPose.Rotation = _simpleSmoother.UpdateRotation(jointPose.Rotation);

                        break;
                    case MagicLeapHandTrackingInputProfile.SmoothingType.Robust:

                        jointPose.Position = _keyPointMotionFilter.Filter(jointPose.Position);
                        if (updateRotation)
                            jointPose.Rotation = _rotationDataFilter.Filter(time, jointPose.Rotation);

                        break;
                }
                return jointPose;
            }

            public void Reset()
            {
               _positionDataFilter.Reset();
               _simpleSmoother.Reset();
            }

        }


        public void SmoothJoints(ref Dictionary<TrackedHandJoint, MixedRealityPose> handPoses, MagicLeapHandTrackingInputProfile.SmoothingType type)
        {
            //Used for euro filter
            double time = Time.timeAsDouble;
            foreach (var key in _handJoints)
            {
                if (handPoses.ContainsKey(key))
                {
                    if (!_progressByHandJoint.ContainsKey(key))
                    {
                        _progressByHandJoint.Add(key, new PoseFilter());
                    }
                    handPoses[key] = _progressByHandJoint[key].FilterPose(handPoses[key], time, type, true);
                }
            }
        }
        
        public MixedRealityPose SmoothJoint(TrackedHandJoint joint, MixedRealityPose pose, MagicLeapHandTrackingInputProfile.SmoothingType type, bool updateRotation)
        {

            if (!_progressByHandJoint.ContainsKey(joint))
            {
                _progressByHandJoint.Add(joint, new PoseFilter());
            }

            return _progressByHandJoint[joint].FilterPose(pose, Time.timeAsDouble, type, updateRotation);
        }

        public void Reset()
        {
           foreach (var progress in _progressByHandJoint.Values)
           {
               progress.Reset();
           }
        }
    }
}