using LinePutScript.Converter;

namespace VPet.Plugin.VPet_VoiceBox
{
    public class Setting
    {
        [Line]
        public int Speaker { get; set; } = 1;

        [Line]
        public bool Enable { get; set; } = true;

        [Line]
        public double VolumeScale { get; set; } = 1;

        [Line]
        public string VoiceBoxEngineUri { get; set; } = "http://127.0.0.1:50021";
    }
}
