using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Collections.Specialized;

namespace DiscordDMLogitechLCD
{
    public class LCDWrapper
    {
        //LCD	SDK
        public const int LOGI_LCD_COLOR_BUTTON_LEFT = (0x00000100);
        public const int LOGI_LCD_COLOR_BUTTON_RIGHT = (0x00000200);
        public const int LOGI_LCD_COLOR_BUTTON_OK = (0x00000400);
        public const int LOGI_LCD_COLOR_BUTTON_CANCEL = (0x00000800);
        public const int LOGI_LCD_COLOR_BUTTON_UP = (0x00001000);
        public const int LOGI_LCD_COLOR_BUTTON_DOWN = (0x00002000);
        public const int LOGI_LCD_COLOR_BUTTON_MENU = (0x00004000);
        public const int LOGI_LCD_MONO_BUTTON_0 = (0x00000001);
        public const int LOGI_LCD_MONO_BUTTON_1 = (0x00000002);
        public const int LOGI_LCD_MONO_BUTTON_2 = (0x00000004);
        public const int LOGI_LCD_MONO_BUTTON_3 = (0x00000008);
        public const int LOGI_LCD_MONO_WIDTH = 160;
        public const int LOGI_LCD_MONO_HEIGHT = 43;
        public const int LOGI_LCD_COLOR_WIDTH = 320;
        public const int LOGI_LCD_COLOR_HEIGHT = 240;
        public const int LOGI_LCD_TYPE_MONO = (0x00000001);
        public const int LOGI_LCD_TYPE_COLOR = (0x00000002);
        [DllImport("LogitechLcdEnginesWrapper", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLcdInit(String friendlyName, int lcdType);
        [DllImport("LogitechLcdEnginesWrapper", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLcdIsConnected(int lcdType);
        [DllImport("LogitechLcdEnginesWrapper", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLcdIsButtonPressed(int button);
        [DllImport("LogitechLcdEnginesWrapper", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern void LogiLcdUpdate();
        [DllImport("LogitechLcdEnginesWrapper", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern void LogiLcdShutdown();
        //	Monochrome	LCD	functions
        [DllImport("LogitechLcdEnginesWrapper", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLcdMonoSetBackground(byte[] monoBitmap);
        [DllImport("LogitechLcdEnginesWrapper", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLcdMonoSetText(int lineNumber, String text);
        //	Color	LCD	functions
        [DllImport("LogitechLcdEnginesWrapper", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLcdColorSetBackground(byte[] colorBitmap);
        [DllImport("LogitechLcdEnginesWrapper", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLcdColorSetTitle(String text, int red, int green, int blue);
        [DllImport("LogitechLcdEnginesWrapper", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool LogiLcdColorSetText(int lineNumber, String text, int red, int green, int blue);
    }
}
