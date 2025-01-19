#include "audio_context.h"
#include "native_audio.h"
#include "wav_data.h"

AudioContext::AudioContext() :
	playbackDeviceName(""),
	captureDeviceName(""),
	audioApi(0),
	captureInitialized(false),
	playbackInitialized(false),
	bufferPlaybackInitialized(false)
{
	ma_context_config contextConfig = ma_context_config_init();
	contextConfig.threadPriority = ma_thread_priority_normal;
	contextConfig.jack.pClientName = "org.vatsim.vatis";
	contextConfig.pulse.pApplicationName = "org.vatsim.vatis";
	ma_context_init(NULL, 0, &contextConfig, &context);
}

AudioContext::~AudioContext()
{

}

void AudioContext::Close()
{
	if (captureInitialized) {
		ma_device_uninit(&captureDevice);
		captureInitialized = false;
	}

	if (playbackInitialized) {
		ma_device_uninit(&playbackDevice);
		playbackInitialized = false;
	}

	ma_context_uninit(&context);
}

std::map<int, ma_device_info> AudioContext::GetPlaybackDevices(unsigned int api)
{
	std::map<int, ma_device_info> deviceList;

	ma_device_info* devices;
	ma_uint32 deviceCount;

	ma_result result = ma_context_get_devices(&context, &devices, &deviceCount, NULL, NULL);
	if (result == MA_SUCCESS) {
		for (ma_uint32 i = 0; i < deviceCount; i++) {
			deviceList.emplace(i, devices[i]);
		}
	}

	return deviceList;
}

std::map<int, ma_device_info> AudioContext::GetCaptureDevices(unsigned int api)
{
	std::map<int, ma_device_info> deviceList;

	ma_device_info* devices;
	ma_uint32 deviceCount;

	ma_result result = ma_context_get_devices(&context, NULL, NULL, &devices, &deviceCount);
	if (result == MA_SUCCESS) {
		for (ma_uint32 i = 0; i < deviceCount; i++) {
			deviceList.emplace(i, devices[i]);
		}
	}

	return deviceList;
}

std::map<int, std::string> AudioContext::GetAvailableAudioApis()
{
	ma_backend enabledBackends[MA_BACKEND_COUNT];
	size_t enabledBackendCount;

	std::map<int, std::string> apiList;

	ma_result result = ma_get_enabled_backends(enabledBackends, MA_BACKEND_COUNT, &enabledBackendCount);
	if (result != MA_SUCCESS) {
		return apiList;
	}

	for (int i = 0; i < enabledBackendCount; i++) {
		apiList.emplace(static_cast<int>(enabledBackends[i]), ma_get_backend_name(enabledBackends[i]));
	}

	return apiList;
}

std::string AudioContext::GetDeviceId(const ma_device_id& deviceId, unsigned int api, const std::string& deviceName)
{
	if (api >= MA_BACKEND_COUNT || api == -1) {
		// Unknown API, return device name
		return deviceName;
	}

	const auto ma_api = static_cast<ma_backend>(api);
	if (ma_api == ma_backend_wasapi) {
#ifdef WIN32
		// Determine the length of the converted string
		int length = WideCharToMultiByte(CP_UTF8, 0, deviceId.wasapi, -1, nullptr, 0, nullptr, nullptr);
		if (length == 0) {
			// Conversion failed
			return "";
		}

		// Allocate a buffer to hold the converted string
		std::string result(length - 1, '\0'); // Length includes the null terminator, which we don't need

		// Perform the conversion
		WideCharToMultiByte(CP_UTF8, 0, deviceId.wasapi, -1, &result[0], length, nullptr, nullptr);
		return result;
#endif // WIN32
	}

	if (ma_api == ma_backend_dsound) {
		return deviceName;
	}

	if (ma_api == ma_backend_winmm) {
		return std::to_string(deviceId.winmm);
	}

	if (ma_api == ma_backend_coreaudio) {
		return deviceId.coreaudio;
	}

	if (ma_api == ma_backend_sndio) {
		return deviceId.sndio;
	}

	if (ma_api == ma_backend_audio4) {
		return deviceId.audio4;
	}

	if (ma_api == ma_backend_oss) {
		return deviceId.oss;
	}

	if (ma_api == ma_backend_pulseaudio) {
		return deviceId.pulse;
	}

	if (ma_api == ma_backend_alsa) {
		return deviceId.alsa;
	}

	if (ma_api == ma_backend_jack) {
		return std::to_string(deviceId.jack);
	}

	if (ma_api == ma_backend_aaudio) {
		return std::to_string(deviceId.aaudio);
	}

	if (ma_api == ma_backend_opensl) {
		return std::to_string(deviceId.opensl);
	}

	if (ma_api == ma_backend_webaudio) {
		return deviceId.webaudio;
	}

	if (ma_api == ma_backend_null) {
		return std::to_string(deviceId.nullbackend);
	}

	return deviceName;
}

void AudioContext::SetAudioApi(unsigned int api)
{
	std::lock_guard<std::mutex> lock(audioMutex);
	audioApi = api;
}

void AudioContext::SetCaptureDevice(const std::string deviceName)
{
	std::lock_guard<std::mutex> lock(audioMutex);
	captureDeviceName = deviceName;
}

void AudioContext::SetPlaybackDevice(const std::string deviceName)
{
	std::lock_guard<std::mutex> lock(audioMutex);
	playbackDeviceName = deviceName;
}

bool AudioContext::StartRecording(const std::string deviceName)
{
	std::lock_guard<std::mutex> lock(audioMutex);
	audioBuffer.clear();

	if (!captureInitialized) {
		ma_device_id deviceId;
		if (!GetDeviceFromName(deviceName, deviceId, true)) {
			return false;
		}
		ma_device_config deviceConfig = ma_device_config_init(ma_device_type_capture);
		deviceConfig.capture.pDeviceID = &deviceId;
		deviceConfig.capture.format = ma_format_s16;
		deviceConfig.capture.channels = 1;
		deviceConfig.sampleRate = sampleRateHz;
		deviceConfig.periodSizeInFrames = frameSizeSamples;
		deviceConfig.dataCallback = MicrophoneCallback;
		deviceConfig.pUserData = this;

		if (ma_device_init(&context, &deviceConfig, &captureDevice) != MA_SUCCESS) {
			return false;
		}

		captureInitialized = true;
	}

	if (ma_device_start(&captureDevice) != MA_SUCCESS) {
		ma_device_uninit(&captureDevice);
		return false;
	}

	return true;
}

std::vector<uint8_t> AudioContext::StopRecording()
{
	std::lock_guard<std::mutex> lock(audioMutex);
	ma_device_stop(&captureDevice);
	return audioBuffer;
}

void AudioContext::MicrophoneCallback(ma_device* pDevice, void* pOutput, const void* pInput, ma_uint32 frameCount)
{
	AudioContext* pContext = static_cast<AudioContext*>(pDevice->pUserData);
	const int16_t* inputData = static_cast<const int16_t*>(pInput);
	size_t byteCount = static_cast<unsigned long long>(frameCount) * pDevice->capture.channels * sizeof(int16_t);
	{
		std::lock_guard<std::mutex> lock(pContext->audioMutex);
		pContext->audioBuffer.insert(pContext->audioBuffer.end(), reinterpret_cast<const uint8_t*>(inputData), reinterpret_cast<const uint8_t*>(inputData) + byteCount);
	}
}

bool AudioContext::StartBufferPlayback(void *buffer, size_t bufferSize)
{
    {
        std::lock_guard<std::mutex> lock(audioMutex);
        playbackPos = 0;

        // Clear and resize the audio buffer
        audioBuffer.clear();
        audioBuffer.resize(bufferSize);

        // Copy provided buffer to internal buffer
        std::memcpy(audioBuffer.data(), buffer, bufferSize);

        // Add silence to the end of playback
        AddSilence(audioBuffer, sampleRateHz, 3);
    }

    if (!bufferPlaybackInitialized) {
        ma_device_config deviceConfig = ma_device_config_init(ma_device_type_playback);
		deviceConfig.playback.pDeviceID = nullptr;
		deviceConfig.playback.format = ma_format_s16;
		deviceConfig.playback.channels = 1;
		deviceConfig.sampleRate = sampleRateHz;
		deviceConfig.periodSizeInFrames = frameSizeSamples;
		deviceConfig.playback.shareMode = ma_share_mode_shared;
		deviceConfig.dataCallback = PlaybackCallback;
		deviceConfig.pUserData = this;

        if (ma_device_init(&context, &deviceConfig, &bufferPlaybackDevice) != MA_SUCCESS) {
            ma_device_uninit(&bufferPlaybackDevice);
            return false;
        }

        bufferPlaybackInitialized = true;
    }

    if (ma_device_start(&bufferPlaybackDevice) != MA_SUCCESS) {
        return false;
    }

    return true;
}

bool AudioContext::StopBufferPlayback()
{
	std::lock_guard<std::mutex> lock(audioMutex);
	ma_device_stop(&bufferPlaybackDevice);
	playbackPos = 0;
	return true;
}

bool AudioContext::StartPlayback(const std::string deviceName)
{
	std::lock_guard<std::mutex> lock(audioMutex);
	playbackPos = 0;

	if (!playbackInitialized) {
		ma_device_id deviceId;
		if (!GetDeviceFromName(deviceName, deviceId, false)) {
			return false;
		}
		ma_device_config deviceConfig = ma_device_config_init(ma_device_type_playback);
		deviceConfig.playback.pDeviceID = &deviceId;
		deviceConfig.playback.format = ma_format_s16;
		deviceConfig.playback.channels = 1;
		deviceConfig.sampleRate = sampleRateHz;
		deviceConfig.periodSizeInFrames = frameSizeSamples;
		deviceConfig.playback.shareMode = ma_share_mode_shared;
		deviceConfig.dataCallback = PlaybackCallback;
		deviceConfig.pUserData = this;

		if (ma_device_init(&context, &deviceConfig, &playbackDevice) != MA_SUCCESS) {
			ma_device_uninit(&playbackDevice);
			return false;
		}

		playbackInitialized = true;
	}

	if (ma_device_start(&playbackDevice) != MA_SUCCESS) {
		return false;
	}

	return true;
}

bool AudioContext::StopPlayback() {
	std::lock_guard<std::mutex> lock(audioMutex);
	ma_device_stop(&playbackDevice);
	playbackPos = 0;
	return true;
}

void AudioContext::PlaybackCallback(ma_device* pDevice, void* pOutput, const void* pInput, ma_uint32 frameCount)
{
    AudioContext* pContext = static_cast<AudioContext*>(pDevice->pUserData);
    uint8_t* outputData = static_cast<uint8_t*>(pOutput);
    size_t byteCount = static_cast<size_t>(frameCount) * pDevice->playback.channels * sizeof(int16_t);

    size_t playbackPosCopy;
    size_t remainingBytesCopy;

    {
        std::lock_guard<std::mutex> lock(pContext->audioMutex);
        playbackPosCopy = pContext->playbackPos;
        remainingBytesCopy = pContext->audioBuffer.size() - playbackPosCopy;
    }

    if (playbackPosCopy >= pContext->audioBuffer.size()) {
        playbackPosCopy = 0;
    }

    if (byteCount <= remainingBytesCopy) {
        std::memcpy(outputData, &pContext->audioBuffer[playbackPosCopy], byteCount);
        playbackPosCopy += byteCount;
    } else {
        std::memcpy(outputData, &pContext->audioBuffer[playbackPosCopy], remainingBytesCopy);
        std::memset(outputData + remainingBytesCopy, 0, byteCount - remainingBytesCopy);
        playbackPosCopy = 0;
    }

    {
        std::lock_guard<std::mutex> lock(pContext->audioMutex);
        pContext->playbackPos = playbackPosCopy;
    }
}

bool AudioContext::GetDeviceFromName(const std::string& deviceName, ma_device_id& deviceId, bool isInput)
{
	const auto& devices = isInput ? GetCaptureDevices(0) : GetPlaybackDevices(0);
	if (!devices.empty()) {
		for (const auto& device : devices) {
			if (device.second.name == deviceName) {
				deviceId = device.second.id;
				return true;
			}
		}
	}
	return false;
}

void AudioContext::DestroyDevices()
{
	ma_device_uninit(&captureDevice);
	captureInitialized = false;

	ma_device_uninit(&playbackDevice);
	playbackInitialized = false;
}

void AudioContext::EmitSound(SoundType soundType)
{
	std::thread([soundType]() {
		ma_engine engine;
		ma_result result = ma_engine_init(NULL, &engine);
		if (result != MA_SUCCESS) {
			return;
		}

		const unsigned char* soundData = nullptr;
		unsigned int soundDataSize = 0;

		switch (soundType) {
		case SoundType::Error:
			soundData = error_sound;
			soundDataSize = error_sound_size;
			break;
		case SoundType::Notification:
			soundData = notification_sound;
			soundDataSize = notification_sound_size;
			break;
		default:
			ma_engine_uninit(&engine);
			return;
		}

		ma_decoder decoder;
		result = ma_decoder_init_memory(soundData, soundDataSize, NULL, &decoder);
		if (result != MA_SUCCESS) {
			ma_engine_uninit(&engine);
			return;
		}

		ma_sound sound;
		result = ma_sound_init_from_data_source(&engine, &decoder, 0, NULL, &sound);
		if (result != MA_SUCCESS) {
			ma_decoder_uninit(&decoder);
			ma_engine_uninit(&engine);
			return;
		}

		ma_sound_start(&sound);

		while (!ma_sound_at_end(&sound)) {
			std::this_thread::sleep_for(std::chrono::milliseconds(100));
		}
		std::this_thread::sleep_for(std::chrono::milliseconds(100));

		ma_sound_stop(&sound);
		ma_sound_uninit(&sound);
		ma_decoder_uninit(&decoder);
		ma_engine_uninit(&engine);
	}).detach();
}

void AudioContext::AddSilence(std::vector<uint8_t>& buffer, size_t sampleRate, size_t durationInSeconds)
{
    size_t numSamples = sampleRate * durationInSeconds;
    buffer.resize(buffer.size() + numSamples);
    std::memset(buffer.data() + buffer.size() - numSamples, 0, numSamples);
}
