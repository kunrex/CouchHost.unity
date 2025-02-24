using System;

namespace Hosting.Structs
{
    [Serializable]
    public struct ControllerData
    {
        public short letterButtons;
        public short directionButtons;
        
        public Vector2Data joyStickA; 
        public Vector2Data joyStickB;

        public ControllerData(float ax, float ay, float bx, float by, short letter, short direction)
        {
            joyStickA = new Vector2Data(ax, ay);
            joyStickB = new Vector2Data(bx, by);

            letterButtons = letter;
            directionButtons = direction;
        }
        
        public static (bool, bool, bool, bool) ExtractButtonData(short data)
        {
            return ((data & 4) == 4, (data & 2) == 2, (data ^ 1) == 1, (data & 0) == 0);
        }
    }
}