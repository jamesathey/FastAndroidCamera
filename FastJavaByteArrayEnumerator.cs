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

using System;
using System.Collections.Generic;

namespace ApxLabs.FastAndroidCamera
{
	internal class FastJavaByteArrayEnumerator : IEnumerator<byte>
	{
		internal FastJavaByteArrayEnumerator(FastJavaByteArray arr)
		{
			if (arr == null)
				throw new ArgumentNullException();

			_arr = arr;
			_idx = 0;
		}

		/// <summary>
		/// Gets the current byte in the collection.
		/// </summary>
		public byte Current
		{
			get
			{
				byte retval;
				unsafe
				{
					// get value from pointer
					retval = _arr.Raw[_idx];
				}
				return retval;
			}
		}

		/// <summary>
		/// Releases all resource used by the <see cref="T:ApxLabs.FastAndroidCamera.FastJavaByteArrayEnumerator"/> object.
		/// </summary>
		/// <remarks>Call <see cref="Dispose"/> when you are finished using the
		/// <see cref="T:ApxLabs.FastAndroidCamera.FastJavaByteArrayEnumerator"/>. The <see cref="Dispose"/> method leaves the
		/// <see cref="T:ApxLabs.FastAndroidCamera.FastJavaByteArrayEnumerator"/> in an unusable state. After calling
		/// <see cref="Dispose"/>, you must release all references to the
		/// <see cref="T:ApxLabs.FastAndroidCamera.FastJavaByteArrayEnumerator"/> so the garbage collector can reclaim the
		/// memory that the <see cref="T:ApxLabs.FastAndroidCamera.FastJavaByteArrayEnumerator"/> was occupying.</remarks>
		public void Dispose()
		{
		}

		/// <summary>
		/// Advances the enumerator to the next element of the collection.
		/// </summary>
		/// <returns><c>true</c> if the enumerator was successfully advanced to the next element; <c>false</c> if the enumerator has passed the end of the collection.</returns>
		public bool MoveNext()
		{
			if (_idx > _arr.Count)
				return false;

			++_idx;

			return _idx < _arr.Count;
		}

		/// <summary>
		/// Sets the enumerator to its initial position, which is before the first element in the collection.
		/// </summary>
		public void Reset()
		{
			_idx = 0;
		}

		#region IEnumerator implementation

		/// <summary>
		/// Gets the current element in the collection.
		/// </summary>
		/// <value>The system. collections. IE numerator. current.</value>
		object System.Collections.IEnumerator.Current
		{
			get
			{
				byte retval;
				unsafe
				{
					// get value from pointer
					retval = _arr.Raw[_idx];
				}
				return retval;
			}
		}

		#endregion

		FastJavaByteArray _arr;
		int _idx;
	}
}
