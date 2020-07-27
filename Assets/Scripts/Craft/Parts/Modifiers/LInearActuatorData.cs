namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using Assets.Scripts.Design;
    using ModApi.Craft.Parts;
    using ModApi.Craft.Parts.Attributes;
    using ModApi.Design.PartProperties;
    using ModApi.Math;
    using System;
    using UnityEngine;

    [Serializable]
    [DesignerPartModifier("LinearActuator")]
    [PartModifierTypeId("RealActuators.LinearActuator")]
    public class LinearActuatorData : PartModifierData<LinearActuatorScript>, IPowerData
    {
        private const float DefaultLength = 0.4f;

        private const float DefaultAcceleration = 1f;

        private const float DefaultVelocity = 1f;

        private const float DefaultForce = 10f;

        private const float Density = 1550f; // ?

        [SerializeField]
        [PartModifierProperty(true, false)]
        private float _currentPosition;

        [SerializeField]
        [DesignerPropertySlider(0.05f, 1.2f, 24, Label = "Length", Order = 0, PreserveStateMode = PartModifierPropertyStatePreservationMode.SaveAlways, Tooltip = "Changes the length of the extender.")]
        private float _length = 0.4f;

        [SerializeField]
        [DesignerPropertySlider(10f, 100f, 20, Label = "Force", Order = 1, Tooltip = "Change the maximum force of the extender.")]
        private float _force = 10f;

        [SerializeField]
        [DesignerPropertySlider(1f, 200f, 200, Label = "Acceleration", Order = 1, Tooltip = "Change the acceleration of the extender.")]
        private float _acceleration = 1f;

        [SerializeField]
        [DesignerPropertySlider(0.01f, 1f, 100, Label = "Velocity", Order = 1, Tooltip = "Change the operation velocity of the extender.")]
        private float _velocity = 1f;

        public float CurrentPosition { get { return _currentPosition; } set { _currentPosition = value; } }

        //public override float Mass => CalculateVolume() * 1550f * 0.01f;

        //public override int Price => (int)(15000f * Width);

        public float InputVolt => 120f;

        public float MaxAmpere => 1f;

        public float Resistance => 1f;

        public float Length => _length;

        public float Force => _force;

        public float Acceleration => _acceleration;

        public float Velocity => _velocity;
        /*
                public void UpdateScale()
                {
                    base.Script.UpdateScale();
                }
        */
        protected override void OnDesignerInitialization(IDesignerPartPropertiesModifierInterface d)
        {
            d.OnValueLabelRequested(() => _length, (float x) => x.ToString("0.00") + "m");
            d.OnValueLabelRequested(() => _force, (float x) => Units.GetForceString(x));
            d.OnValueLabelRequested(() => _acceleration, (float x) => Units.GetAccelerationString(x));
            d.OnValueLabelRequested(() => _velocity, (float x) => Units.GetVelocityString(x));
            d.OnPropertyChanged(() => _length, (x, y) =>
            {
                Symmetry.SynchronizePartModifiers(base.Part.PartScript);
                base.Part.PartScript.CraftScript.RaiseDesignerCraftStructureChangedEvent();
            });
        }
    }
}