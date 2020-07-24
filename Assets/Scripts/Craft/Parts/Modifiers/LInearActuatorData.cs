namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using Assets.Scripts.Design;
    using ModApi;
    using ModApi.Craft.Parts;
    using ModApi.Craft.Parts.Attributes;
    using ModApi.Design.PartProperties;
    using ModApi.Math;
    using System;
    using System.Xml.Linq;
    using UnityEngine;

    [Serializable]
    [DesignerPartModifier("LInearActuator")]
    [PartModifierTypeId("RealActuators.LInearActuator")]
    public class LInearActuatorData : PartModifierData<LInearActuatorScript>
    {
        private const float DefaultLength = 0.4f;

        private const float DefaultAcceleration = 0.5f;

        private const float DefaultVelocity = 0.5f;

        private const float Density = 1550f; // ?

        [SerializeField]
        [PartModifierProperty(true, false)]
        private float _currentPosition;

        [SerializeField]
        [DesignerPropertySlider(0.05f, 1.2f, 24, Label = "Length", Order = 0, PreserveStateMode = PartModifierPropertyStatePreservationMode.SaveAlways, Tooltip = "Changes the length of the extender.")]
        private float _length = 0.5f;

        [SerializeField]
        [DesignerPropertySlider(1f, 200f, 200, Label = "Force", Order = 1, Tooltip = "Change the maximum force of the extender.")]
        private float _force = 0.5f;

        [SerializeField]
        [DesignerPropertySlider(1f, 200f, 200, Label = "Acceleration", Order = 1, Tooltip = "Change the acceleration of the extender.")]
        private float _acceleration = 0.5f;

        [SerializeField]
        [DesignerPropertySlider(1f, 200f, 200, Label = "Velocity", Order = 1, Tooltip = "Change the operation velocity of the extender.")]
        private float _velocity = 0.5f;

        public int AttachPointIndex {get; set;}

        public float CurrentPosition {get{return _currentPosition;} set{_currentPosition = value;}}

        //public override float Mass => CalculateVolume() * 1550f * 0.01f;

        //public override int Price => (int)(15000f * Width);

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
        protected override void OnCreated(XElement partModifierXml)
        {
            base.OnCreated(partModifierXml);
            AttachPointIndex = 0;
        }

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