#include "ArgusRuntimeModule.h"

#include "Engine/Engine.h"
#include "HAL/PlatformProcess.h"
#include "Misc/App.h"
#include "Misc/Guid.h"

#define LOCTEXT_NAMESPACE "FArgusRuntimeModule"

void FArgusRuntimeModule::StartupModule()
{
}

void FArgusRuntimeModule::ShutdownModule()
{
    Flush();
}

FArgusRuntimeModule& FArgusRuntimeModule::Get()
{
    return FModuleManager::LoadModuleChecked<FArgusRuntimeModule>("ArgusRuntime");
}

void FArgusRuntimeModule::Init(const FArgusConfig& InConfig)
{
    Config = InConfig;
    SessionId = FGuid::NewGuid().ToString(EGuidFormats::Digits);
    bEnabled = !Config.ProjectId.IsEmpty() && !Config.ApiKey.IsEmpty() && !Config.Mode.Equals(TEXT("off"), ESearchCase::IgnoreCase);
    Event(TEXT("sdk_health"), MakeShared<FJsonObject>());
}

void FArgusRuntimeModule::Event(const FString& Name, const TSharedPtr<FJsonObject>& Properties)
{
    if (!CanSend())
    {
        return;
    }
    TSharedPtr<FJsonObject> Payload = MakeShared<FJsonObject>();
    Payload->SetStringField(TEXT("name"), Name);
    Payload->SetNumberField(TEXT("ts"), FPlatformTime::Seconds());
    if (Properties.IsValid())
    {
        Payload->SetObjectField(TEXT("properties"), Properties);
    }
    Enqueue(TEXT("event"), Payload);
}

void FArgusRuntimeModule::Metric(const FString& Name, double Value, const TSharedPtr<FJsonObject>& Tags)
{
    if (!CanSend())
    {
        return;
    }
    TSharedPtr<FJsonObject> Payload = MakeShared<FJsonObject>();
    Payload->SetStringField(TEXT("name"), Name);
    Payload->SetNumberField(TEXT("value"), Value);
    if (Tags.IsValid())
    {
        Payload->SetObjectField(TEXT("tags"), Tags);
    }
    Enqueue(TEXT("metric"), Payload);
}

void FArgusRuntimeModule::SetConsent(bool bGranted)
{
    Config.bConsentGranted = bGranted;
}

void FArgusRuntimeModule::ApplyConfig(const TSharedPtr<FJsonObject>& RemoteConfig)
{
    if (!RemoteConfig.IsValid())
    {
        return;
    }
    bool bRemoteEnabled = true;
    if (RemoteConfig->TryGetBoolField(TEXT("enabled"), bRemoteEnabled) && !bRemoteEnabled)
    {
        bEnabled = false;
    }
    bool bKillSwitch = false;
    if (RemoteConfig->TryGetBoolField(TEXT("kill_switch"), bKillSwitch) && bKillSwitch)
    {
        bEnabled = false;
    }
}

FString FArgusRuntimeModule::CaptureStateHash() const
{
    const FString MapName = GEngine && GEngine->GetWorld() ? GEngine->GetWorld()->GetMapName() : TEXT("unknown");
    return FGuid::NewGuid().ToString(EGuidFormats::Digits) + TEXT(":") + MapName;
}

TSharedPtr<FJsonObject> FArgusRuntimeModule::RuntimeMetadata() const
{
    TSharedPtr<FJsonObject> Metadata = MakeShared<FJsonObject>();
    Metadata->SetStringField(TEXT("engine"), TEXT("unreal"));
    Metadata->SetStringField(TEXT("engine_version"), FEngineVersion::Current().ToString());
    Metadata->SetStringField(TEXT("platform"), FPlatformProperties::PlatformName());
    Metadata->SetStringField(TEXT("sdk_schema_version"), TEXT("1.0.0"));
    Metadata->SetStringField(TEXT("sdk_mode"), Config.Mode);
    return Metadata;
}

TArray<TSharedPtr<FJsonObject>> FArgusRuntimeModule::Flush()
{
    TArray<TSharedPtr<FJsonObject>> Batch;
    TSharedPtr<FJsonObject> Item;
    while (Queue.Dequeue(Item))
    {
        Batch.Add(Item);
    }
    return Batch;
}

bool FArgusRuntimeModule::CanSend() const
{
    return bEnabled && (Config.Mode.Equals(TEXT("test"), ESearchCase::IgnoreCase) || Config.bConsentGranted);
}

void FArgusRuntimeModule::Enqueue(const FString& EventType, const TSharedPtr<FJsonObject>& Payload)
{
    TSharedPtr<FJsonObject> Envelope = MakeShared<FJsonObject>();
    Envelope->SetStringField(TEXT("schema_version"), TEXT("1.0.0"));
    Envelope->SetStringField(TEXT("sdk_name"), TEXT("argus-unreal"));
    Envelope->SetStringField(TEXT("sdk_version"), TEXT("0.1.0"));
    Envelope->SetStringField(TEXT("engine"), TEXT("unreal"));
    Envelope->SetStringField(TEXT("engine_version"), FEngineVersion::Current().ToString());
    Envelope->SetStringField(TEXT("platform"), FPlatformProperties::PlatformName());
    Envelope->SetStringField(TEXT("project_id"), Config.ProjectId);
    Envelope->SetStringField(TEXT("session_id"), SessionId);
    Envelope->SetStringField(TEXT("privacy_mode"), Config.Mode);
    Envelope->SetStringField(TEXT("event_type"), EventType);
    Envelope->SetObjectField(TEXT("payload"), Payload);
    Queue.Enqueue(Envelope);
}

#undef LOCTEXT_NAMESPACE

IMPLEMENT_MODULE(FArgusRuntimeModule, ArgusRuntime)
