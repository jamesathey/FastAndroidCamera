# FastAndroidCamera

Xamarin's' default wrapping strategy to generate the C# bindings to the Android SDK works well in most cases. The
<a href="https://developer.android.com/reference/android/hardware/Camera.html">Android.Hardware.Camera API</a> is
**not** one of those cases.

If you only want to open the camera, take a picture, and get a JPEG buffer, then the existing wrapper should work fine
for you. If instead you need to do Computer Vision or video encoding in software, then the way Xamarin marshals byte
arrays from Java to C# will result in lots of pointless buffer copies, as well as thrashing in the garbage collectors in
both the JVM and in Mono.

FastAndroidCamera makes it possible to use the Android.Hardware.Camera API as efficiently in C# as using the underlying
android.hardware.Camera API in Java. No additional byte arrays are created, and no additional buffer copies are needed.

## Features

* Get Android camera preview callbacks in your favorite CLR language without buffer duplication or GC overhead
* Direct access to the framebuffer's Java byte array (FastJavaByteArray)
** Direct (unsafe) access to the underlying byte*, which can then be provided to native methods (via PInvoke)
** Fast per-element access (via the pinned byte*)
** Read-only and read-write modes
** Implements IList<byte>
* "One shot" callback is also supported
** Java overhead is the same, but reduces marshaling overhead of using that buffer in C# or native code

## Usage

```C#
void StartCamera()
{
	Camera camera = Camera.Open();
	Camera.Parameters parameters = camera.GetParameters();

	// snip - set resolution, frame rate, preview format, etc.

	camera.SetParameters(parameters);

	// assuming the SurfaceView has been set up elsewhere
	camera.SetPreviewDisplay(_surfaceView.Holder);
	camera.StartPreview();

	int numBytes = (parameters.PreviewSize.Width * parameters.PreviewSize.Height * ImageFormat.GetBitsPerPixel(parameters.PreviewFormat)) / 8;
	for (uint i = 0; i < NUM_PREVIEW_BUFFERS; ++i)
	{
		// allocate new Java byte arrays for Android to use for preview frames
		camera.AddCallbackBuffer(new FastJavaByteArray(numBytes));
	}

	// non-marshaling version of the preview callback
	camera.SetNonMarshalingPreviewCallback(this);
}

public void OnPreviewFrame(IntPtr data, SdkCamera camera)
{
	// Wrap the JNI reference to the Java byte array
	FastJavaByteArray buffer = new FastJavaByteArray(data);

	// Get individual bytes
	byte firstByte = buffer[0];
	byte lastByte = buffer[buffer.Count - 1];

	// Iterate over it
	foreach (byte b in buffer)
	{
		// access one at a time
	}

	// Pass it to native APIs
	myNativeBytePointerMethod(buffer.Raw, buffer.Count);

	// reuse the Java byte array; return it to the Camera API
	camera.AddCallbackBuffer(buffer);
}
```

# Analysis

## The Preview Callback

For example, the ordinary way to get preview callbacks from the Camera looks like the following:

```C#
void StartCamera()
{
	Camera camera = Camera.Open();
	Camera.Parameters parameters = camera.GetParameters();

	// snip - set resolution, frame rate, preview format, etc.

	// assuming the SurfaceView has been set up elsewhere
	camera.SetPreviewDisplay(_surfaceView.Holder);

	camera.StartPreview();
	camera.SetPreviewCallback(this);
}

public void OnPreviewFrame(byte[] data, SdkCamera camera)
{
	// Do per-frame video processing here
}
```

While the camera is open, performance suffers badly. Under the hood, the following takes place:

1. Every preview frame, the JVM creates a new byte array big enough to hold the frame buffer, and copies the image into it.
2. The JVM calls the Java onPreviewFrame() callback, auto-generated by Xamarin.
3. The Android Callable Wrapper creates a new C# byte array, copies the contents of the Java byte array into it, and
provides that new C# byte array to OnPreviewFrame().
4. Without any other references to the Java byte array, it will be garbage collected by the JVM sometime soon.
5. At the end of OnPreviewFrame(), assuming it is not retained elsewhere, the C# byte array will pass out of scope and
will eventually get garbage collected.

Compared to running the equivalent code in Java, using this technique in C# involves one additional array, one additional
buffer copy, and one additional item to get GC'd. These arrays are very large. For example, at 720p with the default
NV21 ImageFormat, each array is

1280 pixels * 720 pixels * 1.5 bytes/pixel = 1382400 bytes.

The preview callback is called at the camera framerate - up to 60 times a second, but more commonly 30 times a second.
At 720p/30, 1382400 * 30 = 41472000 bytes/sec (~39.55 MiB/s) has to be allocated, copied, and garbage collected.
40 megs! The memory pressure from the constant construction and destruction of these huge arrays causes the garbage
collectors to run very frequently, and whenever a GC runs in either VM, the world stops.

## Using AddPreviewCallback()

"But wait!", you exclaim. "Just pre-allocate your preview buffers, and provide them to the API. That way, the byte
arrays are never garbage collected, and the thrashing goes away." That strategy looks like the following:

```C#
void StartCamera()
{
	Camera camera = Camera.Open();
	Camera.Parameters parameters = camera.GetParameters();

	// snip - set resolution, frame rate, preview format, etc.

	camera.SetParameters(parameters);

	// assuming the SurfaceView has been set up elsewhere
	camera.SetPreviewDisplay(_surfaceView.Holder);

	int numBytes = (parameters.PreviewSize.Width * parameters.PreviewSize.Height * ImageFormat.GetBitsPerPixel(parameters.PreviewFormat)) / 8;
	for (uint i = 0; i < NUM_PREVIEW_BUFFERS; ++i)
	{
		camera.AddCallbackBuffer(new byte[numBytes]);
	}

	camera.StartPreview();
	camera.SetPreviewCallback(this);
}

public void OnPreviewFrame(byte[] data, SdkCamera camera)
{
	// Do per-frame video processing here

	camera.AddCallbackBuffer(data);
}
```

In Java, this strategy improves performance because the frame buffers get reused, and assuming NUM_PREVIEW_BUFFERS is
large enough, the camera API is never starved for a preview buffer. Unfortunately, in C#, this strategy is worse than
the naive approach!

1. For each call to AddCallbackBuffer() in StartCamera(), Mono creates a **numBytes**-sized C# byte array (as requested).
2. The Android Callable Wrapper creates a **numBytes**-sized Java byte array, *copies its contents* into the Java byte
array, and then passes it to the real Android.Hardware.Camera.addCallbackBuffer() method in Java.
3. Without any other references to the C# byte array, it will be garbage collected by Mono sometime soon.
4. For every preview frame, Java provides the Java byte array from step 2 to the Java version of the preview callback.
5. The Android Callable Wrapper creates a new C# byte array, copies the contents of the Java byte array into it, and
provides that new C# byte array to OnPreviewFrame().
6. Without any other references to the Java byte array, it will be garbage collected by the JVM sometime soon.
7. At the end of OnPreviewFrame(), the new C# byte array from step 5 is provided to AddCallbackBuffer; go back to step 2.
This C# byte array will be pointlessly copied into a new Java byte array, and will eventually get garbage collected.

In other words, every frame causes two new buffers to be allocated (one in Java and one in C#), two extra buffer copies
(from Java to Mono and back to Java again), and both buffers to be discarded and eventually GC'd. Depending on your
preview framerate, this process can happen up to 60 times a second!

# Acknowledgments

Many thanks to Jon Pryor from Xamarin, who showed us how to bypass the usual callback registration mechanism for this
API.
