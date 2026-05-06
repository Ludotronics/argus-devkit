#include "ArgusAutomationBridge.h"

#include "ArgusRuntimeModule.h"
#include "Engine/Engine.h"

FString FArgusAutomationBridge::CaptureStateHash()
{
    return FArgusRuntimeModule::Get().CaptureStateHash();
}

TSharedPtr<FJsonObject> FArgusAutomationBridge::CaptureState()
{
    TSharedPtr<FJsonObject> State = MakeShared<FJsonObject>();
    State->SetStringField(TEXT("state_hash"), CaptureStateHash());
    State->SetStringField(TEXT("scene"), GEngine && GEngine->GetWorld() ? GEngine->GetWorld()->GetMapName() : TEXT("unknown"));
    State->SetObjectField(TEXT("runtime"), FArgusRuntimeModule::Get().RuntimeMetadata());
    return State;
}

bool FArgusAutomationBridge::AcceptCommand(const TSharedPtr<FJsonObject>& Command)
{
    if (!Command.IsValid())
    {
        return false;
    }
    const FString Action = Command->GetStringField(TEXT("action"));
    return Action == TEXT("tap") || Action == TEXT("swipe") || Action == TEXT("key") || Action == TEXT("wait");
}
