using System.Security.Cryptography;

namespace PhPayrollTimeApi.Api.Keys;

public static class KeyManagement
{
    public static void EnsureKeysExist(string publicKeyPath, string privateKeyPath)
    {
        if (File.Exists(publicKeyPath) && File.Exists(privateKeyPath))
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(publicKeyPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(privateKeyPath)!);

        using var rsa = RSA.Create(2048);
        File.WriteAllText(publicKeyPath, rsa.ExportSubjectPublicKeyInfoPem());
        File.WriteAllText(privateKeyPath, rsa.ExportPkcs8PrivateKeyPem());
    }
}
