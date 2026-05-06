#include "Modules/ModuleManager.h"

class FArgusDeveloperModule : public IModuleInterface
{
public:
    virtual void StartupModule() override {}
    virtual void ShutdownModule() override {}
};

IMPLEMENT_MODULE(FArgusDeveloperModule, ArgusDeveloper)
