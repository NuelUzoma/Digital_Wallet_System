using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;

public class ReferenceProtector
{
    private readonly IDataProtector _protector;

    public ReferenceProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("TransactionReferences");
    }

    public string Protect(string reference)
    {
        if (string.IsNullOrEmpty(reference))
            return string.Empty;

        return _protector.Protect(reference);
    }

    public string Unprotect(string? encryptedReference)
    {
        if (string.IsNullOrEmpty(encryptedReference))
            return string.Empty;
        
        try
        {
            return _protector.Unprotect(encryptedReference);
        }
        catch (CryptographicException)
        {
            // If decryption fails, return the original string
            // This allows handling of unencrypted legacy data
            return encryptedReference;
        }
    }
}