#include "PaperWorkNative.h"
#include "IUnityInterface.h"
#include "steam\steam_api.h"
#include <Windows.h>

static IUnityInterfaces* s_UnityInterfaces = 0;

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces* unityInterfaces)
{
	s_UnityInterfaces = unityInterfaces;
}

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginUnload()
{
	//...
}

PW_EXPORT bool pwSteamInit()
{
	LOG("Init Steam.");
	return SteamAPI_Init();
}

PW_EXPORT bool pwSteamRestartIfNecessary(uint32 AppID)
{
	LOG("Restart through Steam if required.");
	return SteamAPI_RestartAppIfNecessary(AppID);
}

PW_EXPORT void pwSteamShutdown()
{
	LOG("Shutdown Steam.");
	SteamAPI_Shutdown();
}

PW_EXPORT void pwSteamUpdate()
{
	// TODO: Run callbacks
}

PW_EXPORT const char * pwSteamGetName()
{	
	LOG("Get Name");
	// TODO: How do we know if this can be null?
	if (SteamFriends() == 0)
		return 0;

	return SteamFriends()->GetPersonaName();
}

PW_EXPORT uint64 pwSteamGetID()
{
	CSteamID id = SteamUser()->GetSteamID();
	return id.ConvertToUint64();
}

PW_EXPORT float GetSimpleValue(float Test)
{
	return Test + 10.0f;
}
