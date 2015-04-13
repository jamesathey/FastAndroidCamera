// <copyright company="APX Labs, Inc.">
//     Copyright (c) APX Labs, Inc. All rights reserved.
// </copyright>
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//
// Many thanks to Jonathan Pryor from Xamarin for his assistance

using Android.Runtime;
using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ApxLabs.FastAndroidCamera
{
	public enum PrimitiveArrayReleaseMode {
		CommitAndRelease = 0,
		Commit = 1,
		Release = 2
	}

	public static class JniEnvEx
	{
		delegate IntPtr IntPtr_IntPtr_ref_Boolean_IntPtr (IntPtr env, IntPtr array, ref bool copy);

		delegate void IntPtr_IntPtr_IntPtr_int_void (IntPtr env, IntPtr array, IntPtr elements, int mode);

		delegate IntPtr IntPtr_int_IntPtr (IntPtr env, int length);

		static void GetDelegate<TDelegate> (string name, ref TDelegate value)
			where TDelegate : class
		{
			if (value != null)
				return;
			var envP = typeof(JNIEnv).GetProperty ("Env", BindingFlags.NonPublic | BindingFlags.Static);
			var env = envP.GetValue (null);
			var JniEnvP = env.GetType ().GetField ("JniEnv", BindingFlags.NonPublic | BindingFlags.Instance);
			var JniEnv = JniEnvP.GetValue (env);
			var d = JniEnv.GetType ().GetField (name);
			value = (TDelegate) (object) Marshal.GetDelegateForFunctionPointer ((IntPtr) d.GetValue (JniEnv), typeof (TDelegate));
		}

		[ThreadStatic]
		static IntPtr_int_IntPtr _NewByteArray;

		public static IntPtr NewByteArray (int length)
		{
			GetDelegate ("NewByteArray", ref _NewByteArray);
			return _NewByteArray (JNIEnv.Handle, length);
		}

		[ThreadStatic]
		static IntPtr_IntPtr_ref_Boolean_IntPtr _GetByteArrayElements;

		public static unsafe byte* GetByteArrayElements (IntPtr array, ref bool isCopy)
		{
			GetDelegate ("GetByteArrayElements", ref _GetByteArrayElements);
			return (byte*) _GetByteArrayElements (JNIEnv.Handle, array, ref isCopy);
		}

		[ThreadStatic]
		static IntPtr_IntPtr_IntPtr_int_void _ReleaseByteArrayElements;

		public static unsafe void ReleaseByteArrayElements (IntPtr array, byte* elements, PrimitiveArrayReleaseMode mode)
		{
			GetDelegate ("ReleaseByteArrayElements", ref _ReleaseByteArrayElements);
			_ReleaseByteArrayElements (JNIEnv.Handle, array, (IntPtr)elements, (int)mode);
		}
	}
}
