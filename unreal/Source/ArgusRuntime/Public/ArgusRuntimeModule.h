#pragma once

#include "Containers/Queue.h"
#include "Dom/JsonObject.h"
#include "Modules/ModuleManager.h"
#include "Templates/SharedPointer.h"

struct FArgusConfig
{
    FString ApiKey;
    FString ProjectId;
    FString BackendUrl = TEXT("https://api.argus.ludotronics.io");
    FString Mode = TEXT("live");
    bool bConsentGranted = false;
};

class FArgusRuntimeModule : public IModuleInterface
{
public:
    virtual void StartupModule() override;
    virtual void ShutdownModule() override;

    void Init(const FArgusConfig& InConfig);
    void Event(const FString& Name, const TSharedPtr<FJsonObject>& Properties = nullptr);
    void Metric(const FString& Name, double Value, const TSharedPtr<FJsonObject>& Tags = nullptr);
    void SetConsent(bool bGranted);
    void ApplyConfig(const TSharedPtr<FJsonObject>& RemoteConfig);
    FString CaptureStateHash() const;
    TSharedPtr<FJsonObject> RuntimeMetadata() const;
    TArray<TSharedPtr<FJsonObject>> Flush();

    static FArgusRuntimeModule& Get();

private:
    bool CanSend() const;
    void Enqueue(const FString& EventType, const TSharedPtr<FJsonObject>& Payload);

    FArgusConfig Config;
    FString SessionId;
    bool bEnabled = false;
    TQueue<TSharedPtr<FJsonObject>> Queue;
    int32 DroppedEvents = 0;
};
