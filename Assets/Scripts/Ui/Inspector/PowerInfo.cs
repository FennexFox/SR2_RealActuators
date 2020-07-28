namespace Assets.Scripts.Ui.Inspector
{
    using System;
    using ModApi.Craft.Parts;
    using ModApi.Craft.Parts.Input;
    using ModApi.Ui.Inspector;

    public class PowerInfo
    {
        private float _inputVolt;

        private float _resistance;

        private float _maxAmpere;

        private float _currentAmpere;

        public void AddPowerInfoModel(PartInspectorModel model)
        {
            var powerInfo = new GroupModel("Power Info"); // need to add ElectroMagnet Info
            powerInfo.Add(new TextModel("Input Volt", () => GetPowerInfoString("InpVolt")));
            powerInfo.Add(new TextModel("Internal Volt", () => GetPowerInfoString("IntVolt")));
            powerInfo.Add(new TextModel("Ampere", () => GetPowerInfoString("Ampere")));
            powerInfo.Add(new TextModel("Watt", () => GetPowerInfoString("Watt")));
            model.AddGroup(powerInfo);
        }

        public string GetPowerInfoString(string Label)
        {
            string result = null;
            switch (Label)
            {
                case "InpVolt":
                    result = $"{_inputVolt} V"; break;
                case "IntVolt":
                    result = $"{_currentAmpere * _resistance:n0} V"; break;
                case "Ampere":
                    result = $"{_currentAmpere:n0} A"; break;
                case "Watt":
                    result = $"{_inputVolt * _currentAmpere:n0} W"; break;
            }
            return result;
        }

        public PowerInfo(float InputVolt, float MaxAmpere, float CurrentAmpere, float Resistance)
        {
            _inputVolt = InputVolt;
            _maxAmpere = MaxAmpere;
            _currentAmpere = CurrentAmpere;
            _resistance = Resistance;
        }
    }
}