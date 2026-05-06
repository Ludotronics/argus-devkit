#pragma once

#include "Dom/JsonObject.h"
#include "Templates/SharedPointer.h"

class FArgusAutomationBridge
{
public:
    static FString CaptureStateHash();
    static TSharedPtr<FJsonObject> CaptureState();
    static bool AcceptCommand(const TSharedPtr<FJsonObject>& Command);
};
