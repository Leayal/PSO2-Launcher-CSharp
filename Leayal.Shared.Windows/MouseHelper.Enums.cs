using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.Shared.Windows
{
	public static partial class MouseHelper
	{
		/// <summary>Contains information about the state of the mouse.</summary>
		/// <remarks><see href="https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-rawmouse"/></remarks>
		enum RawInputMouseFlags : ushort
		{
			/// <summary>Left button changed to down.</summary>
			RI_MOUSE_BUTTON_1_DOWN = 0x0001,
			/// <summary>Left button changed to down.</summary>
			RI_MOUSE_LEFT_BUTTON_DOWN = 0x0001,
			/// <summary>Left button changed to up.</summary>
			RI_MOUSE_BUTTON_1_UP = 0x0002,
			/// <summary>Left button changed to up.</summary>
			RI_MOUSE_LEFT_BUTTON_UP = 0x0002,
			/// <summary>Right button changed to down.</summary>
			RI_MOUSE_BUTTON_2_DOWN = 0x0004,
			/// <summary>Right button changed to down.</summary>
			RI_MOUSE_RIGHT_BUTTON_DOWN = 0x0004,
			/// <summary>Right button changed to up.</summary>
			RI_MOUSE_BUTTON_2_UP = 0x0008,
			/// <summary>Right button changed to up.</summary>
			RI_MOUSE_RIGHT_BUTTON_UP = 0x0008,
			/// <summary>Middle button changed to down.</summary>
			RI_MOUSE_BUTTON_3_DOWN = 0x0010,
			/// <summary>Middle button changed to down.</summary>
			RI_MOUSE_MIDDLE_BUTTON_DOWN = 0x0010,
			/// <summary>Middle button changed to up.</summary>
			RI_MOUSE_BUTTON_3_UP = 0x0020,
			/// <summary>Middle button changed to up.</summary>
			RI_MOUSE_MIDDLE_BUTTON_UP = 0x0020,
			/// <summary>XBUTTON1 changed to down.</summary>
			RI_MOUSE_BUTTON_4_DOWN = 0x0040,
			/// <summary>XBUTTON1 changed to up.</summary>
			RI_MOUSE_BUTTON_4_UP = 0x0080,
			/// <summary>XBUTTON2 changed to down.</summary>
			RI_MOUSE_BUTTON_5_DOWN = 0x0100,
			/// <summary>XBUTTON2 changed to up.</summary>
			RI_MOUSE_BUTTON_5_UP = 0x0200,
			/// <summary>
			/// Raw input comes from a mouse wheel. The wheel delta is stored in usButtonData.
			/// A positive value indicates that the wheel was rotated forward, away from the user; a negative value indicates that the wheel was rotated backward, toward the user.
			/// </summary>
			RI_MOUSE_WHEEL = 0x0400,
			/// <summary>Raw input comes from a horizontal mouse wheel. The wheel delta is stored in usButtonData.
			/// A positive value indicates that the wheel was rotated to the right; a negative value indicates that the wheel was rotated to the left.
			/// Windows XP/2000: This value is not supported.
			/// </summary>
			RI_MOUSE_HWHEEL = 0x0800
		}
	}
}
