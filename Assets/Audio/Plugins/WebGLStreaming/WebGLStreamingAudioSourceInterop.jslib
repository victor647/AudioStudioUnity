var LibraryWebGLStreamingAudioSource = {
$audioInstances: [],

WebGLStreamingAudioSourceCreate: function(url, callbackAction)
{
	console.log("WebGLStreamingAudioSourceCreate  " + callbackAction);

	// find unused <audio>
	var instanceId = -1;

	var arrayLength = audioInstances.length;
	for (var i = 0; i < arrayLength; i++)
	{
		if (audioInstances[i].__free2alloc === true)
		{
			instanceId = i;
			break;
		}
	}

	// need to create a new one?
	if (instanceId === -1)
	{
		var audioTag = document.createElement('audio');
		document.body.appendChild(audioTag);
		instanceId = audioInstances.push(audioTag) - 1;
		audioTag.id = 'audio_' + instanceId;
	}
	
	var str = Pointer_stringify(url);
	
	var audio = audioInstances[instanceId];
	audio.src = str;
	audio.volume = 1;
	audio.onended = callbackAction;
	audio.__free2alloc = false;

	console.log("Created " + instanceId + " for src = " + str);
	
	audio.addEventListener('error', function failed(e) {
	   // audio playback failed - show a message saying why
	   // to get the source of the audio element use $(this).src
	 	switch (e.target.error.code)
	 	{
			case e.target.error.MEDIA_ERR_ABORTED:
				//alert('[' + audio.src + '] You aborted the video playback.');
			break;
			case e.target.error.MEDIA_ERR_NETWORK:
				console.log('[' + audio.src + '] A network error caused the audio download to fail.');
			break;
			case e.target.error.MEDIA_ERR_DECODE:
				console.log('[' + audio.src + '] The audio playback was aborted due to a corruption problem or because the video used features your browser did not support.');
			break;
			case e.target.error.MEDIA_ERR_SRC_NOT_SUPPORTED:
				console.log('[' + audio.src + '] The video audio not be loaded, either because the server or network failed or because the format is not supported.');
			break;
			default:
				console.log('[' + audio.src + '] An unknown error occurred.');
			break;
		}
 	}, true);
	
	return instanceId;
},

WebGLStreamingAudioSourceSet3DPosition: function(audio, x, y, z)
{
	if (audioInstances[audio].__webAudioPanningNode === undefined)
	{
		var ctx = WEBAudio.audioContext;
		var source = ctx.createMediaElementSource(audioInstances[audio]);
		var pannerNode = ctx.createPanner();
		var gainNode = ctx.createGain();
		
		gainNode.gain.value = 1;
		
		source.connect(pannerNode);
		pannerNode.connect(gainNode);
		
		gainNode.connect(ctx.destination);
		
		pannerNode.refDistance = 1.0;
		pannerNode.rolloffFactor = 1.0;
		pannerNode.maxDistance = 500.0;
		pannerNode.distanceModel = 'inverse';
		pannerNode.coneInnerAngle = 360.0;
		pannerNode.coneOuterAngle = 0.0;
		pannerNode.coneOuterGain = 0.0;

		//pannerNode.panningModel = 'HRTF';

		audioInstances[audio].__webAudioSource = source;
		audioInstances[audio].__webAudioPanningNode = pannerNode;
		audioInstances[audio].__webAudioGainNode = gainNode;
	}
	
	audioInstances[audio].__webAudioPanningNode.setPosition(x, y, z);
},

WebGLStreamingAudioSourcePlay: function(audio)
{
	console.log("WebGLStreamingAudioSourcePlay "+audio);
	var audioTag = audioInstances[audio];
	if(!audioTag.paused)
		return;
	var playPromise = audioTag.play();
	if (playPromise != undefined) 
	{
		playPromise.catch(function(err){
			console.log("WebGLStreamingAudioSourcePlay err: "+err.message);
		});
	}
},

WebGLStreamingAudioSourcePause: function(audio)
{
	console.log("WebGLStreamingAudioSourcePause "+audio);
	audioInstances[audio].pause();
},

WebGLStreamingAudioSourceDestroy: function(audio)
{
	audioInstances[audio].pause();
	audioInstances[audio].__free2alloc = true;
	console.log("Destroyed " + audio);
},

WebGLStreamingAudioSourceGetCurrentTime: function(audio)
{
	return audioInstances[audio].currentTime;
},

WebGLStreamingAudioSourceSetCurrentTime: function(audio, newTime)
{
	audioInstances[audio].currentTime = newTime;
},

WebGLStreamingAudioSourceDuration: function(audio)
{
	var duration = audioInstances[audio].duration;
	if(isNaN(duration))
		return 1;			// 预估值
	else
		return duration;
},

WebGLStreamingAudioSourceGetVolume: function(audio)
{
	return audioInstances[audio].volume;
},

WebGLStreamingAudioSourceSetVolume: function(audio, newVolume)
{
	//console.log("WebGLStreamingAudioSourceSetVolume "+audio+" "+newVolume);
	audioInstances[audio].volume = newVolume;
},

WebGLStreamingAudioSourceSetLoop: function(audio, loop)
{
	audioInstances[audio].loop = loop;
},

};
autoAddDeps(LibraryWebGLStreamingAudioSource, '$audioInstances');
mergeInto(LibraryManager.library, LibraryWebGLStreamingAudioSource);