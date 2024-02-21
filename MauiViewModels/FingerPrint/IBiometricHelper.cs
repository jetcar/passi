using System;

namespace MauiViewModels.FingerPrint;

public interface IBiometricHelper
{
    void RegisterOrAuthenticate();
}