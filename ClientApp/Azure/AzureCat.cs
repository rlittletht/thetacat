using System;
using System.Threading;
using Azure.Core;
using Azure.Identity;
using System.Threading.Tasks;
using Thetacat.Types;
using System.ComponentModel;
using Thetacat.Model;

namespace Thetacat.Azure;

public class AzureCat
{
    private static readonly string s_sAppID = "e1a078dd-755e-4b81-acb5-59d7770f96c8";
//    private string s_sAppID = "bfbaffd7-2217-4deb-a85a-4f697e6bdf94";
    private static readonly string s_sAppTenant = "b90f9ef3-5e11-43e0-a75c-1f45e6b223fb";
    private static readonly string s_catalog = "littles";

    private string m_storageAccountName = string.Empty;
    private TokenCredential? m_credential = null;

    private static AzureCat? s_azureCat;
    private object m_lockObject = new object();
    private static readonly object s_globalLock = new object();

    public static AzureCat _Instance
    {
        get
        {
            if (s_azureCat == null)
                throw new CatExceptionInitializationFailure();
            return s_azureCat;
        }
    }

    public static void Create(string storageAccountName)
    {
        s_azureCat = new AzureCat();

        TokenCredentialOptions options = new TokenCredentialOptions();

        s_azureCat.m_credential = new InteractiveBrowserCredential(s_sAppTenant, s_sAppID, options);
        s_azureCat.m_storageAccountName = storageAccountName;
    }

    public static void EnsureCreated(string storageAccountName)
    {
        lock (s_globalLock)
        {
            if (s_azureCat == null)
                Create(storageAccountName);
        }
    }

    public async Task<TcBlobContainer> OpenContainerForCatalog(string catalog)
    {
        return await BlobSync.OpenContainer(catalog, m_storageAccountName, m_credential);
    }

    private TcBlobContainer? m_catalogContainer;

    public async Task<TcBlob> UploadMedia(string destination, string source)
    {
        TcBlobContainer? container = null;

        if (m_catalogContainer == null)
        {
            container = await OpenContainerForCatalog(s_catalog);
            if (container == null)
                throw new CatExceptionAzureFailure();
        }

        lock (m_lockObject)
        {
            if (m_catalogContainer == null && container == null)
                throw new CatExceptionInternalFailure("m_catalogContainer was freed between check and lock");

            m_catalogContainer ??= container;
        }

        return await m_catalogContainer!.Upload(source, destination);
    }
}