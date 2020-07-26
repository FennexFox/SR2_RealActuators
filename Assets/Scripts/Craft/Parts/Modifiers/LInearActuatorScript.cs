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

    public class LinearActuatorScript : PartModifierScript<LinearActuatorData>, IDesignerUpdate, IGameLoopItem, IFlightStart, IFlightUpdate, IFlightFixedUpdate
    {
        private float _priorLength;

        private float _lengthOffset;

        private bool _updatePistonShaft;

        private IBodyJoint _bodyJoint;

        private bool _initializationComplete;

        private IInputController _input;

        private ConfigurableJoint _joint;

        private Rigidbody _jointRigidbody;

        private Transform Extender1;
        private Transform Extender2;
        private Transform Extender3;

        void IDesignerUpdate.DesignerUpdate(in DesignerFrameData frame)
        {
            if (_initializationComplete)
            {
                base.Data.CurrentPosition = 0f;
                UpdateShaftExtension();
            }
        }

        void IFlightFixedUpdate.FlightFixedUpdate(in FlightFrameData frame)
        {
            if (!_initializationComplete) { return; }

            Data.CurrentPosition = (ConnectedBodyLocalPosition() - _lengthOffset) - 0.5f;
            Debug.Log($"{_joint}, {base.PartScript.CommandPod}, {_input}");

            if (base.PartScript.CommandPod == null || _input == null) { return; }

            if (_joint != null)
            {
                if (_bodyJoint != null && !_bodyJoint.PartConnection.IsDestroyed)
                {
                    float currentVelocity = (Data.CurrentPosition - _priorLength) / Time.deltaTime;

                    _joint.connectedBody.WakeUp();
                    _jointRigidbody.WakeUp();

                    float targetVelocity = Data.Velocity * _input.Value;
                    float nextVelocity = currentVelocity + Data.Acceleration * Math.Sign(_input.Value) * Time.deltaTime;
                    nextVelocity = Mathf.Clamp(nextVelocity, -1 * Math.Abs(targetVelocity), Math.Abs(targetVelocity));
                    float nextLength = Data.CurrentPosition + nextVelocity * Time.deltaTime;
                    nextLength = Mathf.Clamp(nextLength, 0f, Data.Length);

                    _priorLength = Data.CurrentPosition;
                    Data.CurrentPosition = nextLength;
                }
                Vector3 targetposition = new Vector3(Data.CurrentPosition - Data.Length/2, 0f, 0f);
                _joint.targetPosition = targetposition;
            }

            if (_updatePistonShaft) { UpdateShaftExtension(); }
        }

        void IFlightStart.FlightStart(in FlightFrameData frame)
        {
            _input = GetInputController("Velocity");
            FindAndSetupConnectionJoint();
            _joint.anchor += Vector3.up * Data.Length/2;
            _lengthOffset = ConnectedBodyOffset();
            _initializationComplete = true;
            UpdateShaftExtension();
        }

        void IFlightUpdate.FlightUpdate(in FlightFrameData frame)
        {
        }

        public override void OnCraftStructureChanged(ICraftScript craftScript)
        {
            base.OnCraftStructureChanged(craftScript);
            if (Game.InFlightScene)
            {
                _lengthOffset = ConnectedBodyOffset();
                if (_jointRigidbody != base.PartScript.BodyScript.RigidBody || _joint == null)
                {
                    FindAndSetupConnectionJoint();
                }
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
            AttachPoint attachPoint = base.PartScript.Data.AttachPoints[1];
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

                            SoftJointLimit _Linearlimit = new SoftJointLimit();
                            _Linearlimit.limit = Data.Length/2;
                            _Linearlimit.bounciness = 0;
                            _Linearlimit.contactDistance = 0.001f;

                            jointForAttachPoint.xMotion = ConfigurableJointMotion.Limited;
                            jointForAttachPoint.yMotion = ConfigurableJointMotion.Locked;
                            jointForAttachPoint.zMotion = ConfigurableJointMotion.Locked;

                            JointDrive jointDrive = default(JointDrive);
                            jointDrive.positionSpring = float.MaxValue;
                            jointDrive.positionDamper = 0f;
                            jointDrive.maximumForce = Data.Force;
                            jointForAttachPoint.xDrive = jointDrive;

                            _joint = jointForAttachPoint;
                            _joint.linearLimit = _Linearlimit;
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

        private float ConnectedBodyLocalPosition()
        {
            Vector3 position = _joint.connectedBody.position;
            position = this.PartScript.Transform.InverseTransformPoint(position);
            return position.y;
        }

        private float ConnectedBodyOffset()
        {
            Vector3 position = base.PartScript.Transform.TransformPoint(base.PartScript.Data.AttachPoints[1].Position);
            position = _joint.connectedBody.transform.InverseTransformPoint(position);
            return position.magnitude;
        }

        private void UpdateShaftExtension()
        {
            if (_initializationComplete)
            {
                Vector3 vector = new Vector3(0, 0, 1);
                Extender1.localPosition = vector * Data.CurrentPosition;
                Extender2.localPosition = vector * Math.Max(0f, (Data.CurrentPosition - 0.4f));
                Extender3.localPosition = vector * Math.Max(0f, (Data.CurrentPosition - 0.8f));
            }
        }
    }
}