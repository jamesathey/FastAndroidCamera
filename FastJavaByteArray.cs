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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Java.Interop;

namespace ApxLabs.FastAndroidCamera
{
	/// <summary>
	/// A wrapper around a Java array that reads elements directly from the pointer instead of through expensive JNI calls.
	/// </summary>
	public sealed class FastJavaByteArray : IList<byte>, IDisposable
	{
		private JniObjectReference _javaRef;

		#region Constructors

		/// <summary>
		/// Creates a new FastJavaByteArray with the given number of bytes reserved.
		/// </summary>
		/// <param name="length">Number of bytes to reserve</param>
		public FastJavaByteArray(int length)
		{
			if (length <= 0)
				throw new ArgumentOutOfRangeException();

			JniObjectReference localRef = JniEnvironment.Arrays.NewByteArray(length);
			if (!localRef.IsValid)
				throw new OutOfMemoryException();

			// Retain a global reference to the byte array.
			_javaRef = localRef.NewGlobalRef();
			Count = length;

			bool isCopy = false;
			unsafe
			{
				// Get the pointer to the byte array using the global Handle
				Raw = (byte*)JniEnvironment.Arrays.GetByteArrayElements(_javaRef, &isCopy);
			}
		}

		/// <summary>
		/// Creates a FastJavaByteArray wrapper around an existing Java/JNI byte array
		/// </summary>
		/// <param name="handle">Native Java array handle</param>
		/// <param name="readOnly">Whether to consider this byte array read-only</param>
		public FastJavaByteArray(IntPtr handle, bool readOnly = true)
		{
			if (handle == IntPtr.Zero)
				throw new ArgumentNullException("handle");

			IsReadOnly = readOnly;

			// Retain a global reference to the byte array.
			_javaRef = new JniObjectReference(handle).NewGlobalRef();
			Count = JniEnvironment.Arrays.GetArrayLength(_javaRef);

			bool isCopy = false;
			unsafe
			{
				// Get a pinned pointer to the byte array using the global Handle
				Raw = (byte*)JniEnvironment.Arrays.GetByteArrayElements(_javaRef, &isCopy);
			}
		}

		#endregion

		#region Dispose Pattern

		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the
		/// <see cref="T:ApxLabs.FastAndroidCamera.FastJavaByteArray"/> is reclaimed by garbage collection.
		/// </summary>
		~FastJavaByteArray()
		{
			Dispose(false);
		}

		/// <summary>
		/// Releases all resource used by the <see cref="T:ApxLabs.FastAndroidCamera.FastJavaByteArray"/> object.
		/// </summary>
		/// <remarks>Call <see cref="Dispose"/> when you are finished using the
		/// <see cref="T:ApxLabs.FastAndroidCamera.FastJavaByteArray"/>. The <see cref="Dispose"/> method leaves the
		/// <see cref="T:ApxLabs.FastAndroidCamera.FastJavaByteArray"/> in an unusable state. After calling
		/// <see cref="Dispose"/>, you must release all references to the
		/// <see cref="T:ApxLabs.FastAndroidCamera.FastJavaByteArray"/> so the garbage collector can reclaim the memory that
		/// the <see cref="T:ApxLabs.FastAndroidCamera.FastJavaByteArray"/> was occupying.</remarks>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if (!_javaRef.IsValid)
				return;

			unsafe
			{
				// tell Java that we're done with this array
				JniEnvironment.Arrays.ReleaseByteArrayElements(_javaRef, (sbyte*)Raw, JniReleaseArrayElementsMode.Default);
			}

			if (disposing)
			{
				JniObjectReference.Dispose(ref _javaRef);
			}
		}

		#endregion

		#region IList<byte> Properties

		/// <summary>
		/// Count of bytes
		/// </summary>
		public int Count { get; private set; }

		/// <summary>
		/// Gets a value indicating whether this byte array is read only.
		/// </summary>
		/// <value><c>true</c> if read only; otherwise, <c>false</c>.</value>
		public bool IsReadOnly
		{
			get;
			private set;
		}

		/// <summary>
		/// Indexer
		/// </summary>
		/// <param name="index">Index of byte</param>
		/// <returns>Byte at the given index</returns>
		public byte this[int index]
		{
			get
			{
				if (index < 0 || index >= Count)
				{
					throw new ArgumentOutOfRangeException();
				}
				byte retval;
				unsafe
				{
					retval = Raw[index];
				}
				return retval;
			}
			set
			{
				if (IsReadOnly)
				{
					throw new NotSupportedException("This FastJavaByteArray is read-only");
				}

				if (index < 0 || index >= Count)
				{
					throw new ArgumentOutOfRangeException();
				}
				unsafe
				{
					Raw[index] = value;
				}
			}
		}

		#endregion

		#region IList<byte> Methods

		/// <summary>
		/// Adds a single byte to the list. Not supported
		/// </summary>
		/// <param name="item">byte to add</param>
		public void Add(byte item)
		{
			throw new NotSupportedException("FastJavaByteArray is fixed length");
		}

		/// <summary>
		/// Not supported
		/// </summary>
		public void Clear()
		{
			throw new NotSupportedException("FastJavaByteArray is fixed length");
		}

		/// <summary>
		/// Returns true if the item is found int he array
		/// </summary>
		/// <param name="item">Item to find</param>
		/// <returns>True if the item is found</returns>
		public bool Contains(byte item)
		{
			return IndexOf(item) >= 0;
		}

		/// <summary>
		/// Copies the contents of the FastJavaByteArray into a byte array
		/// </summary>
		/// <param name="array">The array to copy to.</param>
		/// <param name="arrayIndex">The zero-based index into the destination array where CopyTo should start.</param>
		public void CopyTo(byte[] array, int arrayIndex)
		{
			unsafe
			{
				Marshal.Copy(new IntPtr(Raw), array, arrayIndex, Math.Min(Count, array.Length - arrayIndex));
			}
		}

		/// <summary>
		/// Retreives enumerator
		/// </summary>
		/// <returns>Enumerator</returns>
		[DebuggerHidden]
		public IEnumerator<byte> GetEnumerator()
		{
			return new FastJavaByteArrayEnumerator(this);
		}

		/// <summary>
		/// Retreives enumerator
		/// </summary>
		/// <returns>Enumerator</returns>
		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return new FastJavaByteArrayEnumerator(this);
		}

		/// <summary>
		/// Gets the first index of the given value
		/// </summary>
		/// <param name="item">Item to search for</param>
		/// <returns>Index of found item</returns>
		public int IndexOf(byte item)
		{
			for (int i = 0; i < Count; ++i)
			{
				byte current;
				unsafe
				{
					current = Raw[i];
				}
				if (current == item)
					return i;
			}
			return -1;
		}

		/// <summary>
		/// Not supported
		/// </summary>
		/// <param name="index"></param>
		/// <param name="item"></param>
		public void Insert(int index, byte item)
		{
			throw new NotSupportedException("FastJavaByteArray is fixed length");
		}

		/// <summary>
		/// Not supported
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public bool Remove(byte item)
		{
			throw new NotSupportedException("FastJavaByteArray is fixed length");
		}

		/// <summary>
		/// Not supported
		/// </summary>
		/// <param name="index"></param>
		public void RemoveAt(int index)
		{
			throw new NotSupportedException("FastJavaByteArray is fixed length");
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// Gets the raw pointer to the underlying data.
		/// </summary>
		public unsafe byte* Raw { get; private set; }

		/// <summary>
		/// Gets the handle of the Java reference to the array.
		/// </summary>
		/// <value>The handle.</value>
		public IntPtr Handle
		{
			get { return _javaRef.Handle; }
		}

		#endregion
	}
}
