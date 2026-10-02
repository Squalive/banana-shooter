using System.Collections.Generic;
using Steamworks;
using UnityEngine;

namespace UI
{
    public class SteamVoiceChatPeer
    {
        private Queue<VoiceChatPacket> _packetsToPlay = new();

        private AudioSource m_audioSource;

        private VoiceChatPacket _currentPacket;
        private int _currentPacketSampleIndex = 0;
        int _position = 0;

        public SteamVoiceChatPeer(AudioSource audioSource)
        {
            int sampleRate = (int)SteamUser.GetVoiceOptimalSampleRate();
            int size = sampleRate * 10;// bigger size seems to help with popping a little, but i might be making that up.
 
            m_audioSource = audioSource;
            m_audioSource.loop = true;
            m_audioSource.clip = AudioClip.Create ("VoiceChat", size, 1, sampleRate, true, OnAudioRead, OnAudioSetPosition);
            m_audioSource.Play ();
        }

        
        void OnAudioRead(float[] data)
        {
            // fresh start?
            if (_currentPacket == null) {
                _currentPacket = NextPacket ();
                _currentPacketSampleIndex = 0;
            }

            int count = 0;
            while (count < data.Length) {
                // copy the right data over.
                float sample = 0;
                if (_currentPacket != null) {
                    sample = _currentPacket.DecodedData [_currentPacketSampleIndex];

                    _currentPacketSampleIndex++;
                    if (_currentPacketSampleIndex >= _currentPacket.DecodedData.Length) {
                        _currentPacket = NextPacket();
                        _currentPacketSampleIndex = 0;
                    }
                }
                data [count] = sample;
                _position++;
                count++;
            }
        }

        void OnAudioSetPosition(int newPosition)
        {
            _position = newPosition;
        }
        
        VoiceChatPacket NextPacket()
        {
            if (_packetsToPlay.Count > 0)
            {
                return _packetsToPlay.Dequeue();
            }
            return null;
        }

        public void OnNewSample(VoiceChatPacket newPacket)
        {
            newPacket.Decode();

            if (!newPacket.IsSilence)
            {
                _packetsToPlay.Enqueue(newPacket);
            }
        }
    }

    public class VoiceChatPacket
    {
        private readonly uint _length;
        private readonly byte[] _data;

        public readonly bool IsSilence = false;
        
        public float[] DecodedData = null;

        public int LengthInSamples;

        public VoiceChatPacket(uint length, byte[] data)
        {
            _length = length;
            _data = data;
        }

        public void Decode()
        {
            DecodedData = new float[_length / 2];// todo :: pool this array?
            for (int i = 0; i < DecodedData.Length; i++) {
                float value = System.BitConverter.ToInt16 (_data, i * 2);
                DecodedData [i] = value / short.MaxValue;
            }
            LengthInSamples = DecodedData.Length;
        }
    }
}