#include "native_audio.h"
#include "audio_context.h"

#include <map>
#include <vector>
#include <string>

struct AudioClientHandle_ {
	AudioContext* impl = nullptr;

	AudioClientHandle_() {
		impl = new AudioContext();
	}

	~AudioClientHandle_() {
		impl->Close();
	}
};

AUDIO_API AudioClientHandle Initialize()
{
	return new AudioClientHandle_();
}

AUDIO_API void Destroy(AudioClientHandle handle)
{
	delete handle;
}

AUDIO_API void GetAudioApis(AudioClientHandle handle, AudioApisCallback callback)
{
	typedef std::map<int, std::string> MapType;
	auto m = handle->impl->GetAvailableAudioApis();
	for (MapType::iterator it = m.begin(); it != m.end(); ++it) {
		callback(it->first, it->second.c_str());
	}
}

AUDIO_API void SetAudioApi(AudioClientHandle handle, unsigned int audioApi)
{
	handle->impl->SetAudioApi(audioApi);
}

AUDIO_API void GetCaptureDevices(AudioClientHandle handle, unsigned int audioApi, AudioInterfaceCallback callback)
{
	auto devices = handle->impl->GetCaptureDevices(audioApi);
	for (auto& device : devices) {
		auto deviceName = handle->impl->GetDeviceId(device.second.id, audioApi, device.second.name);
		callback(deviceName.c_str(), device.second.name, device.second.isDefault);
	}
}

AUDIO_API void SetCaptureDevice(AudioClientHandle handle, char* deviceName)
{
	handle->impl->SetCaptureDevice(deviceName);
}

AUDIO_API void GetPlaybackDevices(AudioClientHandle handle, unsigned int audioApi, AudioInterfaceCallback callback)
{
	auto devices = handle->impl->GetPlaybackDevices(audioApi);
	for (auto& device : devices) {
		auto deviceName = handle->impl->GetDeviceId(device.second.id, audioApi, device.second.name);
		callback(deviceName.c_str(), device.second.name, device.second.isDefault);
	}
}

AUDIO_API void SetPlaybackDevice(AudioClientHandle handle, char* deviceName)
{
	handle->impl->SetPlaybackDevice(deviceName);
}

AUDIO_API bool StartRecording(AudioClientHandle handle, char* deviceName)
{
	return handle->impl->StartRecording(deviceName);
}

AUDIO_API void StopRecording(AudioClientHandle handle, AudioDataCallback callback)
{
	auto data = handle->impl->StopRecording();
	callback(data.data(), data.size());
}

AUDIO_API bool StartBufferPlayback(AudioClientHandle handle, void *buffer, size_t bufferSize)
{
    return  handle->impl->StartBufferPlayback(buffer, bufferSize);
}

AUDIO_API bool StopBufferPlayback(AudioClientHandle handle)
{
    return handle->impl->StopBufferPlayback();
}

AUDIO_API bool StartPlayback(AudioClientHandle handle, char* deviceName)
{
	return handle->impl->StartPlayback(deviceName);
}

AUDIO_API bool StopPlayback(AudioClientHandle handle)
{
	return handle->impl->StopPlayback();
}

AUDIO_API void DestroyDevices(AudioClientHandle handle)
{
	handle->impl->DestroyDevices();
}

AUDIO_API void EmitSound(AudioClientHandle handle, SoundType soundType)
{
	handle->impl->EmitSound(soundType);
}