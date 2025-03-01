#ifndef AUDIO_CONTEXT_H
#define AUDIO_CONTEXT_H

#include "miniaudio.h"
#include "sound_type.h"

#include <mutex>
#include <cstring>
#include <string>
#include <vector>
#include <map>
#include <functional>
#include <thread>
#include <chrono>

#ifdef _WIN32
#include <Windows.h>
#endif

class AudioContext {
public:
	AudioContext();
	~AudioContext();

	void Close();

	std::map<int, ma_device_info> GetPlaybackDevices(unsigned int api);
	std::map<int, ma_device_info> GetCaptureDevices(unsigned int api);
	std::map<int, std::string> GetAvailableAudioApis();
	std::string GetDeviceId(const ma_device_id& deviceId, unsigned int api, const std::string& deviceName);
	void SetAudioApi(unsigned int api);
	void SetCaptureDevice(const std::string deviceName);
	void SetPlaybackDevice(const std::string deviceName);

	bool StartRecording(const std::string deviceName);
	std::vector<uint8_t> StopRecording();
	static void MicrophoneCallback(ma_device* pDevice, void* pOutput, const void* pInput, ma_uint32 frameCount);

	bool StartBufferPlayback(void* buffer, size_t bufferSize);
	bool StopBufferPlayback();
	static void AddSilence(std::vector<uint8_t>& audioBuffer, size_t sampleRate, size_t durationInSeconds);

	bool StartPlayback(const std::string deviceName);
	bool StopPlayback();
	static void PlaybackCallback(ma_device* pDevice, void* pOutput, const void* pInput, ma_uint32 frameCount);

	bool GetDeviceFromName(const std::string& deviceName, ma_device_id &deviceId, bool isInput);
	void DestroyDevices();

	void EmitSound(SoundType sound);

	const int frameLengthMs = 20;
	const int sampleRateHz = 48000;
	const int frameSizeSamples = (sampleRateHz * frameLengthMs / 1000);

private:
	std::string playbackDeviceName;
	std::string captureDeviceName;
	unsigned int audioApi;
	bool captureInitialized;
	bool playbackInitialized;
	bool bufferPlaybackInitialized;
	ma_context context;
	ma_device playbackDevice = {};
	ma_device captureDevice = {};
	ma_device bufferPlaybackDevice = {};

	size_t playbackPos = 0;
	std::mutex audioMutex;
	std::vector<uint8_t> audioBuffer;
};

#endif // !