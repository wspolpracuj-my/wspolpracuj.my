using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;

#pragma warning disable CS0618

namespace wspolpracujmy.Services
{
    /// <summary>
    /// Serwis do operacji na plikach w Google Cloud Storage.
    /// </summary>
    public class GcsService
    {
        private readonly string _bucketName;
        private readonly string _projectId;
        private readonly string _credentialsPath;
        private readonly StorageClient _storageClient;
        private readonly UrlSigner _urlSigner;

        /// <summary>
        /// Inicjalizuje klienta GCS na podstawie konfiguracji aplikacji.
        /// </summary>
        public GcsService(IConfiguration configuration)
        {
            var gcsSection = configuration.GetSection("GCS");

            _bucketName = gcsSection["BucketName"] ?? throw new ArgumentNullException("GCS:BucketName is not configured.");
            _projectId = gcsSection["ProjectId"] ?? throw new ArgumentNullException("GCS:ProjectId is not configured.");
            _credentialsPath = gcsSection["CredentialsPath"] ?? throw new ArgumentNullException("GCS:CredentialsPath is not configured.");

            var credential = GoogleCredential.FromFile(_credentialsPath);
            _storageClient = StorageClient.Create(credential);
            _urlSigner = UrlSigner.FromCredentialFile(_credentialsPath);
        }

        /// <summary>
        /// Wysyła plik do GCS i zwraca nazwę obiektu.
        /// </summary>
        public async Task<string> UploadFileAsync(Stream stream, string fileName, string contentType, int teamId, CancellationToken cancellationToken = default)
        {
            var safeFileName = Path.GetFileName(fileName);
            var objectName = $"teams/{teamId}/{Guid.NewGuid():N}_{safeFileName}";

            await _storageClient.UploadObjectAsync(
                bucket: _bucketName,
                objectName: objectName,
                contentType: string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
                source: stream,
                cancellationToken: cancellationToken);

            return objectName;
        }

        /// <summary>
        /// Generuje czasowy signed URL do pobrania pliku.
        /// </summary>
        public async Task<string> GenerateDownloadUrlAsync(string objectName)
        {
            var signedUrl = _urlSigner.Sign(
                _bucketName,
                objectName,
                TimeSpan.FromMinutes(15),
                HttpMethod.Get);

            return await Task.FromResult(signedUrl);
        }

        /// <summary>
        /// Usuwa pojedynczy obiekt z GCS.
        /// </summary>
        public async Task DeleteFileAsync(string objectName, CancellationToken cancellationToken = default)
        {
            await _storageClient.DeleteObjectAsync(
                bucket: _bucketName,
                objectName: objectName,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Usuwa wiele obiektów z GCS.
        /// </summary>
        public async Task DeleteFilesAsync(IEnumerable<string> objectNames, CancellationToken cancellationToken = default)
        {
            var objectList = objectNames?.ToList() ?? new List<string>();

            if (objectList.Count == 0)
            {
                return;
            }

            foreach (var objectName in objectList)
            {
                try
                {
                    await _storageClient.DeleteObjectAsync(
                        bucket: _bucketName,
                        objectName: objectName,
                        cancellationToken: cancellationToken);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to delete GCS object '{objectName}': {ex.Message}");
                }
            }
        }
    }
}

#pragma warning restore CS0618