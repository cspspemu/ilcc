using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace ilcc.Runtime.C
{
	unsafe public sealed class CTime
	{
		/// <summary>
		/// Returns the number of clock ticks elapsed since the program was launched.
		///
		/// The macro constant expression CLOCKS_PER_SEC specifies the relation between
		/// a clock tick and a second (clock ticks per second).
		///
		/// The initial moment of reference used by clock as the beginning of the program
		/// execution may vary between platforms. To calculate the actual processing times
		/// of a program, the value returned by clock should be compared to a value returned
		/// by an initial call to clock.
		/// </summary>
		/// <returns>
		/// The number of clock ticks elapsed since the program start.
		/// 
		/// On failure, the function returns a value of -1.
		/// 
		/// clock_t is a type defined in &lt;ctime&gt; to some type capable of representing
		/// clock tick counts and support arithmetical operations (generally a long integer).
		/// </returns>
		/// <see cref="http://www.cplusplus.com/reference/clibrary/ctime/clock/"/>
		[CExport]
		static public int clock()
		{
			return (int)(DateTime.UtcNow - Process.GetCurrentProcess().StartTime).TotalMilliseconds;
		}

		/// <summary>
		/// Get current time
		/// Get the current calendar time as a time_t object.
		///
		/// The function returns this value, and if the argument is not
		/// a null pointer, the value is also set to the object pointed by timer.
		/// </summary>
		/// <param name="timer">
		/// Pointer to an object of type time_t, where the time value is stored.
		/// Alternativelly, this parameter can be a null pointer, in which case
		/// the parameter is not used, but a time_t object is still returned by the function.
		/// </param>
		/// <returns>
		/// The current calendar time as a time_t object.
		/// 
		/// If the argument is not a null pointer, the return value is the same as the one stored in the location pointed by the argument.
		/// 
		/// If the function could not retrieve the calendar time, it returns a -1 value.
		/// </returns>
		[CExport]
		static public int time(int *timer)
		{
			int timestamp = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
			if (timer != null) *timer = timestamp;
			return timestamp;
		}
	}
}
