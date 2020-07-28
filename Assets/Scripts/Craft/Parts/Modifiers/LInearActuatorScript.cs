namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using Assets.Scripts;
    using Assets.Scripts.Ui.Inspector;
    using ModApi;
    using ModApi.Craft;
    using ModApi.Craft.Parts;
    using ModApi.Craft.Parts.Input;
    using ModApi.Design;
    using ModApi.GameLoop;
    using ModApi.GameLoop.Interfaces;
    using ModApi.Math;
    using ModApi.Ui.Inspector;
    using System;
    using UnityEngine;

    public class LinearActuatorScript : PartModifierScript<LinearActuatorData>, IDesignerUpdate, IGameLoopItem, IFlightStart, IFlightUpdate, IFlightFixedUpdate
    {
        private PowerInfo powerInfo;

        private ICraftFlightData flightData;

        private Vector3 localUp => base.PartScript.Transform.TransformDirection(Vector3.up);

        private float _priorVelocity;

        private float _currentVelocity;

        private float _currentAcceleration;

        private Vector3 _currentForce;

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
            _currentVelocity = (Data.CurrentPosition - _priorLength) / Time.deltaTime;
            _currentAcceleration = (_currentVelocity - _priorVelocity) / Time.deltaTime;
            _currentForce = (_currentAcceleration * localUp) * _joint.connectedBody.mass + _joint.currentForce;
            if (!(Data.Length / 2 - Math.Abs(Data.CurrentPosition - Data.Length / 2) < 0.001f && Vector3.Angle(localUp * (Data.CurrentPosition - Data.Length / 2), flightData.GravityFrame) < 90))
            { _currentForce -= Vector3.Project(flightData.GravityFrame - flightData.AccelerationFrame, localUp) * _joint.connectedBody.mass; }
            _priorLength = Data.CurrentPosition;
            _priorVelocity = _currentVelocity;

            if (base.PartScript.CommandPod != null && _input != null && _joint != null)
            {
                if (_bodyJoint != null && !_bodyJoint.PartConnection.IsDestroyed)
                {
                    _joint.connectedBody.WakeUp();
                    _jointRigidbody.WakeUp();

                    float targetVelocity = Data.Velocity * _input.Value;
                    float nextVelocity = _currentVelocity + Data.Acceleration * Math.Sign(_input.Value) * Time.deltaTime;
                    nextVelocity = Mathf.Clamp(nextVelocity, -1 * Math.Abs(targetVelocity), Math.Abs(targetVelocity));
                    float nextLength = Data.CurrentPosition + nextVelocity * Time.deltaTime;
                    nextLength = Mathf.Clamp(nextLength, 0f, Data.Length);
                    Data.CurrentPosition = nextLength;
                }
            }

            Vector3 targetposition = new Vector3(Data.CurrentPosition - Data.Length / 2, 0f, 0f);
            _joint.targetPosition = targetposition;

            if (_updatePistonShaft) { UpdateShaftExtension(); }
        }

        void IFlightStart.FlightStart(in FlightFrameData frame)
        {
            _input = GetInputController("Velocity");
            powerInfo = new PowerInfo(_input, base.PartScript.BatteryFuelSource, Data.InputVolt, Data.MaxAmpere, Data.Resistance);
            flightData = this.PartScript.CraftScript.FlightData;
            FindAndSetupConnectionJoint();
            _joint.anchor += _jointRigidbody.transform.InverseTransformVector(localUp * Data.Length / 2);
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
            if (powerInfo != null) { powerInfo.UpdateBattery(base.PartScript.BatteryFuelSource); }
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
                            _Linearlimit.limit = Data.Length / 2;
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
            return position.magnitude;
        }

        private float ConnectedBodyOffset()
        {
            Vector3 position = base.PartScript.Transform.TransformPoint(base.PartScript.Data.AttachPoints[1].Position);
            position = _joint.connectedBody.transform.InverseTransformPoint(position);
            return position.magnitude;
        }

        private string GetActuatorInfo(string Label)
        {
            string result = null;
            switch (Label)
            {
                case "Position":
                    result = $"{Data.CurrentPosition:n2} m"; break;
                case "Velocity":
                    result = $"{_currentVelocity:n2} m/s"; break;
                case "Acceleration":
                    result = $"{_currentAcceleration:n2} m/s2"; break;
                case "Force":
                    result = $"{Units.GetForceString(_currentForce.magnitude)}"; break;
            }
            return result;
        }

        public override void OnGenerateInspectorModel(PartInspectorModel model)
        {
            powerInfo.AddPowerInfoModel(model);

            var actuatorInfo = new GroupModel("Actuator Info");
            actuatorInfo.Add(new TextModel("Position", () => GetActuatorInfo("Position")));
            actuatorInfo.Add(new TextModel("Veclocity", () => GetActuatorInfo("Velocity")));
            actuatorInfo.Add(new TextModel("Acceleration", () => GetActuatorInfo("Acceleration")));
            actuatorInfo.Add(new TextModel("Force", () => GetActuatorInfo("Force")));
            model.AddGroup(actuatorInfo);
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