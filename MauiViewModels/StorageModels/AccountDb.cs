using MauiViewModels.ViewModels;

namespace MauiViewModels.StorageModels;

public class AccountDb : AccountModel
{
    private bool _haveFingerprint;
    public string Salt { get; set; }
    public string PrivateCertBinary { get; set; }
    public int pinLength { get; set; }
    public string PublicCertBinary { get; set; }

    public bool HaveFingerprint
    {
        get => _haveFingerprint;
        set => _haveFingerprint = value;
    }
}