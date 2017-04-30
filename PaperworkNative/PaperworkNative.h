#pragma once

#define PW_EXPORT extern "C" __declspec(dllexport)
#define LOG(X) (OutputDebugStringA("NATIVE: " X))