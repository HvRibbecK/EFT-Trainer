using System;
using System.Windows.Media;

namespace EFT_Trainer.Config
{
    [Serializable]
    public class OverlayConfig
    {
        public string ProcessFilePath { get; set; } = "";
        public string CrosshairFileLocation { get; set; } = "";

        public int OffsetX { get; set; } = 0;
        public int OffsetY { get; set; } = 0;

        public int CrosshairColorIndex { get; set; } = 0;
        public float CrosshairScale { get; set; } = 2;
        public double CrosshairOpacity { get; set; } = 1D;
    }
}
