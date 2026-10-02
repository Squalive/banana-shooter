
using Manager;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

namespace Audio
{
    public class AudioManager : MonoBehaviour
    {
        public Sound[] sounds;

		public Sound[] footsteps;

		public Sound[] jumps;

		public Sound  startSlide;

		public Sound hitGround,hitPlayer;

		private float desiredFreq = 500f;

		private float velFreq;

		private float freqSpeed = 0.2f;

		public bool muted;
		

		private static AudioManager _instance;

		internal static AudioManager Instance
		{
			get => _instance;
			private set
			{
				if (_instance == null)
					_instance = value;
				else if (_instance != value)
				{
					Debug.Log($"{nameof(AudioManager)} instance already exists, destroying object!");
					Destroy(value);
				}
			}
		}

		public AudioMixerGroup soundEffect,uiSound;

		[SerializeField] private AnimationCurve audioCurve;
		private void Awake()
		{
			Instance = this;
			Sound[] array = sounds;
			foreach (Sound sound in array)
			{
				if (sound.clip != null)
				{
					sound.source = gameObject.AddComponent<AudioSource>();
					sound.source.outputAudioMixerGroup =sound.name=="Button"? uiSound: soundEffect;
					sound.source.clip = sound.clip;
					sound.source.loop = sound.loop;
					sound.source.volume = sound.volume;
					sound.source.pitch = sound.pitch;
					sound.source.bypassListenerEffects = sound.bypass;
					sound.source.bypassReverbZones = sound.bypassReverb;
				}
			}
			array = footsteps;
			foreach (Sound sound2 in array)
			{
				if (sound2.clip != null)
				{
					sound2.source = base.gameObject.AddComponent<AudioSource>();
					sound2.source.outputAudioMixerGroup = soundEffect;
					sound2.source.clip = sound2.clip;
					sound2.source.loop = sound2.loop;
					sound2.source.volume = sound2.volume;
					sound2.source.pitch = sound2.pitch;
					sound2.source.bypassListenerEffects = sound2.bypass;
					sound2.source.bypassReverbZones = sound2.bypassReverb;
				}
			}
			array = jumps;
			foreach (Sound sound4 in array)
			{
				if (sound4.clip != null)
				{
					sound4.source = base.gameObject.AddComponent<AudioSource>();
					sound4.source.outputAudioMixerGroup = soundEffect;
					sound4.source.clip = sound4.clip;
					sound4.source.loop = sound4.loop;
					sound4.source.volume = sound4.volume;
					sound4.source.pitch = sound4.pitch;
					sound4.source.bypassListenerEffects = sound4.bypass;
					sound4.source.bypassReverbZones = sound4.bypassReverb;
				}
			}

			if (startSlide.clip != null)
			{
				startSlide.source = gameObject.AddComponent<AudioSource>();
				startSlide.source.outputAudioMixerGroup = soundEffect;
				startSlide.source.clip = startSlide.clip;
				startSlide.source.loop = startSlide.loop;
				startSlide.source.volume = startSlide.volume;
				startSlide.source.pitch = startSlide.pitch;
				startSlide.source.bypassListenerEffects = startSlide.bypass;
				startSlide.source.bypassReverbZones = startSlide.bypassReverb;
			}

			if (hitGround.clip != null)
			{
				hitGround.source = gameObject.AddComponent<AudioSource>();
				hitGround.source.outputAudioMixerGroup = soundEffect;
				hitGround.source.clip = hitGround.clip;
				hitGround.source.loop = hitGround.loop;
				hitGround.source.volume = hitGround.volume;
				hitGround.source.pitch = hitGround.pitch;
				hitGround.source.bypassListenerEffects = hitGround.bypass;
				hitGround.source.bypassReverbZones = hitGround.bypassReverb;
			}

			if (hitPlayer.clip != null)
			{
				hitPlayer.source = gameObject.AddComponent<AudioSource>();
				hitPlayer.source.outputAudioMixerGroup = soundEffect;
				hitPlayer.source.clip = hitPlayer.clip;
				hitPlayer.source.loop = hitPlayer.loop;
				hitPlayer.source.volume = hitPlayer.volume;
				hitPlayer.source.pitch = hitPlayer.pitch;
				hitPlayer.source.bypassListenerEffects = hitPlayer.bypass;
				hitPlayer.source.bypassReverbZones = hitPlayer.bypassReverb;
			}

			gunShootSource = gameObject.AddComponent<AudioSource>();
			gunShootSource.outputAudioMixerGroup = soundEffect;
			gunShootSource.loop = false;
			gunShootSource.volume = gunVolume;
			gunShootSource.pitch = 1;
			
			gunReloadSource = gameObject.AddComponent<AudioSource>();
			gunReloadSource.outputAudioMixerGroup = soundEffect;
			gunReloadSource.loop = false;
			gunReloadSource.volume = gunVolume;
			gunReloadSource.pitch = 1;
			
			gunResetSource = gameObject.AddComponent<AudioSource>();
			gunResetSource.outputAudioMixerGroup = soundEffect;
			gunResetSource.loop = false;
			gunResetSource.volume = gunVolume;
			gunResetSource.pitch = 1;
		}

		public float gunVolume = .7f;
		private AudioSource gunShootSource,gunReloadSource,gunResetSource;
		

		public void MuteSounds(bool b)
		{
			if (b)
			{
				AudioListener.volume = 0f;
			}
			else
			{
				AudioListener.volume = 1f;
			}
			muted = b;
		}

		public void PlayButton()
		{
			if (muted)
			{
				return;
			}
			Sound[] array = sounds;
			foreach (Sound sound in array)
			{
				if (sound.name == "Button")
				{
					if (sound.source != null)
					{
						sound.source.pitch = 0.8f + Random.Range(-0.03f, 0.03f);
						sound.source.Play();
					}
					break;
				}
			}
		}

		public void PlayPitched(string n, float v)
		{
			if (muted)
			{
				return;
			}
			Sound[] array = sounds;
			foreach (Sound sound in array)
			{
				if (sound.name == n )
				{
					if (sound.source != null)
					{
						sound.source.pitch = 1f + Random.Range(-v, v);
					}
					break;
				}
			}
			Play(n);
		}

		public void MuteMusic()
		{
			Sound[] array = sounds;
			foreach (Sound sound in array)
			{
				if (sound.name == "Song")
				{
					sound.source.volume = 0f;
					break;
				}
			}
		}

		[SerializeField] private AudioMixer master;
		private const string Master = "MasterVolume";
		private const string UI = "UIVolume";
		private const string Ambience = "AmbienceVolume";
		private const string SoundEffect = "SoundEffectVolume";
		public void SetMasterVolume(float v)
		{
			v = Mathf.Clamp(v, 0.001f, 1f);
			master.SetFloat(Master,Mathf.Log10(v)*20 );
		}
		public void SetUIVolume(float v)
		{
			v = Mathf.Clamp(v, 0.001f, 1f);
			master.SetFloat(UI,Mathf.Log10(v)*20 );
		}
		public void SetAmbienceVolume(float v)
		{
			v = Mathf.Clamp(v, 0.001f, 1f);
			master.SetFloat(Ambience,Mathf.Log10(v)*20 );
		}
		public void SetSoundEffectVolume(float v)
		{
			v = Mathf.Clamp(v, 0.001f, 1f);
			master.SetFloat(SoundEffect,Mathf.Log10(v)*20 );
		}
		public void UnmuteMusic()
		{
			Sound[] array = sounds;
			foreach (Sound sound in array)
			{
				if (sound.name == "Song")
				{
					sound.source.volume = 1.15f;
					break;
				}
			}
		}

		public void Play(string n)
		{
			if (muted && n != "Song")
			{
				return;
			}

			if (n == "HitMarker")
			{
				Play("HItMarker2");
				Play("HitMarker3");
			}
			Sound[] array = sounds;
			foreach (Sound sound in array)
			{
				if (sound.name == n)
				{
					if (sound.source != null)
					{
						sound.source.Play();
					}
					break;
				}
			}
		}

		// public void Stop(string n)
		// {
		// 	
		// }

		public void PlayFootStep()
		{
			if (!muted)
			{
				int num = Random.Range(0, footsteps.Length - 1);
				var sound = footsteps[num];
				if (sound.source != null)
				{
					sound.source.Play();
				}
			}
		}

		public void PlayJump()
		{
			if (!muted)
			{
				int num = Random.Range(0, jumps.Length - 1);
				Sound sound = jumps[num];
				if (sound.source != null) 
				{
					sound.source.Play();
				}
			}
		}

		public void Stop(string n)
		{
			Sound[] array = sounds;
			foreach (Sound sound in array)
			{
				if (sound.name == n)
				{
					sound.source.Stop();
					break;
				}
			}
		}

		public void SetFreq(float freq)
		{
			desiredFreq = freq;
		}
		
		public void PlayStartSlide()
		{
			if (!muted && startSlide.source != null)
			{
				startSlide.source.Play();
			}
		}
		

		public void PlayHitGround()
		{
			if (!muted && hitGround.source != null) 
			{
				hitGround.source.Play();
			}
		}
		
		public void PlayHitPlayer()
		{
			if (!muted && hitPlayer.source != null)
			{
				hitPlayer.source?.Play();
			}
		}

		public void PlayGunShoot(AudioClip clip)
		{
			if (!muted && clip!=null && gunShootSource != null)
			{
				gunShootSource?.PlayOneShot(clip);
			}
		}
		
		public void PlayGunReload(AudioClip clip,float pitch = 1f)
		{
			if (!muted  && clip!=null&& gunReloadSource!=null)
			{
				gunReloadSource.pitch =pitch;
				gunReloadSource.PlayOneShot(clip);
			}
		}
		
		public void PlayGunReset(AudioClip clip)
		{
			if (!muted  && clip!=null&& gunResetSource!=null)
			{
				gunResetSource.PlayOneShot(clip);
			}
		}

		public void StopGunShoot()
		{
			gunShootSource.Stop();
		}
		public void StopGunReload()
		{
			gunReloadSource.Stop();
		}
		public void StopGunReset()
		{
			gunResetSource.Stop();
		}

		AudioClip GetSound(string n)
		{
			Sound[] array = sounds;
			foreach (Sound sound in array)
			{
				if (sound.name == n)
				{
					return sound.clip;
				}
			}

			return null;
		}

		public GameObject soundEffect3D;
		public void SoundEffect3D(string n, Vector3 point,float volume=1f)
		{
			if (n == "Explosion")
			{
				GameManager.Instance.CameraShake3D(4,4,0.1f,1f,point);
			}
			// PlayClipAtPoint(GetSound(n), point, volume, minDistance,5f, soundEffect);
			
			AudioSource source = Instantiate(soundEffect3D,point,Quaternion.identity).GetComponent<AudioSource>();
			source.clip = n == "start_slide" ? startSlide.clip : GetSound(n);
			source.volume = volume;
			source.Play();
			if (source.clip != null)
			{
				Destroy(source.gameObject, source.clip.length * (Time.timeScale < 0.009999999776482582 ? 0.01f : Time.timeScale));
			}
	    }
		
		public void SoundEffect3D(string n, Vector3 point,float volume,float minDistance)
		{
			PlayClipAtPoint(GetSound(n), point, volume, minDistance, soundEffect);
		}
		
		public void SoundEffect3DPitch(string n, Vector3 point,float offset)
		{
			if (n == "Explosion")
			{
				GameManager.Instance.CameraShake3D(4,4,0.1f,1f,point);
			}
			AudioSource source = Instantiate(soundEffect3D,point,Quaternion.identity).GetComponent<AudioSource>();
			source.clip = GetSound(n);
			source.pitch = 1f + Random.Range(-offset, offset);
			source.Play();
			if (source.clip != null)
			{
				Destroy(source.gameObject,source.clip.length);
			}
		}
		
		private static void PlayClipAtPoint(AudioClip clip, Vector3 position, [UnityEngine.Internal.DefaultValue("1.0F")] float volume,float minDistance, AudioMixerGroup group=null)
		{
			GameObject gameObject = new GameObject("One shot audio");
			gameObject.transform.position = position;
			AudioSource audioSource = (AudioSource) gameObject.AddComponent(typeof (AudioSource));
			if (group != null)
			{
				audioSource.outputAudioMixerGroup = group;
			}
			audioSource.clip = clip;
			audioSource.spatialBlend = 1f;
			audioSource.volume = volume;
			audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
			audioSource.minDistance = minDistance;
			audioSource.playOnAwake = false;
			if (clip)
			{
				Destroy(gameObject, clip.length * (Time.timeScale < 0.009999999776482582 ? 0.01f : Time.timeScale));
			}
			audioSource.Play();
		}
    }
}

