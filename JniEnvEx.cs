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

using System;
using Java.Interop;

namespace ApxLabs.FastAndroidCamera
{
	public enum PrimitiveArrayReleaseMode {
		CommitAndRelease = 0,
		Commit = 1,
		Release = 2
	}

	public static class JniEnvEx
	{
		public static IntPtr NewByteArray(int length)
		{
			return JniEnvironment.Arrays.NewByteArray(length).Handle;
		}

		public static unsafe byte* GetByteArrayElements(IntPtr array, bool isCopy)
		{
			return (byte*)JniEnvironment.Arrays.GetByteArrayElements(new JniObjectReference(array, JniObjectReferenceType.Global), &isCopy);
		}

		public static unsafe void ReleaseByteArrayElements(IntPtr array, byte* elements, PrimitiveArrayReleaseMode mode)
		{
			JniEnvironment.Arrays.ReleaseByteArrayElements(new JniObjectReference(array, JniObjectReferenceType.Global), (sbyte*)elements, (JniReleaseArrayElementsMode)mode);
		}
	}
}
