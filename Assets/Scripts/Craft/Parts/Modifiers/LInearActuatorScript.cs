namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using Assets.Scripts;
    using ModApi;
    using ModApi.Craft;
    using ModApi.Craft.Parts;
    using ModApi.Craft.Parts.Input;
    using ModApi.Design;
    using ModApi.GameLoop;
    using ModApi.GameLoop.Interfaces;
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public class LInearActuatorScript : PartModifierScript<LInearActuatorData>, IDesignerUpdate, IGameLoopItem, IFlightStart, IFlightUpdate, IFlightFixedUpdate
    {
        private float _priorLength;

        private bool _updatePistonShaft;

        private AudioSource _audio;

        private IBodyJoint _bodyJoint;

        private Transform _expectedJointPosition;

        private bool _initializationComplete;

        private IInputController _input;

        private ConfigurableJoint _joint;

        private Rigidbody _jointRigidbody;

        private Transform Extender1;
        private Transform Extender2;
        private Transform Extender3;

        private bool _moving;

        private float _pitch;

        private float _volume;

        void IDesignerUpdate.DesignerUpdate(in DesignerFrameData frame)
        {
            if (_initializationComplete)
            {
                float currentPosition = 0f;
                base.Data.CurrentPosition = currentPosition;
                UpdateShaftExtension();
            }
        }

        void IFlightFixedUpdate.FlightFixedUpdate(in FlightFrameData frame)
        {
            if (!_initializationComplete) { return; }
            if (base.PartScript.CommandPod == null || _input == null) { return; }

            Data.CurrentPosition = _joint.transform.localPosition.x - 0.5f;

            if (_joint != null && !_bodyJoint.PartConnection.IsDestroyed)
            {
                float currentVelocity = (Data.CurrentPosition - _priorLength) / Time.deltaTime;

                _joint.connectedBody.WakeUp();
                _jointRigidbody.WakeUp();

                float targetVelocity = Data.Velocity * _input.Value;
                float nextVelocity = currentVelocity + Data.Acceleration * Time.deltaTime;
                nextVelocity = Mathf.Clamp(nextVelocity, -1 * Math.Abs(targetVelocity), Math.Abs(targetVelocity));
                float nextLength = Data.CurrentPosition + nextVelocity * Time.deltaTime;
                nextLength = Mathf.Clamp(nextLength, 0f, Data.Length);

                Data.CurrentPosition = nextLength;
            }

            Vector3 targetposition = new Vector3(Data.CurrentPosition + 0.5f, 0f, 0f);
            _joint.targetPosition = targetposition;
            if (_updatePistonShaft) { UpdateShaftExtension(); }
        }

        void IFlightStart.FlightStart(in FlightFrameData frame)
        {
            _audio = base.PartScript.GameObject.GetComponent<AudioSource>();
            _input = GetInputController("Velocity");
            FindAndSetupConnectionJoint();
            _initializationComplete = true;
            UpdateShaftExtension();
        }

        void IFlightUpdate.FlightUpdate(in FlightFrameData frame)
        {
            if (!_initializationComplete)
            {
                return;
            }
            if (!(_audio != null))
            {
                return;
            }
            if (_moving)
            {
                if (!_audio.isPlaying)
                {
                    _audio.Play();
                }
                _audio.pitch = _pitch;
                _audio.volume = _volume;
            }
            else if (_audio.isPlaying)
            {
                _audio.Stop();
            }
        }

        public override void OnCraftStructureChanged(ICraftScript craftScript)
        {
            base.OnCraftStructureChanged(craftScript);
            if (Game.InFlightScene && (_jointRigidbody != base.PartScript.BodyScript.RigidBody || _joint == null))
            {
                FindAndSetupConnectionJoint();
            }
        }

        public override void OnSymmetry(SymmetryMode mode, IPartScript originalPart, bool created)
        {
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            Extender1 = Utilities.FindFirstGameObjectMyselfOrChildren("Extender1", base.PartScript.GameObject).transform;
            Extender2 = Utilities.FindFirstGameObjectMyselfOrChildren("Extender2", base.PartScript.GameObject).transform;
            Extender3 = Utilities.FindFirstGameObjectMyselfOrChildren("Extender3", base.PartScript.GameObject).transform;
            if (!Game.InFlightScene)
            {
                _initializationComplete = true;
                UpdateShaftExtension();
            }
        }

        private void FindAndSetupConnectionJoint()
        {
            int attachPointIndex = base.Data.AttachPointIndex;
            if (base.PartScript.Data.AttachPoints.Count <= attachPointIndex)
            {
                return;
            }
            AttachPoint attachPoint = base.PartScript.Data.AttachPoints[attachPointIndex];
            if (attachPoint.PartConnections.Count == 1)
            {
                foreach (IBodyJoint joint in base.PartScript.BodyScript.Joints)
                {
                    ConfigurableJoint jointForAttachPoint = joint.GetJointForAttachPoint(attachPoint);
                    if (jointForAttachPoint != null)
                    {
                        Rigidbody component = jointForAttachPoint.GetComponent<Rigidbody>();
                        if (base.PartScript.BodyScript.RigidBody == component)
                        {
                            _bodyJoint = joint;
                            _updatePistonShaft = true;

                            SoftJointLimit _linearlimit = new SoftJointLimit();
                            _linearlimit.limit = Data.Length;
                            _linearlimit.bounciness = 0;
                            _linearlimit.contactDistance = 0.001f;

                            jointForAttachPoint.xMotion = ConfigurableJointMotion.Limited;
                            jointForAttachPoint.yMotion = ConfigurableJointMotion.Locked;
                            jointForAttachPoint.zMotion = ConfigurableJointMotion.Locked;

                            JointDrive jointDrive = default(JointDrive);
                            jointDrive.positionSpring = float.MaxValue;
                            jointDrive.positionDamper = 0f;
                            jointDrive.maximumForce = Data.Force;
                            jointForAttachPoint.xDrive = jointDrive;

                            _joint = jointForAttachPoint;
                            _joint.linearLimit = _linearlimit;
                            _jointRigidbody = component;

                            break;
                        }
                    }
                }
            }
            else if (attachPoint.PartConnections.Count == 0)
            {
                _updatePistonShaft = true;
            }
        }

        private void UpdateShaftExtension()
        {
            if (_initializationComplete)
            {
                Vector3 vector = new Vector3(1, 0, 0);
                Extender1.localPosition = vector * Data.CurrentPosition;
                Extender2.localPosition = vector * Math.Max(0f, (Data.CurrentPosition - 0.4f));
                Extender3.localPosition = vector * Math.Max(0f, (Data.CurrentPosition - 0.8f));
            }
        }
    }
}