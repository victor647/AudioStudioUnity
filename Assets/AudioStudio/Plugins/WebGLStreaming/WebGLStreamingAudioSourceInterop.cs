using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

#if !UNITY_EDITOR
using System.Collections;
using System.Runtime.InteropServices;
using System;
using System.IO;
#endif
		
public class WebGLStreamingAudioSourceInterop
{
	static Dictionary<int, WebGLStreamingAudioSourceInterop> audioSourceDict = new Dictionary<int, WebGLStreamingAudioSourceInterop>();

	public delegate void WebGLStreamingAudioDidFinish(int audio);
	public delegate void InvalidatedDelegate(WebGLStreamingAudioSourceInterop sender);

	// -------------------------------------------------------------------------------

	int 	m_instance;
	string 	m_url;
	bool 	m_playing = false;
	bool	m_destroyEnded = false;

	// -------------------------------------------------------------------------------

	// Unique ID - native audio source handler
	public int Id
	{
		get { return m_instance; }
	}

	// URL for audio source
	public string Url
	{
		get { return m_url; }
	}

	// Is it valid/initialized?
	public bool IsValid { 
		get 
		{ 
			#if (UNITY_EDITOR || !UNITY_WEBGL)
			return m_instance != 0; // instance id
			#else
			return m_instance >= 0; // indx in array
			#endif
		} 
	}

	// Is it paused by user? If you need precise isPlaying for <audio> - you should write more complete logic
	// by checking <audio> tag in WebGLStreamingAudioSourceInterop.jslib for something like 'isPlaying'
	// details: http://stackoverflow.com/questions/9437228/html5-check-if-audio-is-playing
	public bool IsPlaying {
		get
		{
			return m_playing;
		}
	}

	// [0.0f, 1.0f] range volume
	public float Volume {
		get
		{
			Assert.IsTrue(this.IsValid, "[WebGLStreamingAudioSource] invalid instance id");
			return WebGLStreamingAudioSourceGetVolume(m_instance);
		}
		set
		{
			Assert.IsTrue(this.IsValid, "[WebGLStreamingAudioSource] invalid instance id");
			WebGLStreamingAudioSourceSetVolume(m_instance, value);
		}
	}

	// [0.0f, Duration] current time
	public float CurrentTime {
		get
		{
			Assert.IsTrue(this.IsValid, "[WebGLStreamingAudioSource] invalid instance id");
			return WebGLStreamingAudioSourceGetCurrentTime(m_instance);
		}
		set
		{
			Assert.IsTrue(this.IsValid, "[WebGLStreamingAudioSource] invalid instance id");
			WebGLStreamingAudioSourceSetCurrentTime(m_instance, value);
		}
	}

	// duration of current sound
	public float Duration {
		get
		{
			Assert.IsTrue(this.IsValid, "[WebGLStreamingAudioSource] invalid instance id");
			return WebGLStreamingAudioSourceDuration(m_instance);
		}
	}

	// -------------------------------------------------------------------------------

	// callback if audio source is invalidated/destroyed
	public event InvalidatedDelegate invalidated;

	// -------------------------------------------------------------------------------
	// EDITOR EMULATION
	// -------------------------------------------------------------------------------

	#if (UNITY_EDITOR || !UNITY_WEBGL)

	static Dictionary<int, AudioSource> audioSourceEmulationDict = new Dictionary<int, AudioSource>();

	public static int WebGLStreamingAudioSourceCreate(string url, WebGLStreamingAudioDidFinish endedCallback, GameObject parent)
	{
		Assert.IsNotNull(url, "WebGLStreamingAudioSourceCreate called with url == null");

		AudioSource audioSource = parent.AddComponent<AudioSource>();
		
		audioSource.hideFlags = HideFlags.HideInInspector;

		audioSource.bypassEffects = true;
		audioSource.bypassListenerEffects = true;
		audioSource.bypassReverbZones = true;

		if (string.IsNullOrEmpty(url) == false)
		{
			WWW www = new WWW(url);

			while (www.isDone == false) {
				//wait a bit. this cause stall, but we're in emulation mode, so not a problem
			}

	        Assert.IsNull(www.error, "www <" + url + "> returns error <" + www.error + ">");
	        var audioClip = www.GetAudioClip();
			Assert.IsNotNull(audioClip, "<" + url + "> is not a valid audio");

			audioSource.clip = audioClip;
		}

		var key = audioSource.GetInstanceID();

		audioSourceEmulationDict[key] = audioSource;

		return key;
	}

	// https://webaudio.github.io/web-audio-api/#idl-def-DistanceModelType
	static float linearRolloff(float distance01, float refDistance, float rolloffFactor, float maxDistance)
	{
		float distance = Mathf.Max(refDistance, distance01 * maxDistance);
		distance = Mathf.Min(distance, maxDistance);
		return 1.0f - rolloffFactor * ((distance - refDistance) / (maxDistance - refDistance));
	}

	static float inverseRolloff(float distance01, float refDistance, float rolloffFactor, float maxDistance)
	{
		float distance = Mathf.Max(refDistance, distance01 * maxDistance);
		return refDistance / (refDistance + rolloffFactor * (distance - refDistance));
	}

	static float expRolloff(float distance01, float refDistance, float rolloffFactor, float maxDistance)
	{
		float distance = Mathf.Max(refDistance, distance01 * maxDistance);
		return Mathf.Pow(distance / refDistance, -rolloffFactor);
	}

	public static void WebGLStreamingAudioSourceSet3DPosition(int audio, float x, float y, float z)
	{
		Assert.IsTrue(audioSourceEmulationDict.ContainsKey(audio), "Unknown audio instance id: " + audio);

		// emulate <audio> rolloff curve
		AnimationCurve rolloffCurve = new AnimationCurve();

		float refDistance = 1.0f;		// https://developer.mozilla.org/en-US/docs/Web/API/PannerNode/refDistance
		float rolloffFactor = 1.0f;		// https://developer.mozilla.org/en-US/docs/Web/API/PannerNode/rolloffFactor
		float maxDistance = 500.0f;		// https://developer.mozilla.org/en-US/docs/Web/API/PannerNode/maxDistance

		for (float distance = 0.0f; distance <= 1.0f; )
		{
			//rolloffCurve.AddKey(distance, linearRolloff(distance, refDistance, rolloffFactor, maxDistance));
			rolloffCurve.AddKey(distance, inverseRolloff(distance, refDistance, rolloffFactor, maxDistance));
			//rolloffCurve.AddKey(distance, expRolloff(distance, refDistance, rolloffFactor, maxDistance));
			distance += (distance < 0.1f) ? 0.001f : 0.2f;
		}

		var source = audioSourceEmulationDict[audio];
		source.rolloffMode = AudioRolloffMode.Custom;
		source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, rolloffCurve);
		
		//source.rolloffFactor = rolloffFactor;
		source.minDistance = refDistance;
		source.maxDistance = maxDistance;

		source.spatialBlend = 1.0f;

		// <audio> doesn't support doppler. You could emulate this if you want
		source.dopplerLevel = 0.0f;
	
		source.transform.position = new Vector3(x, y, z);
	}

	public static void WebGLStreamingAudioSourcePlay(int audio)
	{
		Assert.IsTrue(audioSourceEmulationDict.ContainsKey(audio), "Unknown audio instance id: " + audio);

		audioSourceEmulationDict[audio].Play();
	}

	public static void WebGLStreamingAudioSourcePause(int audio)
	{
		Assert.IsTrue(audioSourceEmulationDict.ContainsKey(audio), "Unknown audio instance id: " + audio);

		audioSourceEmulationDict[audio].Pause();
	}

	public static void WebGLStreamingAudioSourceDestroy(int audio)
	{
		Assert.IsTrue(audioSourceEmulationDict.ContainsKey(audio), "Unknown audio instance id: " + audio);

		audioSourceEmulationDict[audio].Stop ();
		GameObject.Destroy(audioSourceEmulationDict[audio]);
		audioSourceEmulationDict.Remove(audio);
	}

	public static float WebGLStreamingAudioSourceGetCurrentTime(int audio)
	{
		Assert.IsTrue(audioSourceEmulationDict.ContainsKey(audio), "Unknown audio instance id: " + audio);

		return audioSourceEmulationDict[audio].time;
	}

	public static void WebGLStreamingAudioSourceSetCurrentTime(int audio, float newTime)
	{
		Assert.IsTrue(audioSourceEmulationDict.ContainsKey(audio), "Unknown audio instance id: " + audio);

		audioSourceEmulationDict[audio].time = newTime;
	}

	public static float WebGLStreamingAudioSourceDuration(int audio)
	{
		Assert.IsTrue(audioSourceEmulationDict.ContainsKey(audio), "Unknown audio instance id: " + audio);

		var source = audioSourceEmulationDict[audio];

		if (source == null || source.clip == null)
			return 0;

		return source.clip.length;
	}

	public static float WebGLStreamingAudioSourceGetVolume(int audio)
	{
		Assert.IsTrue(audioSourceEmulationDict.ContainsKey(audio), "Unknown audio instance id: " + audio);

		return audioSourceEmulationDict[audio].volume;
	}

	public static void WebGLStreamingAudioSourceSetVolume(int audio, float volume)
	{
		Assert.IsTrue(audioSourceEmulationDict.ContainsKey(audio), "Unknown audio instance id: " + audio);

		audioSourceEmulationDict[audio].volume = volume;
	}
	
	public static void WebGLStreamingAudioSourceSetLoop(int audio, bool loop)
	{
		Assert.IsTrue(audioSourceEmulationDict.ContainsKey(audio), "Unknown audio instance id: " + audio);

		audioSourceEmulationDict[audio].loop = loop;
	}

	#else
	[DllImport("__Internal")]
	private static extern int WebGLStreamingAudioSourceCreate(string url, WebGLStreamingAudioDidFinish callbackAction);

	[DllImport("__Internal")]
	private static extern void WebGLStreamingAudioSourceSet3DPosition(int audio, float x, float y, float z);
	
	[DllImport("__Internal")]
	private static extern void WebGLStreamingAudioSourcePlay(int audio);

	[DllImport("__Internal")]
	private static extern void WebGLStreamingAudioSourcePause(int audio);

	[DllImport("__Internal")]
	private static extern void WebGLStreamingAudioSourceDestroy(int audio);

	[DllImport("__Internal")]
	private static extern float WebGLStreamingAudioSourceGetCurrentTime(int audio);

	[DllImport("__Internal")]
	private static extern void WebGLStreamingAudioSourceSetCurrentTime(int audio, float newTime);

	[DllImport("__Internal")]
	private static extern float WebGLStreamingAudioSourceDuration(int audio);

	[DllImport("__Internal")]
	private static extern float WebGLStreamingAudioSourceGetVolume(int audio);

	[DllImport("__Internal")]
	private static extern void WebGLStreamingAudioSourceSetVolume(int audio, float volume);
	
	[DllImport("__Internal")]
	private static extern void WebGLStreamingAudioSourceSetLoop(int audio, bool loop);

	#endif

	// -------------------------------------------------------------------------------

	// Callback from .jslib for <audio> .onended event
	[AOT.MonoPInvokeCallback(typeof(WebGLStreamingAudioDidFinish))]
	public static void WebGLStreamingAudioDidFinishCallback(int audio)
	{
		Debug.Log("WebGLStreamingAudioDidFinishCallback called " + audio);

		Assert.IsTrue(audioSourceDict.ContainsKey(audio));
		if (audioSourceDict[audio].m_destroyEnded)
			audioSourceDict[audio].Invalidate();
	}

	void Invalidate()
	{
		audioSourceDict.Remove(m_instance);

		#if (UNITY_EDITOR || !UNITY_WEBGL)
		m_instance = 0;
		#else
		m_instance = -1;
		#endif

		if (invalidated != null)
			invalidated(this);
		invalidated = null;
	}

	// param @parent is used only for Editor fallback
	public WebGLStreamingAudioSourceInterop(string url, GameObject parent)
	{
		m_url = url;

#if (UNITY_EDITOR || !UNITY_WEBGL)
		m_instance = WebGLStreamingAudioSourceCreate(url, WebGLStreamingAudioDidFinishCallback, parent);
#else
		m_instance = WebGLStreamingAudioSourceCreate(url, WebGLStreamingAudioDidFinishCallback);
#endif

        Debug.LogFormat("WebGLStreamingAudioSourceInterop {0} {1}", m_instance, m_url);

        m_playing = false;

		audioSourceDict[m_instance] = this;
	}

	public void SetPosition3d(Vector3 p)
	{
		Assert.IsTrue(this.IsValid, "[WebGLStreamingAudioSource] invalid instance id");
		WebGLStreamingAudioSourceSet3DPosition(m_instance, p.x, p.y, p.z);
	}

	public void SetLoop(bool loop)
	{
		WebGLStreamingAudioSourceSetLoop(m_instance, loop);
	}
	
	public void Play()
	{
        Debug.LogFormat("Play {0} {1}", m_instance, m_url);

        Assert.IsTrue(this.IsValid, "[WebGLStreamingAudioSource] invalid instance id");
		WebGLStreamingAudioSourcePlay(m_instance);
		m_playing = true;
	}

	public void Pause()
	{
        Assert.IsTrue(this.IsValid, "[WebGLStreamingAudioSource] invalid instance id");
		WebGLStreamingAudioSourcePause(m_instance);
		m_playing = false;
	}

	public void Destroy()
	{
		if (this.IsValid)
		{
			WebGLStreamingAudioSourceDestroy(m_instance);
			Invalidate();
		}
		m_playing = false;
	}
}

