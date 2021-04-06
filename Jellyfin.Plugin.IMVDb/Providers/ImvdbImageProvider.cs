using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.IMVDb.Providers
{
    /// <summary>
    /// The IMVDb image provider.
    /// </summary>
    public class ImvdbImageProvider : IRemoteImageProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IImvdbClient _imvdbClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImvdbImageProvider"/> class.
        /// </summary>
        /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
        /// <param name="imvdbClient">Instance of the <see cref="IImvdbClient"/> interface.</param>
        public ImvdbImageProvider(
            IHttpClientFactory httpClientFactory,
            IImvdbClient imvdbClient)
        {
            _httpClientFactory = httpClientFactory;
            _imvdbClient = imvdbClient;
        }

        /// <inheritdoc />
        public string Name => "IMVDb";

        /// <inheritdoc />
        public bool Supports(BaseItem item)
            => item is MusicVideo;

        /// <inheritdoc />
        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            yield return ImageType.Primary;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            if (!item.TryGetProviderId(ImvdbPlugin.ProviderName, out var imvdbId))
            {
                return Enumerable.Empty<RemoteImageInfo>();
            }

            var imvdbVideo = await _imvdbClient.GetVideoIdResultAsync(imvdbId, cancellationToken)
                .ConfigureAwait(false);
            if (string.IsNullOrEmpty(imvdbVideo?.Image?.Size1))
            {
                return Enumerable.Empty<RemoteImageInfo>();
            }

            return new[]
            {
                new RemoteImageInfo
                {
                    ProviderName = ImvdbPlugin.ProviderName,
                    Url = imvdbVideo.Image.Size1,
                    Type = ImageType.Primary
                }
            };
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClientFactory.CreateClient(NamedClient.Default)
                .GetAsync(new Uri(url), cancellationToken);
        }
    }
}